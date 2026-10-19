namespace Bedrokia.Server.Performance;

/// <summary>
/// Immutable snapshot of server performance metrics at a point in time.
/// </summary>
public sealed record PerformanceSnapshot(
    double CurrentTps,
    double AverageTps1Min,
    double AverageTps5Min,
    double CurrentTickMs,
    double AverageTickMs,
    double TickUsage,
    long GcTotalMemory,
    long ProcessWorkingSet,
    DateTimeOffset Timestamp
);
