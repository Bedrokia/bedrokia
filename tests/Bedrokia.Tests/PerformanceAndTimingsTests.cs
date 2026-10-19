using Bedrokia.Server;
using Bedrokia.Server.Commands;
using Bedrokia.Server.Core;
using Bedrokia.Server.Performance;
using Xunit;

namespace Bedrokia.Tests;

public class PerformanceAndTimingsTests
{
    private static BedrokiaServer CreateTestServer()
    {
        var tempWorlds = TestWorlds.NewDirectory();
        var config = new ServerConfig { WorldsDirectory = tempWorlds, Port = 0, MaxPlayers = 5 };
        var logger = new Logger(LogLevel.Warn, null);
        return new BedrokiaServer(config, logger);
    }

    [Fact]
    public void PerformanceTracker_initializes_with_20_tps()
    {
        var tracker = new PerformanceTracker();
        Assert.Equal(20.0, tracker.CurrentTps);
        Assert.Equal(20.0, tracker.AverageTps1Min);
        Assert.Equal(20.0, tracker.AverageTps5Min);
        Assert.Equal(0.0, tracker.AverageTickMs);
    }

    [Fact]
    public void PerformanceTracker_updates_tps_and_mspt_on_ticks()
    {
        var tracker = new PerformanceTracker();

        // Simulate 20 ticks taking 5ms each, spaced by 50ms (20 TPS)
        for (var i = 0; i < 20; i++)
            tracker.OnTick(elapsedMs: 5.0, intervalMs: 50.0);

        Assert.Equal(20.0, tracker.CurrentTps);
        Assert.InRange(tracker.AverageTickMs, 4.9, 5.1);
        Assert.InRange(tracker.TickLoadPercentage, 9.8, 10.2);

        // Simulate lag: 100ms interval (10 TPS)
        tracker.OnTick(elapsedMs: 100.0, intervalMs: 100.0);
        Assert.Equal(10.0, tracker.CurrentTps);
        Assert.True(tracker.AverageTps1Min < 20.0);
    }

    [Fact]
    public void PerformanceTracker_snapshot_contains_memory_and_stats()
    {
        var tracker = new PerformanceTracker();
        tracker.OnTick(2.5, 50.0);

        var snap = tracker.GetSnapshot();
        Assert.Equal(20.0, snap.CurrentTps);
        Assert.InRange(snap.AverageTickMs, 2.4, 2.6);
        Assert.True(snap.GcTotalMemory > 0);
        Assert.True(snap.ProcessWorkingSet > 0);
    }

    [Fact]
    public void TimingsManager_records_subsystem_timings_when_enabled()
    {
        var timings = new TimingsManager();
        Assert.False(timings.IsEnabled);

        // Tracking when disabled does nothing
        using (timings.Track(timings.Mobs))
        {
            Thread.Sleep(2);
        }
        Assert.Equal(0, timings.Mobs.Count);

        // Enable timings
        timings.Start();
        Assert.True(timings.IsEnabled);

        using (timings.Track(timings.Mobs))
        {
            Thread.Sleep(5);
        }

        Assert.Equal(1, timings.Mobs.Count);
        Assert.True(timings.Mobs.TotalTime.TotalMilliseconds > 0);

        // Custom timer for plugins
        var custom = timings.GetOrCreate("Plugin:TestPlugin");
        using (timings.Track(custom))
        {
            Thread.Sleep(2);
        }
        Assert.Equal(1, custom.Count);

        // Tick end updates sampled ticks
        timings.OnTickEnd();
        Assert.Equal(1, timings.SampledTicks);

        var report = timings.GenerateReport();
        Assert.Contains("Server Timings", report);
        Assert.Contains("Mobs & AI", report);
        Assert.Contains("Plugin:TestPlugin", report);

        // Reset
        timings.Reset();
        Assert.Equal(0, timings.SampledTicks);
        Assert.Equal(0, timings.Mobs.Count);
    }

    [Fact]
    public void Commands_tps_and_timings_are_registered_with_permissions()
    {
        var server = CreateTestServer();

        var tpsCmd = server.Commands.All.FirstOrDefault(c => c.Name.Equals("tps", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(tpsCmd);
        Assert.Equal("bedrokia.command.tps", tpsCmd.Permission);

        var timingsCmd = server.Commands.All.FirstOrDefault(c => c.Name.Equals("timings", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(timingsCmd);
        Assert.Equal("bedrokia.command.timings", timingsCmd.Permission);
    }

    [Fact]
    public void Tps_command_dispatches_cleanly()
    {
        var server = CreateTestServer();
        var replies = new List<string>();
        var console = new CommandSender("Server", isOp: true, replies.Add);

        server.Commands.Dispatch(console, "tps");

        Assert.NotEmpty(replies);
        var combined = string.Join('\n', replies);
        Assert.Contains("Server Performance", combined);
        Assert.Contains("TPS:", combined);
        Assert.Contains("Tick Time:", combined);
        Assert.Contains("Memory:", combined);
    }

    [Fact]
    public void Timings_command_workflow_works()
    {
        var server = CreateTestServer();
        var replies = new List<string>();
        var console = new CommandSender("Server", isOp: true, replies.Add);

        // Disabled by default
        server.Commands.Dispatch(console, "timings");
        Assert.Contains(replies, r => r.Contains("disabled") || r.Contains("off"));

        replies.Clear();
        server.Commands.Dispatch(console, "timings on");
        Assert.Contains(replies, r => r.Contains("enabled"));
        Assert.True(server.Timings.IsEnabled);

        // Tick the server once to generate sample data
        server.Tick();

        replies.Clear();
        server.Commands.Dispatch(console, "timings report");
        var report = string.Join('\n', replies);
        Assert.Contains("Server Timings", report);

        replies.Clear();
        server.Commands.Dispatch(console, "timings off");
        Assert.Contains(replies, r => r.Contains("stopped"));
        Assert.False(server.Timings.IsEnabled);

        replies.Clear();
        server.Commands.Dispatch(console, "timings reset");
        Assert.Contains(replies, r => r.Contains("reset"));
        Assert.Equal(0, server.Timings.SampledTicks);
    }
}
