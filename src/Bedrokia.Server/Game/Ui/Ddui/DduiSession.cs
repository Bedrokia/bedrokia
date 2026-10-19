using System.Collections.Concurrent;

namespace Bedrokia.Server.Game.Ui.Ddui;

/// <summary>
/// Tracks active DDUI screens and data stores for a connected player, with per-tick flood protection.
/// </summary>
public sealed class DduiSession
{
    public const int MaxUpdatesPerTick = 20;

    private readonly HashSet<string> _openScreens = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DduiDataStore> _dataStores = new(StringComparer.OrdinalIgnoreCase);
    private int _updatesThisTick;
    private long _lastTick;

    public IReadOnlyCollection<string> OpenScreens => _openScreens;
    public IReadOnlyDictionary<string, DduiDataStore> DataStores => _dataStores;

    public void OpenScreen(string screenId, DduiDataStore? dataStore = null)
    {
        _openScreens.Add(screenId);
        if (dataStore is not null)
            _dataStores[dataStore.Name] = dataStore;
    }

    public void CloseScreen(string screenId) =>
        _openScreens.Remove(screenId);

    public void CloseAll()
    {
        _openScreens.Clear();
        _dataStores.Clear();
    }

    public void RegisterDataStore(DduiDataStore dataStore) =>
        _dataStores[dataStore.Name] = dataStore;

    public DduiDataStore? GetDataStore(string name) =>
        _dataStores.TryGetValue(name, out var store) ? store : null;

    /// <summary>
    /// Checks rate limit against packet flood attacks from client DDUI events.
    /// </summary>
    public bool CanAcceptUpdate(long currentTick)
    {
        if (currentTick != _lastTick)
        {
            _lastTick = currentTick;
            _updatesThisTick = 0;
        }

        if (_updatesThisTick >= MaxUpdatesPerTick)
            return false;

        _updatesThisTick++;
        return true;
    }
}
