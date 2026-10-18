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


using Bedrokia.Server;
using Bedrokia.Server.Core;

namespace Bedrokia.Tests;

public sealed class PluginTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("bedrokia-plugins").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Source_plugins_compile_at_startup_and_broken_ones_are_reported()
    {
        var plugins = Directory.CreateDirectory(Path.Combine(_dir, "plugins")).FullName;
        File.WriteAllText(Path.Combine(plugins, "Hello.cs"), """
            public sealed class Hello : Plugin
            {
                public override string Description => "says hello";
                public override void OnEnable() =>
                    RegisterCommand("hello", "Says hello.", "", false, (sender, _) => sender.Reply($"Hello {sender.Name} from {Name}"));
            }
            """);
        File.WriteAllText(Path.Combine(plugins, "Broken.cs"), "public sealed class Broken : Plugin { int x = \"no\"; }");
        File.WriteAllText(Path.Combine(plugins, "Crash.cs"), "public sealed class Crash : Plugin { public override void OnEnable() => throw new InvalidOperationException(\"boom\"); }");
        var greeter = Directory.CreateDirectory(Path.Combine(plugins, "Greeter")).FullName;
        File.WriteAllText(Path.Combine(greeter, "plugin.yml"), """
            name: Greeter
            version: 2.1.0
            main: Greeter.Main
            author: Tester
            depend: [Hello]
            commands:
              greet:
                description: Greets.
                usage: "<name>"
                permission: everyone
                aliases: [hey]
            """);
        Directory.CreateDirectory(Path.Combine(greeter, "src", "Commands"));
        File.WriteAllText(Path.Combine(greeter, "src", "Commands", "Main.cs"), """
            namespace Greeter;
            public sealed class Main : Plugin
            {
                public override bool OnCommand(CommandSender sender, string command, string[] args)
                {
                    if (args.Length == 0)
                        return false;
                    sender.Reply(Config.GetString("greeting", "?").Replace("{name}", args[0]) + " x" + Config.GetInt("times"));
                    return true;
                }
            }
            """);
        Directory.CreateDirectory(Path.Combine(greeter, "resources"));
        File.WriteAllText(Path.Combine(greeter, "resources", "config.yml"), "greeting: \"Hi {name}\"\ntimes: 3\n");
        var orphan = Directory.CreateDirectory(Path.Combine(plugins, "Orphan")).FullName;
        File.WriteAllText(Path.Combine(orphan, "plugin.yml"), "name: Orphan\nmain: Orphan.Main\ndepend: [Nowhere]\n");

        using var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Port = 0, OnlineMode = false }, logger)
        {
            PluginsDirectory = plugins,
            PluginDataDirectory = Path.Combine(_dir, "plugin_data"),
        };
        server.Plugins.LoadAll(server.PluginsDirectory, server.PluginDataDirectory);

        Assert.Equal(["Hello", "Greeter"], server.Plugins.All.Select(p => p.Name)); // dependency first
        Assert.Equal("2.1.0", server.Plugins.Get("Greeter")!.Version);
        Assert.True(File.Exists(Path.Combine(_dir, "plugin_data", "Greeter", "config.yml")));
        Assert.True(Directory.Exists(Path.Combine(_dir, "plugin_data", "Hello")));
        Assert.Equal(["Broken.cs", "Crash.cs", "Orphan"], server.Plugins.Failed.Select(f => Path.GetFileName(f.File)).Order());
        Assert.Contains("CS0029", server.Plugins.Failed.Single(f => f.File.EndsWith("Broken.cs")).Error);
        Assert.Contains("boom", server.Plugins.Failed.Single(f => f.File.EndsWith("Crash.cs")).Error);

        var replies = new List<string>();
        server.Commands.Dispatch(new Bedrokia.Server.Commands.CommandSender("Steve", false, replies.Add), "hello");
        Assert.Equal(["Hello Steve from Hello"], replies);
        replies.Clear();
        server.Commands.Dispatch(new Bedrokia.Server.Commands.CommandSender("Steve", false, replies.Add), "hey Alex");
        server.Commands.Dispatch(new Bedrokia.Server.Commands.CommandSender("Steve", false, replies.Add), "greet");
        Assert.Equal(["Hi Alex x3", "§cUsage: /greet <name>"], replies);
    }
}
