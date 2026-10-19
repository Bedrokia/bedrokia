namespace Bedrokia.Server.Performance;

/// <summary>
/// Tracks server tick performance (TPS, tick execution time MSPT) and memory consumption.
/// Uses Exponential Moving Averages (EMA) matching standard Minecraft/Paper server metrics.
/// </summary>
public sealed class PerformanceTracker
{
    public const double TargetTickDurationMs = 50.0;
    public const double TargetTps = 20.0;

    // EMA smoothing weights for 1m (1200 ticks) and 5m (6000 ticks)
    private static readonly double EmaWeight1Min = 1.0 - Math.Exp(-1.0 / (60.0 * TargetTps));
    private static readonly double EmaWeight5Min = 1.0 - Math.Exp(-1.0 / (300.0 * TargetTps));

    private const int HistorySize = 100;
    private readonly double[] _tickTimeHistory = new double[HistorySize];
    private int _historyIndex;
    private int _historyCount;
    private double _tickTimeSum;

    public double CurrentTps { get; private set; } = TargetTps;
    public double AverageTps1Min { get; private set; } = TargetTps;
    public double AverageTps5Min { get; private set; } = TargetTps;
    public double CurrentTickMs { get; private set; }
    public double AverageTickMs { get; private set; }
    public double TickLoadPercentage => (AverageTickMs / TargetTickDurationMs) * 100.0;

    public long GcTotalMemory => GC.GetTotalMemory(forceFullCollection: false);
    public long ProcessWorkingSet => Environment.WorkingSet;

    /// <summary>
    /// Records the execution of a server tick.
    /// </summary>
    /// <param name="elapsedMs">Time spent executing the tick in milliseconds.</param>
    /// <param name="intervalMs">Real-world elapsed time since previous tick started in milliseconds.</param>
    public void OnTick(double elapsedMs, double intervalMs)
    {
        CurrentTickMs = elapsedMs;

        // Maintain moving average of MSPT (Milliseconds Per Tick)
        if (_historyCount < HistorySize)
        {
            _tickTimeHistory[_historyIndex] = elapsedMs;
            _tickTimeSum += elapsedMs;
            _historyCount++;
            _historyIndex = (_historyIndex + 1) % HistorySize;
        }
        else
        {
            _tickTimeSum -= _tickTimeHistory[_historyIndex];
            _tickTimeHistory[_historyIndex] = elapsedMs;
            _tickTimeSum += elapsedMs;
            _historyIndex = (_historyIndex + 1) % HistorySize;
        }

        AverageTickMs = _historyCount > 0 ? _tickTimeSum / _historyCount : elapsedMs;

        // Calculate real-time TPS based on interval (or tick execution if synthetic)
        double sampleInterval = intervalMs > 0 ? intervalMs : Math.Max(TargetTickDurationMs, elapsedMs);
        double instantTps = Math.Clamp(1000.0 / Math.Max(TargetTickDurationMs, sampleInterval), 0.0, TargetTps);

        CurrentTps = instantTps;
        AverageTps1Min += (instantTps - AverageTps1Min) * EmaWeight1Min;
        AverageTps5Min += (instantTps - AverageTps5Min) * EmaWeight5Min;
    }

    /// <summary>
    /// Captures a point-in-time snapshot of performance metrics.
    /// </summary>
    public PerformanceSnapshot GetSnapshot() =>
        new(
            CurrentTps: CurrentTps,
            AverageTps1Min: AverageTps1Min,
            AverageTps5Min: AverageTps5Min,
            CurrentTickMs: CurrentTickMs,
            AverageTickMs: AverageTickMs,
            TickUsage: TickLoadPercentage,
            GcTotalMemory: GcTotalMemory,
            ProcessWorkingSet: ProcessWorkingSet,
            Timestamp: DateTimeOffset.UtcNow
        );

    /// <summary>Formats TPS with Minecraft color code (§a green, §e yellow, §c red).</summary>
    public static string FormatTps(double tps)
    {
        var color = tps >= 19.5 ? "§a" : tps >= 15.0 ? "§e" : "§c";
        return $"{color}{tps:F1}";
    }

    /// <summary>Formats tick execution duration with Minecraft color code.</summary>
    public static string FormatMspt(double ms)
    {
        var color = ms <= 25.0 ? "§a" : ms <= 45.0 ? "§e" : "§c";
        return $"{color}{ms:F1}ms";
    }

    /// <summary>Formats a byte count into MB or GB human-readable representation.</summary>
    public static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024L * 1024L)
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
