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

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Bedrokia.Server.Plugins;

/// <summary>
/// plugin.yml, like Bukkit and PocketMine-MP:
/// <code>
/// name: MyPlugin
/// version: 1.0.0
/// main: MyPlugin.Main          # full name of the class deriving from Plugin
/// api: 1.0.0
/// author: Me
/// description: What it does.
/// depend: [OtherPlugin]        # loaded first; missing = this plugin is not loaded
/// softdepend: [Optional]       # loaded first when present
/// commands:
///   hello:
///     description: Says hello.
///     usage: "[player]"
///     permission: everyone     # everyone or op (default)
///     aliases: [hi]
/// </code>
/// </summary>
public sealed class PluginDescription
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Main { get; set; } = "";
    public string Api { get; set; } = "1.0.0";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string Website { get; set; } = "";
    public List<string> Depend { get; set; } = [];
    public List<string> Softdepend { get; set; } = [];
    public Dictionary<string, PluginCommandInfo> Commands { get; set; } = [];

    /// <exception cref="InvalidDataException">The file is not valid YAML or misses name/main.</exception>
    public static PluginDescription Load(string file)
    {
        PluginDescription? description;
        try
        {
            description = new DeserializerBuilder().WithNamingConvention(LowerCaseNamingConvention.Instance).IgnoreUnmatchedProperties()
                .Build().Deserialize<PluginDescription>(File.ReadAllText(file));
        }
        catch (YamlDotNet.Core.YamlException e)
        {
            throw new InvalidDataException($"plugin.yml is not valid YAML: {e.Message}");
        }
        if (description is null || string.IsNullOrWhiteSpace(description.Name) || string.IsNullOrWhiteSpace(description.Main))
            throw new InvalidDataException("plugin.yml must at least contain 'name' and 'main'.");
        if (description.Name.Any(c => !char.IsLetterOrDigit(c) && c is not ('_' or '-' or '.')))
            throw new InvalidDataException($"plugin name '{description.Name}' may only contain letters, digits, _ - and .");
        return description;
    }
}

public sealed class PluginCommandInfo
{
    public string Description { get; set; } = "";
    public string Usage { get; set; } = "";

    /// <summary>"everyone" or "op".</summary>
    public string Permission { get; set; } = "op";

    public List<string> Aliases { get; set; } = [];
}
