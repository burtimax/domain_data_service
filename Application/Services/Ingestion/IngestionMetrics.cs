using System.Threading;

namespace Application.Services.Ingestion;

public sealed class IngestionMetrics
{
    private readonly DateTimeOffset _startedAtUtc = DateTimeOffset.UtcNow;
    private long _received;
    private long _acked;
    private long _requeued;
    private long _retried;
    private long _deadLettered;
    private long _failed;
    private long _duplicates;
    private long _totalLagMs;
    private long _lagSamples;

    public void IncrementReceived() => Interlocked.Increment(ref _received);
    public void IncrementAcked() => Interlocked.Increment(ref _acked);
    public void IncrementRequeued() => Interlocked.Increment(ref _requeued);
    public void IncrementRetried() => Interlocked.Increment(ref _retried);
    public void IncrementDeadLettered() => Interlocked.Increment(ref _deadLettered);
    public void IncrementFailed() => Interlocked.Increment(ref _failed);
    public void IncrementDuplicates() => Interlocked.Increment(ref _duplicates);
    public void RecordLag(DateTimeOffset observedAtUtc)
    {
        var lagMs = (long)Math.Max(0, (DateTimeOffset.UtcNow - observedAtUtc).TotalMilliseconds);
        Interlocked.Add(ref _totalLagMs, lagMs);
        Interlocked.Increment(ref _lagSamples);
    }

    public IngestionMetricsSnapshot Snapshot()
    {
        var uptimeSeconds = Math.Max(1, (DateTimeOffset.UtcNow - _startedAtUtc).TotalSeconds);
        var acked = Interlocked.Read(ref _acked);
        var lagSamples = Interlocked.Read(ref _lagSamples);
        var avgLagMs = lagSamples == 0 ? 0 : Interlocked.Read(ref _totalLagMs) / (double)lagSamples;
        return new IngestionMetricsSnapshot(
            Received: Interlocked.Read(ref _received),
            Acked: acked,
            Requeued: Interlocked.Read(ref _requeued),
            Retried: Interlocked.Read(ref _retried),
            DeadLettered: Interlocked.Read(ref _deadLettered),
            Failed: Interlocked.Read(ref _failed),
            Duplicates: Interlocked.Read(ref _duplicates),
            ThroughputPerSecond: Math.Round(acked / uptimeSeconds, 4),
            AverageLagMs: Math.Round(avgLagMs, 2),
            UptimeSeconds: Math.Round(uptimeSeconds, 2));
    }
}

public sealed record IngestionMetricsSnapshot(
    long Received,
    long Acked,
    long Requeued,
    long Retried,
    long DeadLettered,
    long Failed,
    long Duplicates,
    double ThroughputPerSecond,
    double AverageLagMs,
    double UptimeSeconds);
