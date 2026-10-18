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
using System.Net;
using System.Net.Sockets;
using Bedrokia.Server;
using Bedrokia.Server.Core;

namespace Bedrokia.Tests;

public class BedrokiaServerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Starts_on_one_port_then_stops_on_command()
    {
        using var logger = new Logger(LogLevel.Warn, null);
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Seed = 42, Port = 0, MaxPlayers = 5 }, logger);
        var run = server.RunAsync();
        await server.Started.WaitAsync(Timeout);
        Assert.NotNull(server.NetherNetEndPoint);

        server.EnqueueCommand("stop");
        await run.WaitAsync(Timeout);
        Assert.True(server.CurrentTick > 0);
    }

    [Fact]
    public async Task Fails_to_start_when_port_is_taken()
    {
        using var logger = new Logger(LogLevel.Error, null);
        using var taken = new TcpListener(IPAddress.Any, 0);
        taken.Start();
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Seed = 42, Port = ((IPEndPoint)taken.LocalEndpoint).Port }, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => server.RunAsync().WaitAsync(Timeout));
        await Assert.ThrowsAnyAsync<Exception>(() => server.Started.WaitAsync(Timeout));
    }

    [Fact]
    public void Auto_webrtc_ports_start_at_the_server_port()
    {
        var config = new ServerConfig { Port = 19132, MaxPlayers = 20 };
        Assert.Equal((19132, 19156), config.WebRtcPortRange());
    }

    [Fact]
    public void Dispatch_ignores_bom_slash_and_case()
    {
        using var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig(), logger);
        string[]? received = null;
        server.Commands.Register("echo", "test", args => received = args);

        server.Commands.Dispatch((char)0xFEFF + "/ECHO  a b ");

        Assert.Equal(["a", "b"], received!);
    }

    [Fact]
    public void Unknown_and_failing_commands_do_not_throw()
    {
        using var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig(), logger);
        server.Commands.Register("boom", "throws", _ => throw new InvalidOperationException());

        server.Commands.Dispatch("does-not-exist");
        server.Commands.Dispatch("/boom arg");
        server.Commands.Dispatch("   ");
        Assert.Throws<InvalidOperationException>(() => server.Commands.Register("HELP", "duplicate", _ => { }));
    }

    [Fact]
    public void Non_operators_cannot_run_op_commands_and_see_only_public_ones()
    {
        using var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig(), logger);
        var replies = new List<string>();
        var player = new CommandSender("Bob", false, replies.Add);

        server.Commands.Dispatch(player, "/stop");
        server.Commands.Dispatch(player, "gamemode creative");
        server.Commands.Dispatch(new CommandSender("Op", true, replies.Add), "time set");

        Assert.Contains("permission", replies[0]);
        Assert.Contains("permission", replies[1]);
        Assert.Contains("Usage: time", replies[2]);
        Assert.True(server.Commands.AvailableCommandsPacket(true).Length > server.Commands.AvailableCommandsPacket(false).Length);
    }
}
