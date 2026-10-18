// ┌───────────────────────────────────────────────────────────────────┐
// │  ____  _____ ____  ____   ___  _  __ ___    _                     │
// │ | __ )| ____|  _ \|  _ \ / _ \| |/ /|_ _|  / \                    │
// │ |  _ \|  _| | | | | |_) | | | | ' /  | |  / _ \                   │
// │ | |_) | |___| |_| |  _ <| |_| | . \  | | / ___ \                  │
// │ |____/|_____|____/|_| \_\\___/|_|\_\|___/_/   \_\                 │
// │                                                                   │
// │  Bedrokia - Minecraft: Bedrock Edition server software            │
// │  Licensed under the Apache License 2.0 - see LICENSE              │
// └───────────────────────────────────────────────────────────────────┘

namespace Bedrokia.Server.Core;

/// <summary>
/// A persisted set of player names (ops, whitelist, bans): one lower-case name per line in a text file under
/// config/, editable by hand while the server is stopped.
/// </summary>
public sealed class PlayerList
{
    private readonly string _path;
    private readonly HashSet<string> _names;

    public PlayerList(string path)
    {
        _path = path;
        _names = File.Exists(path)
            ? File.ReadAllLines(path).Select(l => l.Trim().ToLowerInvariant()).Where(l => l.Length > 0 && !l.StartsWith('#')).ToHashSet()
            : [];
    }

    public IEnumerable<string> Names => _names.Order();

    public bool Contains(string name) => _names.Contains(name.ToLowerInvariant());

    /// <summary>Returns false if the name was already there.</summary>
    public bool Add(string name)
    {
        var added = _names.Add(name.ToLowerInvariant());
        if (added)
            Save();
        return added;
    }

    /// <summary>Returns false if the name was not there.</summary>
    public bool Remove(string name)
    {
        var removed = _names.Remove(name.ToLowerInvariant());
        if (removed)
            Save();
        return removed;
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
        File.WriteAllLines(_path, Names.Prepend($"# {Product.CreatedBy} - one player name per line"));
    }
}
