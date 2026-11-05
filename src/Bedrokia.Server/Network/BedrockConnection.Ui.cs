using System.Collections.Concurrent;
using Bedrokia.Protocol;
using Bedrokia.Server.Game.Ui.Ddui;
using Bedrokia.Server.Game.Ui.Forms;

namespace Bedrokia.Server.Network;

public sealed partial class BedrockConnection
{
    public const int MaxQueuedForms = 3;
    public const long FormTimeoutTicks = 6000; // 5 minutes (20 Hz)

    private readonly ConcurrentDictionary<uint, IForm> _activeForms = new();
    private uint _nextFormId;

    /// <summary>Data-Driven UI session and reactive DataStores for this player.</summary>
    public DduiSession DduiSession { get; } = new();

    /// <summary>Active form count currently awaiting client response.</summary>
    public int ActiveFormCount => _activeForms.Count;

    /// <summary>
    /// Dispatches a form (SimpleForm, ModalForm, CustomForm) to the client.
    /// Manages anti-replay protection with incremental FormId and bounded queueing.
    /// </summary>
    public uint SendForm(IForm form)
    {
        uint formId = Interlocked.Increment(ref _nextFormId);
        form.Id = formId;
        form.SentTick = _server?.CurrentTick ?? 0;

        // Bounded queue: max 3 forms. If exceeded, cancel oldest with UserBusy.
        if (_activeForms.Count >= MaxQueuedForms)
        {
            var oldest = _activeForms.Values.OrderBy(f => f.SentTick).FirstOrDefault();
            if (oldest is not null && _activeForms.TryRemove(oldest.Id, out var removed))
                removed.HandleClose(this, FormCancelReason.UserBusy);
        }

        _activeForms[formId] = form;
        Send(UiPackets.ModalFormRequest(formId, form.SerializeJson()));
        return formId;
    }

    /// <summary>
    /// Closes a specific active form.
    /// </summary>
    public void CloseForm(uint formId, FormCancelReason reason = FormCancelReason.UserClosed)
    {
        if (_activeForms.TryRemove(formId, out var form))
            form.HandleClose(this, reason);
    }

    /// <summary>
    /// Closes all active forms (e.g. on player death, teleportation, or disconnect).
    /// </summary>
    public void CloseAllForms(FormCancelReason reason = FormCancelReason.UserClosed)
    {
        foreach (var key in _activeForms.Keys.ToList())
        {
            if (_activeForms.TryRemove(key, out var form))
                form.HandleClose(this, reason);
        }
    }

    /// <summary>
    /// Shows a Data-Driven UI (DDUI) screen on the client.
    /// </summary>
    public void ShowDduiScreen(string screenId, DduiDataStore? dataStore = null)
    {
        uint formId = Interlocked.Increment(ref _nextFormId);
        DduiSession.OpenScreen(screenId, dataStore);
        Send(UiPackets.DataDrivenUIShowScreen(screenId, formId));
        // The packet has no initial data: the store's current values follow as a normal update.
        if (dataStore is { Values.Count: > 0 })
            Send(UiPackets.ClientboundDataStore(dataStore.Name, dataStore.Values.ToDictionary(
                v => v.Key, v => Convert.ToString(v.Value, System.Globalization.CultureInfo.InvariantCulture) ?? "")));
    }

    /// <summary>
    /// Closes all active Data-Driven UI screens on the client.
    /// </summary>
    public void CloseAllDduiScreens()
    {
        DduiSession.CloseAll();
        Send(UiPackets.DataDrivenUICloseAllScreens());
    }

    /// <summary>
    /// Sends a reactive property update to a client DataStore.
    /// </summary>
    public void SendDataStoreUpdate(string storeName, string property, string value) =>
        Send(UiPackets.ClientboundDataStore(storeName, new Dictionary<string, string> { [property] = value }));

    /// <summary>
    /// Sends batch property updates to a client DataStore.
    /// </summary>
    public void SendDataStoreUpdates(string storeName, IReadOnlyDictionary<string, string> updates) =>
        Send(UiPackets.ClientboundDataStore(storeName, updates));

    /// <summary>
    /// Removes forms that exceeded the maximum timeout (e.g. 5 minutes).
    /// </summary>
    internal void PruneExpiredForms(long currentTick)
    {
        foreach (var (id, form) in _activeForms)
        {
            if (currentTick - form.SentTick > FormTimeoutTicks)
            {
                if (_activeForms.TryRemove(id, out var expired))
                    expired.HandleClose(this, FormCancelReason.Timeout);
            }
        }
    }

    /// <summary>
    /// Handles ModalFormResponsePacket (0x65 / 101) with Dragonfly-grade security validation.
    /// </summary>
    internal void OnModalFormResponse(ref BedrockReader reader)
    {
        if (reader.Remaining == 0)
            return;

        var (formId, hasResponse, responseJson, hasCancel, cancelReason) = UiPackets.ReadModalFormResponse(ref reader);
        if (Server?.Events is { HasPlayerFormResponse: true } formEvs)
        {
            var formEv = new PlayerFormResponseEvent(this, formId, responseJson, hasCancel, cancelReason);
            if (!formEvs.OnPlayerFormResponse(formEv)) return;
        }

        // Anti-Replay: immediately remove before invoking any handlers
        if (!_activeForms.TryRemove(formId, out var form))
            return; // Unknown or expired form, silently ignore

        // Check if the client cancelled or closed the form
        if (hasCancel)
        {
            var reason = cancelReason == 1 ? FormCancelReason.UserBusy : FormCancelReason.UserClosed;
            form.HandleClose(this, reason);
            return;
        }

        if (!hasResponse || responseJson is null || string.Equals(responseJson.Trim(), "null", StringComparison.OrdinalIgnoreCase))
        {
            form.HandleClose(this, FormCancelReason.UserClosed);
            return;
        }

        // Anti-DoS checks before parsing: payload size <= 16 KB and depth <= 2
        if (!FormSecurity.ValidatePayloadSize(responseJson) || !FormSecurity.ValidateJsonDepth(responseJson))
        {
            form.HandleClose(this, FormCancelReason.InvalidResponse);
            return;
        }

        // Strict validation according to form type. The submit and close callbacks are plugin code: an error there is
        // logged instead of dropping the rest of the player's packets.
        try
        {
            if (!form.HandleResponse(this, responseJson))
                form.HandleClose(this, FormCancelReason.InvalidResponse);
        }
        catch (Exception e) when (e is not InvalidDataException)
        {
            _server.Logger.Error($"A plugin crashed while handling {Name}'s answer to form {formId}: {e}");
        }
    }

    /// <summary>
    /// Handles ServerboundDataStorePacket (324) with anti-flood rate limits and schema checks.
    /// </summary>
    internal void OnServerboundDataStore(ref BedrockReader reader)
    {
        if (reader.Remaining == 0)
            return;

        var (storeName, property, value) = UiPackets.ReadServerboundDataStore(ref reader);
        var currentTick = _server?.CurrentTick ?? 0;

        if (!DduiSession.CanAcceptUpdate(currentTick))
            return; // Anti-flood rate limit

        var store = DduiSession.GetDataStore(storeName);
        if (store is null)
            return;

        store.TrySetClientValue(property, value);
    }

    /// <summary>
    /// Handles ServerSettingsRequestPacket (102).
    /// </summary>
    private void OnServerSettingsRequest(ref BedrockReader reader)
    {
        // Reserved for server settings form if registered
    }
}
