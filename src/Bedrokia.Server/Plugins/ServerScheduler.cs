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

namespace Bedrokia.Server.Plugins;

/// <summary>
/// A scheduled task managed by <see cref="ServerScheduler"/>.
/// </summary>
public sealed class ScheduledTask
{
    public long TaskId { get; }
    public Plugin? Owner { get; }
    public long NextRunTick { get; internal set; }
    public int PeriodTicks { get; }
    public bool IsCancelled { get; private set; }
    internal Action Action { get; }

    internal ScheduledTask(long taskId, Plugin? owner, long nextRunTick, int periodTicks, Action action)
    {
        TaskId = taskId;
        Owner = owner;
        NextRunTick = nextRunTick;
        PeriodTicks = periodTicks;
        Action = action;
    }

    /// <summary>Cancels this task so it will not run again.</summary>
    public void Cancel() => IsCancelled = true;
}

/// <summary>
/// Deterministic, tick-safe task scheduler running on the single-threaded 20 Hz game loop.
/// Exceptions thrown in scheduled actions are isolated and logged without crashing the main loop.
/// </summary>
public sealed class ServerScheduler(Logger logger)
{
    private long _nextTaskId;
    private long _currentTick;
    private readonly SortedDictionary<long, List<ScheduledTask>> _tasksByTick = [];
    private readonly Dictionary<long, ScheduledTask> _tasksById = [];

    /// <summary>Current tick observed by the scheduler.</summary>
    public long CurrentTick => _currentTick;

    /// <summary>Schedules an action to run on the very next tick.</summary>
    public ScheduledTask RunNextTick(Action action, Plugin? owner = null) =>
        ScheduleDelayed(1, action, owner);

    /// <summary>Schedules an action to run after <paramref name="delayTicks"/> ticks.</summary>
    public ScheduledTask ScheduleDelayed(int delayTicks, Action action, Plugin? owner = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        var runTick = _currentTick + Math.Max(1, delayTicks);
        var taskId = Interlocked.Increment(ref _nextTaskId);
        var task = new ScheduledTask(taskId, owner, runTick, 0, action);

        AddTask(task);
        return task;
    }

    /// <summary>Schedules an action to run repeatedly every <paramref name="periodTicks"/> after an initial delay.</summary>
    public ScheduledTask ScheduleRepeating(int delayTicks, int periodTicks, Action action, Plugin? owner = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (periodTicks < 1)
            throw new ArgumentOutOfRangeException(nameof(periodTicks), "Period ticks must be at least 1.");

        var runTick = _currentTick + Math.Max(1, delayTicks);
        var taskId = Interlocked.Increment(ref _nextTaskId);
        var task = new ScheduledTask(taskId, owner, runTick, periodTicks, action);

        AddTask(task);
        return task;
    }

    /// <summary>Cancels the task with the given ID.</summary>
    public void CancelTask(long taskId)
    {
        if (_tasksById.TryGetValue(taskId, out var task))
            task.Cancel();
    }

    /// <summary>Cancels all tasks owned by the specified plugin.</summary>
    public void CancelAll(Plugin owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        foreach (var task in _tasksById.Values)
        {
            if (task.Owner == owner)
                task.Cancel();
        }
    }

    private void AddTask(ScheduledTask task)
    {
        _tasksById[task.TaskId] = task;
        if (!_tasksByTick.TryGetValue(task.NextRunTick, out var list))
        {
            list = [];
            _tasksByTick[task.NextRunTick] = list;
        }
        list.Add(task);
    }

    /// <summary>Advances the scheduler to <paramref name="currentTick"/> and executes due tasks.</summary>
    public void Tick(long currentTick)
    {
        _currentTick = currentTick;

        while (_tasksByTick.Count > 0 && _tasksByTick.First().Key <= currentTick)
        {
            var (tick, tasks) = _tasksByTick.First();
            _tasksByTick.Remove(tick);

            foreach (var task in tasks)
            {
                if (task.IsCancelled)
                {
                    _tasksById.Remove(task.TaskId);
                    continue;
                }

                try
                {
                    task.Action();
                }
                catch (Exception ex)
                {
                    var ownerName = task.Owner?.Name ?? "Server";
                    logger.Error($"Scheduled task #{task.TaskId} (owner: '{ownerName}') crashed: {ex}");
                }

                if (task.PeriodTicks > 0 && !task.IsCancelled)
                {
                    task.NextRunTick = currentTick + task.PeriodTicks;
                    AddTask(task);
                }
                else
                {
                    _tasksById.Remove(task.TaskId);
                }
            }
        }
    }
}
