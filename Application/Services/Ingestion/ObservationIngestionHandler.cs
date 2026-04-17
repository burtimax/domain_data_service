using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Ingestion;

namespace Application.Services.Ingestion;

public sealed class ObservationIngestionHandler : IObservationIngestionHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ObservationIngestionHandler> _logger;

    public ObservationIngestionHandler(
        AppDbContext dbContext,
        ILogger<ObservationIngestionHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(
        ObservationIngestionMessage message,
        IngestionSource source,
        CancellationToken cancellationToken)
    {
        Validate(message);

        var isDuplicate = await _dbContext.AuctionObservations
            .AsNoTracking()
            .AnyAsync(x => x.MessageId == message.MessageId, cancellationToken);

        if (isDuplicate)
        {
            _logger.LogInformation("Duplicate ingestion message skipped: {MessageId}", message.MessageId);
            await SaveProcessingLog(message, source, "DUPLICATE_MESSAGE_ID", "Message already processed.", cancellationToken);
            return;
        }

        await SaveProcessingLog(message, source, "ACCEPTED", null, cancellationToken);
        _logger.LogInformation(
            "Observation message accepted: {MessageId}, source={Source}, platform={Platform}, externalAuctionId={ExternalAuctionId}",
            message.MessageId,
            source,
            message.SourcePlatformCode,
            message.ExternalAuctionId);
    }

    private async Task SaveProcessingLog(
        ObservationIngestionMessage message,
        IngestionSource source,
        string resultCode,
        string? details,
        CancellationToken cancellationToken)
    {
        _dbContext.ReceiverProcessingLogs.Add(new ReceiverProcessingLogEntity
        {
            MessageId = message.MessageId,
            ExternalAuctionId = message.ExternalAuctionId,
            SourcePlatformCode = message.SourcePlatformCode,
            ResultCode = resultCode,
            Message = details,
            TraceId = source.ToString(),
            ProcessedAt = DateTimeOffset.UtcNow,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(ObservationIngestionMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.MessageId))
            throw new NonRetryableIngestionException("MessageId is required.");

        if (string.IsNullOrWhiteSpace(message.SchemaVersion))
            throw new NonRetryableIngestionException("SchemaVersion is required.");

        if (!message.SchemaVersion.StartsWith("1", StringComparison.Ordinal))
            throw new NonRetryableIngestionException("SchemaVersion is unsupported.");

        if (string.IsNullOrWhiteSpace(message.SourcePlatformCode))
            throw new NonRetryableIngestionException("SourcePlatformCode is required.");

        if (string.IsNullOrWhiteSpace(message.ExternalAuctionId))
            throw new NonRetryableIngestionException("ExternalAuctionId is required.");

        if (string.IsNullOrWhiteSpace(message.DomainNameOriginal))
            throw new NonRetryableIngestionException("DomainNameOriginal is required.");

        if (message.ObservedAt == default)
            throw new NonRetryableIngestionException("ObservedAt is required.");
    }
}
