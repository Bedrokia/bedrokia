using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a client submits a ModalFormResponse packet answering a UI form.
/// </summary>
public sealed class PlayerFormResponseEvent(BedrockConnection player, uint formId, string? responseJson, bool hasCancel, byte? cancelReason = null) : CancellableEvent, IPlayerEvent
{
    /// <summary>The player who answered the form.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>The unique form identifier.</summary>
    public uint FormId { get; } = formId;

    /// <summary>The JSON response payload, or null if the form was cancelled.</summary>
    public string? ResponseJson { get; } = responseJson;

    /// <summary>True if the user closed or cancelled the form.</summary>
    public bool HasCancel { get; } = hasCancel;

    /// <summary>Cancellation reason reported by the client (0 = user closed, 1 = user busy).</summary>
    public byte? CancelReason { get; } = cancelReason;
}
