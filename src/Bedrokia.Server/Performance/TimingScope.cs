using System.Diagnostics;

namespace Bedrokia.Server.Performance;

/// <summary>
/// Lightweight, allocation-free struct for measuring block execution time.
/// </summary>
public readonly struct TimingScope : IDisposable
{
    private readonly TimingRecord? _record;
    private readonly long _startTimestamp;

    public TimingScope(TimingRecord? record, bool isEnabled)
    {
        if (isEnabled && record is not null)
        {
            _record = record;
            _startTimestamp = Stopwatch.GetTimestamp();
        }
        else
        {
            _record = null;
            _startTimestamp = 0;
        }
    }

    public void Dispose()
    {
        if (_record is not null)
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _record.Record(elapsed);
        }
    }
}
