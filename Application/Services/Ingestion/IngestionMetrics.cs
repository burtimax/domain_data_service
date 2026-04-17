using System.Threading;

namespace Application.Services.Ingestion;

public sealed class IngestionMetrics
{
    private long _received;
    private long _acked;
    private long _requeued;
    private long _deadLettered;
    private long _failed;

    public void IncrementReceived() => Interlocked.Increment(ref _received);
    public void IncrementAcked() => Interlocked.Increment(ref _acked);
    public void IncrementRequeued() => Interlocked.Increment(ref _requeued);
    public void IncrementDeadLettered() => Interlocked.Increment(ref _deadLettered);
    public void IncrementFailed() => Interlocked.Increment(ref _failed);

    public IngestionMetricsSnapshot Snapshot() => new(
        Interlocked.Read(ref _received),
        Interlocked.Read(ref _acked),
        Interlocked.Read(ref _requeued),
        Interlocked.Read(ref _deadLettered),
        Interlocked.Read(ref _failed));
}

public sealed record IngestionMetricsSnapshot(
    long Received,
    long Acked,
    long Requeued,
    long DeadLettered,
    long Failed);
