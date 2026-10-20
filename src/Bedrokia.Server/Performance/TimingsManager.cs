using System.Collections.Concurrent;
using System.Text;

namespace Bedrokia.Server.Performance;

/// <summary>
/// Profiling manager for measuring tick component execution times, inspired by PocketMine-MP and PaperMC timings.
/// Allows detecting performance bottlenecks across entities, physics, world saving, network and plugins.
/// </summary>
public sealed class TimingsManager
{
    private readonly ConcurrentDictionary<string, TimingRecord> _records = new(StringComparer.OrdinalIgnoreCase);
    private long _sampledTicks;

    public bool IsEnabled { get; set; }
    public long SampledTicks => Volatile.Read(ref _sampledTicks);
    public DateTimeOffset? StartTime { get; private set; }

    // Core built-in timing records
    public TimingRecord FullTick { get; }
    public TimingRecord DroppedItems { get; }
    public TimingRecord Mobs { get; }
    public TimingRecord FallingBlocks { get; }
    public TimingRecord Furnaces { get; }
    public TimingRecord Blocks { get; }
    public TimingRecord Events { get; }
    public TimingRecord Players { get; }
    public TimingRecord World { get; }
    public TimingRecord MainThread { get; }

    public TimingsManager()
    {
        FullTick = RegisterRecord("Full Server Tick", "Core");
        DroppedItems = RegisterRecord("Dropped Items", "World");
        Mobs = RegisterRecord("Mobs & AI", "Entities");
        FallingBlocks = RegisterRecord("Falling Blocks", "World");
        Furnaces = RegisterRecord("Furnaces", "World");
        Blocks = RegisterRecord("Blocks & Liquids", "World");
        Events = RegisterRecord("Events", "Plugins");
        Players = RegisterRecord("Players Health", "Players");
        World = RegisterRecord("World Save & Unload", "World");
        MainThread = RegisterRecord("Main Thread Tasks", "Core");
    }

    private TimingRecord RegisterRecord(string name, string group)
    {
        var record = new TimingRecord(name, group);
        _records[name] = record;
        return record;
    }

    /// <summary>
    /// Gets an existing timing record or creates a new one (e.g. for plugins or custom tasks).
    /// </summary>
    public TimingRecord GetOrCreate(string name, string? group = null) =>
        _records.GetOrAdd(name, n => new TimingRecord(n, group));

    /// <summary>
    /// Creates a timing measurement scope for the given record. Zero-allocation.
    /// </summary>
    public TimingScope Track(TimingRecord? record) => new(record, IsEnabled);

    /// <summary>
    /// Starts or resumes timings collection.
    /// </summary>
    public void Start()
    {
        IsEnabled = true;
        StartTime ??= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Pauses timings collection without clearing collected data.
    /// </summary>
    public void Stop()
    {
        IsEnabled = false;
    }

    /// <summary>
    /// Clears all collected timings data and resets tick counters.
    /// </summary>
    public void Reset()
    {
        Volatile.Write(ref _sampledTicks, 0);
        foreach (var record in _records.Values)
            record.Reset();
        StartTime = IsEnabled ? DateTimeOffset.UtcNow : null;
    }

    /// <summary>
    /// Called at the end of each server tick to update sampling count.
    /// </summary>
    public void OnTickEnd()
    {
        if (IsEnabled)
            Interlocked.Increment(ref _sampledTicks);
    }

    /// <summary>
    /// Generates a human-readable performance report formatted for Minecraft chat and console.
    /// </summary>
    public string GenerateReport()
    {
        long ticks = SampledTicks;
        double elapsedSec = ticks * (1.0 / PerformanceTracker.TargetTps);
        if (StartTime is { } start && ticks > 0)
        {
            var wallTime = (DateTimeOffset.UtcNow - start).TotalSeconds;
            if (wallTime > 0)
                elapsedSec = Math.Min(elapsedSec, wallTime);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"§6--- Server Timings (Sampled: {ticks} ticks, {elapsedSec:F1}s) ---");

        double fullTickAvg = FullTick.AverageMsPerTick(ticks);
        double fullTickPeak = FullTick.PeakMs;
        double loadPct = (fullTickAvg / PerformanceTracker.TargetTickDurationMs) * 100.0;
        string loadColor = loadPct <= 50.0 ? "§a" : loadPct <= 90.0 ? "§e" : "§c";

        sb.AppendLine($"§7Total Tick Time: {loadColor}{fullTickAvg:F2}ms §7avg (max {fullTickPeak:F2}ms, {loadColor}{loadPct:F1}%§7 budget)");
        sb.AppendLine("§eSubsystems Breakdown:");

        var breakdown = _records.Values
            .Where(r => r != FullTick && r.Count > 0 && r.Group?.StartsWith(PluginGroup) != true)
            .OrderByDescending(r => r.TotalMs)
            .ToList();

        if (breakdown.Count == 0)
        {
            sb.AppendLine("§7- (No subsystem activity recorded yet)");
        }
        else
        {
            foreach (var record in breakdown)
            {
                double avgMs = record.AverageMsPerTick(ticks);
                double pctOfTick = fullTickAvg > 0 ? (avgMs / fullTickAvg) * 100.0 : 0.0;
                double peakMs = record.PeakMs;
                string itemColor = pctOfTick > 40.0 ? "§c" : pctOfTick > 20.0 ? "§e" : "§f";

                sb.AppendLine($"§7- {record.Name}: {itemColor}{avgMs:F2}ms §7({pctOfTick:F1}%) [peak: {peakMs:F2}ms, count: {record.Count}]");
            }
        }

        // Per plugin: its events and scheduled tasks, the heaviest first (also outside the tick: packet events).
        var plugins = _records.Values.Where(r => r.Count > 0 && r.Group?.StartsWith(PluginGroup) == true)
            .GroupBy(r => r.Group![PluginGroup.Length..])
            .Select(g => (Name: g.Key, Ms: g.Sum(r => r.TotalMs), Top: g.OrderByDescending(r => r.TotalMs).Take(3).ToList()))
            .OrderByDescending(p => p.Ms)
            .ToList();
        if (plugins.Count > 0)
        {
            sb.AppendLine("§ePlugins:");
            foreach (var (name, ms, top) in plugins)
            {
                var avg = ticks > 0 ? ms / ticks : 0;
                var color = avg > 5 ? "§c" : avg > 1 ? "§e" : "§f";
                sb.AppendLine($"§7- §b{name}§7: {color}{avg:F2}ms§7/tick");
                foreach (var record in top)
                    sb.AppendLine($"§8    {record.Name[(name.Length + 2)..]}: {record.AverageMsPerTick(ticks):F2}ms/tick, {record.Count} calls, peak {record.PeakMs:F2}ms");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private const string PluginGroup = "Plugin:";
}
