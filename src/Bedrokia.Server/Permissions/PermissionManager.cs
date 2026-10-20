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

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bedrokia.Server.Permissions;

/// <summary>Represents a permission group/role (like LuckPerms or Dragonfly).</summary>
public sealed class PermissionGroup
{
    public string Name { get; set; } = "";
    public string Prefix { get; set; } = "";
    public int Priority { get; set; }
    public string? Parent { get; set; }
    public List<string> Permissions { get; set; } = [];
}

/// <summary>Per-player permission and group assignment.</summary>
public sealed class PlayerPermissionData
{
    public string Group { get; set; } = "default";
    public List<string> Permissions { get; set; } = [];
}

/// <summary>JSON storage structure for permissions and groups.</summary>
public sealed class PermissionStorage
{
    public Dictionary<string, PermissionGroup> Groups { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, PlayerPermissionData> Players { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Manages granular permissions, groups/ranks with inheritance, prefixes, and thread-safe atomic persistence.
/// </summary>
public sealed class PermissionManager
{
    private readonly string _path;
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, bool> _permCache = new(StringComparer.OrdinalIgnoreCase);
    private PermissionStorage _storage = new();

    public const string DefaultGroupName = "default";

    public void InvalidateCache() => _permCache.Clear();

    public PermissionManager(string path)
    {
        _path = path;
        Load();
    }

    // Players are keyed by their exact name (case-insensitive dictionaries): stripping spaces or accents made
    // "Jean Pierre" and "JeanPierre" share the same permissions, so anyone could take an admin's rights.
    private static string PlayerKey(string name) => name.Trim();

    /// <summary>The error met while reading permissions.json, if any (the broken file was kept aside).</summary>
    public string? LoadError { get; private set; }

    /// <summary>Normalizes group names (lowercase letters, digits, _ . -).</summary>
    public static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";
        // Strip out path traversal indicators and illegal identifier chars
        var cleaned = name.Replace("../", "").Replace("..\\", "").Trim().ToLowerInvariant();
        return Regex.Replace(cleaned, @"[^a-z0-9_.-]", "");
    }

    public void Load()
    {
        lock (_lock)
        {
            _permCache.Clear();
            if (File.Exists(_path))
            {
                try
                {
                    var json = File.ReadAllText(_path);
                    var loaded = JsonSerializer.Deserialize<PermissionStorage>(json);
                    if (loaded is not null)
                    {
                        _storage = loaded;
                        // Ensure case-insensitive comparer
                        _storage.Groups = new Dictionary<string, PermissionGroup>(_storage.Groups, StringComparer.OrdinalIgnoreCase);
                        _storage.Players = new Dictionary<string, PlayerPermissionData>(_storage.Players, StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
                {
                    // Never lose the owner's groups: the next Save() would overwrite the file with an empty one.
                    var backup = $"{_path}.broken-{DateTime.Now:yyyyMMdd-HHmmss}";
                    try
                    {
                        File.Copy(_path, backup, overwrite: true);
                    }
                    catch (IOException)
                    {
                        backup = "(copy failed)";
                    }
                    LoadError = $"{Path.GetFileName(_path)} could not be read ({e.Message}). It was copied to {backup}; fix it and restart.";
                    _storage = new PermissionStorage();
                }
            }

            if (!_storage.Groups.ContainsKey(DefaultGroupName))
            {
                _storage.Groups[DefaultGroupName] = new PermissionGroup
                {
                    Name = DefaultGroupName,
                    Prefix = "",
                    Priority = 0,
                    Permissions =
                    [
                        "bedrokia.command.help",
                        "bedrokia.command.version",
                        "bedrokia.command.plugin",
                        "bedrokia.command.list",
                        "bedrokia.command.seed",
                        "bedrokia.command.me",
                        "bedrokia.command.tell",
                        "bedrokia.command.spawn"
                    ]
                };
            }
        }
    }

    /// <summary>Atomically saves permissions to disk via a temporary file replacement.</summary>
    public void Save()
    {
        lock (_lock)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_path));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var tmpPath = _path + ".tmp";
            var json = JsonSerializer.Serialize(_storage, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, _path, overwrite: true);
            _permCache.Clear();
        }
    }

    private readonly List<Func<string, string, bool>> _providers = [];

    /// <summary>
    /// Grants permissions from a plugin (a rank system...): asked before the groups of permissions.json, with the player
    /// name and the permission; true grants it. Never cached, so a rank change applies at once.
    /// </summary>
    public void AddProvider(Func<string, string, bool> provider)
    {
        lock (_providers)
            _providers.Add(provider);
    }

    public bool HasPermission(string playerName, string? permission, bool isOp = false)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return true;

        Func<string, string, bool>[] providers;
        lock (_providers)
            providers = [.. _providers];
        foreach (var provider in providers)
            if (provider(playerName, permission))
                return true;

        var key = $"{playerName}|{permission}|{isOp}";
        if (_permCache.TryGetValue(key, out var cached))
            return cached;

        var effective = GetEffectivePermissions(playerName);
        var result = PermissionEvaluator.Evaluate(permission, effective, isOp);
        _permCache[key] = result;
        return result;
    }

    public List<string> GetEffectivePermissions(string playerName)
    {
        var cleanName = PlayerKey(playerName);
        lock (_lock)
        {
            var result = new List<string>();

            // 1. Direct player permissions (highest precedence)
            if (_storage.Players.TryGetValue(cleanName, out var playerData))
            {
                result.AddRange(playerData.Permissions);
            }

            // 2. Group permissions (traversing inheritance DAG with cycle detection)
            var groupName = playerData?.Group ?? DefaultGroupName;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var currentGroup = groupName;

            while (!string.IsNullOrEmpty(currentGroup) && visited.Add(currentGroup))
            {
                if (_storage.Groups.TryGetValue(currentGroup, out var group))
                {
                    result.AddRange(group.Permissions);
                    currentGroup = group.Parent;
                }
                else
                {
                    break;
                }
            }

            return result;
        }
    }

    public string GetPlayerPrefix(string playerName)
    {
        var cleanName = PlayerKey(playerName);
        lock (_lock)
        {
            if (_storage.Players.TryGetValue(cleanName, out var playerData) &&
                _storage.Groups.TryGetValue(playerData.Group, out var group))
            {
                return group.Prefix;
            }

            if (_storage.Groups.TryGetValue(DefaultGroupName, out var defaultGroup))
                return defaultGroup.Prefix;

            return "";
        }
    }

    public void CreateGroup(string name, string prefix = "", int priority = 0, string? parent = null)
    {
        var clean = Sanitize(name);
        if (string.IsNullOrEmpty(clean))
            return;

        lock (_lock)
        {
            if (!_storage.Groups.TryGetValue(clean, out var group))
            {
                group = new PermissionGroup { Name = clean };
                _storage.Groups[clean] = group;
            }
            group.Prefix = prefix;
            group.Priority = priority;
            group.Parent = string.IsNullOrEmpty(parent) ? null : Sanitize(parent);
            Save();
        }
    }

    public bool RemoveGroup(string name)
    {
        var clean = Sanitize(name);
        if (clean == DefaultGroupName)
            return false;

        lock (_lock)
        {
            var removed = _storage.Groups.Remove(clean);
            if (removed)
                Save();
            return removed;
        }
    }

    public void AddGroupPermission(string groupName, string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return;
        var clean = Sanitize(groupName);
        if (string.IsNullOrEmpty(clean))
            return;
        var perm = permission.Trim();
        lock (_lock)
        {
            if (!_storage.Groups.TryGetValue(clean, out var group))
            {
                group = new PermissionGroup { Name = clean };
                _storage.Groups[clean] = group;
            }

            if (!group.Permissions.Contains(perm, StringComparer.OrdinalIgnoreCase))
            {
                group.Permissions.Add(perm);
                Save();
            }
        }
    }

    public void RemoveGroupPermission(string groupName, string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return;
        var clean = Sanitize(groupName);
        if (string.IsNullOrEmpty(clean))
            return;
        var perm = permission.Trim();
        lock (_lock)
        {
            if (_storage.Groups.TryGetValue(clean, out var group))
            {
                group.Permissions.RemoveAll(p => p.Equals(perm, StringComparison.OrdinalIgnoreCase));
                Save();
            }
        }
    }

    public void SetPlayerGroup(string playerName, string groupName)
    {
        var cleanPlayer = PlayerKey(playerName);
        if (string.IsNullOrEmpty(cleanPlayer))
            return;
        var cleanGroup = Sanitize(groupName);
        if (string.IsNullOrEmpty(cleanGroup))
            cleanGroup = DefaultGroupName;
        lock (_lock)
        {
            if (!_storage.Players.TryGetValue(cleanPlayer, out var data))
            {
                data = new PlayerPermissionData();
                _storage.Players[cleanPlayer] = data;
            }
            data.Group = cleanGroup;
            Save();
        }
    }

    public string GetPlayerGroup(string playerName)
    {
        var cleanPlayer = PlayerKey(playerName);
        lock (_lock)
        {
            return _storage.Players.TryGetValue(cleanPlayer, out var data) ? data.Group : DefaultGroupName;
        }
    }

    public void AddPlayerPermission(string playerName, string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return;
        var cleanPlayer = PlayerKey(playerName);
        if (string.IsNullOrEmpty(cleanPlayer))
            return;
        var perm = permission.Trim();
        lock (_lock)
        {
            if (!_storage.Players.TryGetValue(cleanPlayer, out var data))
            {
                data = new PlayerPermissionData();
                _storage.Players[cleanPlayer] = data;
            }

            if (!data.Permissions.Contains(perm, StringComparer.OrdinalIgnoreCase))
            {
                data.Permissions.Add(perm);
                Save();
            }
        }
    }

    public void RemovePlayerPermission(string playerName, string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return;
        var cleanPlayer = PlayerKey(playerName);
        if (string.IsNullOrEmpty(cleanPlayer))
            return;
        var perm = permission.Trim();
        lock (_lock)
        {
            if (_storage.Players.TryGetValue(cleanPlayer, out var data))
            {
                data.Permissions.RemoveAll(p => p.Equals(perm, StringComparison.OrdinalIgnoreCase));
                Save();
            }
        }
    }

    public IReadOnlyDictionary<string, PermissionGroup> Groups
    {
        get
        {
            lock (_lock) return new Dictionary<string, PermissionGroup>(_storage.Groups, StringComparer.OrdinalIgnoreCase);
        }
    }

    public IReadOnlyDictionary<string, PlayerPermissionData> Players
    {
        get
        {
            lock (_lock) return new Dictionary<string, PlayerPermissionData>(_storage.Players, StringComparer.OrdinalIgnoreCase);
        }
    }
}
