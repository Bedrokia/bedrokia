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

using System.Globalization;
using YamlDotNet.Serialization;

namespace Bedrokia.Server.Plugins;

/// <summary>
/// A YAML file in the plugin's data folder (config.yml by default), read with dotted paths like Bukkit:
/// <c>Config.Get("sidebar.title", "My server")</c>.
/// </summary>
public sealed class PluginConfig
{
    private readonly string _file;
    private Dictionary<object, object> _root = [];

    public PluginConfig(string file)
    {
        _file = file;
        Reload();
    }

    /// <summary>Reads the file again (after an edit by hand).</summary>
    /// <exception cref="InvalidDataException">The file is not valid YAML.</exception>
    public void Reload()
    {
        if (!File.Exists(_file))
        {
            _root = [];
            return;
        }
        try
        {
            _root = new DeserializerBuilder().Build().Deserialize<Dictionary<object, object>?>(File.ReadAllText(_file)) ?? [];
        }
        catch (YamlDotNet.Core.YamlException e)
        {
            throw new InvalidDataException($"{Path.GetFileName(_file)} is not valid YAML: {e.Message}");
        }
    }

    public bool Contains(string path) => Find(path) is not null;

    /// <summary>Value at <paramref name="path"/> ("a.b.c"), or <paramref name="fallback"/> if missing or of another type.
    /// Lists come as <c>List&lt;string&gt;</c>.</summary>
    public T Get<T>(string path, T fallback)
    {
        var value = Find(path);
        if (value is T typed)
            return typed;
        if (typeof(T) == typeof(List<string>) && value is IEnumerable<object> list)
            return (T)(object)list.Select(v => v?.ToString() ?? "").ToList();
        try
        {
            return value is null ? fallback : (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
        {
            return fallback;
        }
    }

    public string GetString(string path, string fallback = "") => Get(path, fallback);
    public int GetInt(string path, int fallback = 0) => Get(path, fallback);
    public double GetDouble(string path, double fallback = 0) => Get(path, fallback);
    public bool GetBool(string path, bool fallback = false) => Get(path, fallback);
    public List<string> GetList(string path) => Get(path, new List<string>());

    /// <summary>Sets a value (creating the sections on the way); call <see cref="Save"/> to write the file.</summary>
    public void Set(string path, object? value)
    {
        var keys = path.Split('.');
        var section = _root;
        foreach (var key in keys[..^1])
        {
            if (section.GetValueOrDefault(key) is not Dictionary<object, object> next)
                section[key] = next = [];
            section = next;
        }
        if (value is null)
            section.Remove(keys[^1]);
        else
            section[keys[^1]] = value;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
        File.WriteAllText(_file, new SerializerBuilder().Build().Serialize(_root));
    }

    private object? Find(string path)
    {
        object? current = _root;
        foreach (var key in path.Split('.'))
            current = current is Dictionary<object, object> section ? section.GetValueOrDefault(key) : null;
        return current;
    }
}
