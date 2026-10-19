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

using Bedrokia.Server.Core;
using Bedrokia.Server.Plugins;

namespace Bedrokia.Tests;

public sealed class ServerSchedulerTests
{
    private sealed class DummyPlugin : Plugin
    {
    }

    [Fact]
    public void ScheduleDelayed_ExecutesAtExactTick()
    {
        var logger = new Logger(LogLevel.Error, null);
        var scheduler = new ServerScheduler(logger);
        var executed = false;

        scheduler.ScheduleDelayed(3, () => executed = true);

        scheduler.Tick(1);
        Assert.False(executed);
        scheduler.Tick(2);
        Assert.False(executed);
        scheduler.Tick(3);
        Assert.True(executed);
    }

    [Fact]
    public void RunNextTick_ExecutesOnFollowingTick()
    {
        var logger = new Logger(LogLevel.Error, null);
        var scheduler = new ServerScheduler(logger);
        var executed = false;

        scheduler.RunNextTick(() => executed = true);

        Assert.False(executed);
        scheduler.Tick(1);
        Assert.True(executed);
    }

    [Fact]
    public void ScheduleRepeating_ExecutesPeriodically_AndStopsAfterCancel()
    {
        var logger = new Logger(LogLevel.Error, null);
        var scheduler = new ServerScheduler(logger);
        var runCount = 0;

        var task = scheduler.ScheduleRepeating(2, 5, () => runCount++);

        scheduler.Tick(1);
        Assert.Equal(0, runCount);

        // First run at tick 2
        scheduler.Tick(2);
        Assert.Equal(1, runCount);

        // Not yet
        scheduler.Tick(3);
        scheduler.Tick(6);
        Assert.Equal(1, runCount);

        // Second run at tick 7 (2 + 5)
        scheduler.Tick(7);
        Assert.Equal(2, runCount);

        // Third run at tick 12 (7 + 5)
        scheduler.Tick(12);
        Assert.Equal(3, runCount);

        // Cancel the task
        task.Cancel();
        Assert.True(task.IsCancelled);

        // Should not run at tick 17
        scheduler.Tick(17);
        Assert.Equal(3, runCount);
    }

    [Fact]
    public void CancelAll_CancelsAllTasksOfPlugin()
    {
        var logger = new Logger(LogLevel.Error, null);
        var scheduler = new ServerScheduler(logger);
        var plugin1 = new DummyPlugin();
        var plugin2 = new DummyPlugin();

        var p1Count = 0;
        var p2Count = 0;

        scheduler.ScheduleDelayed(2, () => p1Count++, plugin1);
        scheduler.ScheduleRepeating(1, 2, () => p1Count++, plugin1);
        scheduler.ScheduleRepeating(1, 2, () => p2Count++, plugin2);

        scheduler.CancelAll(plugin1);

        scheduler.Tick(1);
        Assert.Equal(0, p1Count);
        Assert.Equal(1, p2Count);

        scheduler.Tick(2);
        Assert.Equal(0, p1Count);
        Assert.Equal(1, p2Count);

        scheduler.Tick(3);
        Assert.Equal(0, p1Count);
        Assert.Equal(2, p2Count);
    }

    [Fact]
    public void ExceptionIsolation_CrashedTaskDoesNotPreventOtherTasks()
    {
        var logger = new Logger(LogLevel.Error, null);
        var scheduler = new ServerScheduler(logger);
        var secondExecuted = false;

        scheduler.ScheduleDelayed(1, () => throw new InvalidOperationException("Task crashed!"));
        scheduler.ScheduleDelayed(1, () => secondExecuted = true);

        // Tick 1 executes both; the crash must be caught and logged, not rethrown
        scheduler.Tick(1);

        Assert.True(secondExecuted);
    }
}
