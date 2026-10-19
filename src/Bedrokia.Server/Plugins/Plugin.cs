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

using Bedrokia.Server.Commands;

namespace Bedrokia.Server.Plugins;

/// <summary>
/// Base class of a Bedrokia plugin, organised like a Bukkit / PocketMine-MP one:
/// <code>
/// plugins/MyPlugin/
///   plugin.yml           name, version, main class, author, dependencies, commands
///   src/**/*.cs          C# code, compiled when the server starts (no build needed)
///   resources/config.yml default config, copied to plugin_data/MyPlugin/ the first time
/// </code>
/// A plain .cs file or a .NET 10 DLL dropped in plugins/ also works (no plugin.yml). Everything runs on the game thread.
/// </summary>
public abstract class Plugin
{
    private PluginConfig? _config;

    /// <summary>Contents of plugin.yml (built from the class for plugins without one).</summary>
    public PluginDescription Info { get; internal set; } = new();

    /// <summary>Name shown in /plugin and used for plugin_data/&lt;Name&gt;/.</summary>
    public string Name => Info.Name;

    public string Version => Info.Version;

    /// <summary>Description from plugin.yml; plugins without one can override it.</summary>
    public virtual string Description => Info.Description;

    public BedrokiaServer Server { get; internal set; } = null!;

    /// <summary>plugin_data/&lt;Name&gt;/, created before <see cref="OnEnable"/>: config and saved data go there.</summary>
    public string DataFolder { get; internal set; } = "";

    /// <summary>The plugin's resources/ folder (default files), null when it has none.</summary>
    public string? ResourceFolder { get; internal set; }

    /// <summary>Server events (join, quit, chat, blocks, items, moves, ticks).</summary>
    public ServerEvents Events => Server.Events;

    /// <summary>Task scheduler for the plugin.</summary>
    public ServerScheduler Scheduler => Server.Scheduler;

    /// <summary>Schedules an action to run on the next tick with this plugin as owner.</summary>
    public ScheduledTask RunNextTick(Action action) => Server.Scheduler.RunNextTick(action, this);

    /// <summary>Schedules an action to run after <paramref name="delayTicks"/> ticks with this plugin as owner.</summary>
    public ScheduledTask ScheduleDelayed(int delayTicks, Action action) => Server.Scheduler.ScheduleDelayed(delayTicks, action, this);

    /// <summary>Schedules an action to run repeatedly every <paramref name="periodTicks"/> after an initial delay with this plugin as owner.</summary>
    public ScheduledTask ScheduleRepeating(int delayTicks, int periodTicks, Action action) =>
        Server.Scheduler.ScheduleRepeating(delayTicks, periodTicks, action, this);

    /// <summary>config.yml in the data folder (copied from resources/config.yml the first time).</summary>
    public PluginConfig Config => _config ??= new PluginConfig(Path.Combine(DataFolder, "config.yml"));

    /// <summary>Called once the plugin is loaded: register events and commands here.</summary>
    public virtual void OnEnable()
    {
    }

    /// <summary>Called when the server stops: save data here.</summary>
    public virtual void OnDisable()
    {
    }

    /// <summary>A command declared in plugin.yml was run. Return false to show its usage.</summary>
    public virtual bool OnCommand(CommandSender sender, string command, string[] args) => false;

    public void Log(string message) => Server.Logger.Info($"[{Name}] {message}");
    public void LogWarning(string message) => Server.Logger.Warn($"[{Name}] {message}");
    public void LogError(string message) => Server.Logger.Error($"[{Name}] {message}");

    /// <summary>Copies resources/&lt;name&gt; into the data folder; returns false if it is missing or already there.</summary>
    public bool SaveResource(string name, bool replace = false)
    {
        var source = ResourceFolder is null ? null : Path.Combine(ResourceFolder, name);
        var target = Path.Combine(DataFolder, name);
        if (source is null || !File.Exists(source) || File.Exists(target) && !replace)
            return false;
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, overwrite: true);
        return true;
    }

    /// <summary>Re-reads config.yml after it was edited by hand.</summary>
    public void ReloadConfig() => Config.Reload();

    /// <summary>Adds a command from code (commands can also be declared in plugin.yml).</summary>
    public void RegisterCommand(string name, string description, string usage, bool opOnly, Action<CommandSender, string[]> execute) =>
        Server.Commands.Register(name, description, usage, opOnly, execute);
}
