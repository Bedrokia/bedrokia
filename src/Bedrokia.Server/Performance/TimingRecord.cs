namespace Bedrokia.Server.Performance;

/// <summary>
/// Profiles execution duration and invocation counts for a server subsystem or plugin.
/// </summary>
public sealed class TimingRecord(string name, string? group = null)
{
    private long _totalTicks;
    private long _peakTicks;
    private long _count;

    public string Name { get; } = name;
    public string? Group { get; } = group;

    public long Count => Volatile.Read(ref _count);
    public TimeSpan TotalTime => TimeSpan.FromTicks(Volatile.Read(ref _totalTicks));
    public TimeSpan PeakTime => TimeSpan.FromTicks(Volatile.Read(ref _peakTicks));

    public double TotalMs => TotalTime.TotalMilliseconds;
    public double PeakMs => PeakTime.TotalMilliseconds;

    /// <summary>
    /// Average milliseconds per tick over the sampled ticks.
    /// </summary>
    public double AverageMsPerTick(long sampledTicks) =>
        sampledTicks > 0 ? TotalMs / sampledTicks : 0.0;

    /// <summary>
    /// Average milliseconds per execution call.
    /// </summary>
    public double AverageMsPerCall =>
        _count > 0 ? TotalMs / _count : 0.0;

    internal void Record(TimeSpan duration)
    {
        Interlocked.Increment(ref _count);
        Interlocked.Add(ref _totalTicks, duration.Ticks);

        long currentPeak;
        do
        {
            currentPeak = Volatile.Read(ref _peakTicks);
            if (duration.Ticks <= currentPeak)
                break;
        } while (Interlocked.CompareExchange(ref _peakTicks, duration.Ticks, currentPeak) != currentPeak);
    }

    internal void Reset()
    {
        Volatile.Write(ref _count, 0);
        Volatile.Write(ref _totalTicks, 0);
        Volatile.Write(ref _peakTicks, 0);
    }
}
