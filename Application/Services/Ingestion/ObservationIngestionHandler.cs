using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Application.Models.Ingestion;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Ingestion;

namespace Application.Services.Ingestion;

public sealed class ObservationIngestionHandler : IObservationIngestionHandler
{
    private static readonly Dictionary<string, int> TerminalPrecedence = new(StringComparer.Ordinal)
    {
        ["sold"] = 4,
        ["not_sold"] = 3,
        ["cancelled"] = 2,
        ["disappeared"] = 1,
    };

    private const int SemanticDedupeWindowMinutes = 5;

    private readonly AppDbContext _dbContext;
    private readonly ILogger<ObservationIngestionHandler> _logger;
    private readonly IngestionMetrics _metrics;

    public ObservationIngestionHandler(
        AppDbContext dbContext,
        ILogger<ObservationIngestionHandler> logger,
        IngestionMetrics metrics)
    {
        _dbContext = dbContext;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task HandleAsync(
        ObservationIngestionMessage message,
        IngestionSource source,
        CancellationToken cancellationToken)
    {
        ValidateSchema(message);
        ValidateBusiness(message);
        var normalized = Normalize(message);

        var isDuplicate = await _dbContext.AuctionObservations
            .AsNoTracking()
            .AnyAsync(x => x.MessageId == normalized.MessageId, cancellationToken);

        if (isDuplicate)
        {
            _logger.LogInformation("Duplicate ingestion message skipped: {MessageId}", normalized.MessageId);
            _metrics.IncrementDuplicates();
            await SaveProcessingLog(normalized, source, "DUPLICATE_MESSAGE_ID", "Message already processed.", cancellationToken);
            return;
        }

        var sourcePlatform = await GetOrCreateSourcePlatform(normalized, cancellationToken);
        var domain = await GetOrCreateDomain(normalized, cancellationToken);
        var auction = await GetOrCreateAuction(normalized, domain.Id, sourcePlatform.Id, cancellationToken);

        var semanticDuplicate = await IsSemanticDuplicateAsync(normalized, auction.Id, cancellationToken);
        if (semanticDuplicate)
        {
            _metrics.IncrementDuplicates();
            await SaveProcessingLog(normalized, source, "SEMANTIC_DUPLICATE", "Semantic duplicate in dedupe window.", cancellationToken);
            return;
        }

        var isOutOfOrder = auction.LastObservedAt.HasValue && normalized.ObservedAtUtc < auction.LastObservedAt.Value;
        var shouldUpdateSnapshot = ShouldUpdateSnapshot(auction, normalized, isOutOfOrder);

        _dbContext.AuctionObservations.Add(new AuctionObservationEntity
        {
            AuctionId = auction.Id,
            MessageId = normalized.MessageId,
            SchemaVersion = normalized.SchemaVersion,
            ParserCode = normalized.ParserCode,
            ParserVersion = normalized.ParserVersion,
            ObservedAt = normalized.ObservedAtUtc,
            AuctionStatusRaw = normalized.AuctionStatusRaw,
            AuctionStatusNormalized = normalized.AuctionStatusNormalized,
            CurrentPrice = normalized.CurrentPrice,
            FinalPrice = normalized.FinalPrice,
            CurrencyCode = normalized.CurrencyCode,
            RawPayload = normalized.RawPayload,
            AttributesJson = normalized.AttributesJson,
            IsOutOfOrder = isOutOfOrder,
        });

        if (shouldUpdateSnapshot)
        {
            var previousStatus = auction.Status;
            ApplyAuctionSnapshot(auction, normalized);

            if (!string.Equals(previousStatus, auction.Status, StringComparison.Ordinal))
            {
                _dbContext.AuctionStatusHistory.Add(new AuctionStatusHistoryEntity
                {
                    AuctionId = auction.Id,
                    PreviousStatus = previousStatus,
                    NewStatus = auction.Status,
                    IsTerminal = IsTerminal(auction.Status),
                    ObservedAt = normalized.ObservedAtUtc,
                    Source = "observation",
                });
            }
        }
        else
        {
            _dbContext.AuctionStatusHistory.Add(new AuctionStatusHistoryEntity
            {
                AuctionId = auction.Id,
                PreviousStatus = auction.Status,
                NewStatus = auction.Status,
                IsTerminal = IsTerminal(auction.Status),
                ObservedAt = normalized.ObservedAtUtc,
                Source = "out_of_order",
            });
        }

        await SaveProcessingLog(
            normalized,
            source,
            shouldUpdateSnapshot ? "PROCESSED" : "OUT_OF_ORDER_IGNORED",
            shouldUpdateSnapshot ? null : "Snapshot was not updated because message is stale and weaker.",
            cancellationToken);

        _logger.LogInformation(
            "Ingestion pipeline processed message. messageId={MessageId}, auctionId={AuctionId}, source={Source}, outOfOrder={OutOfOrder}, snapshotUpdated={SnapshotUpdated}, status={Status}",
            normalized.MessageId,
            auction.Id,
            source,
            isOutOfOrder,
            shouldUpdateSnapshot,
            normalized.AuctionStatusNormalized);
    }

    private async Task<SourcePlatformEntity> GetOrCreateSourcePlatform(
        NormalizedObservation message,
        CancellationToken cancellationToken)
    {
        var platform = await _dbContext.SourcePlatforms
            .SingleOrDefaultAsync(x => x.Code == message.SourcePlatformCode, cancellationToken);
        if (platform is not null)
            return platform;

        platform = new SourcePlatformEntity
        {
            Code = message.SourcePlatformCode,
            Name = message.SourcePlatformCode,
        };
        _dbContext.SourcePlatforms.Add(platform);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return platform;
    }

    private async Task<DomainEntity> GetOrCreateDomain(
        NormalizedObservation message,
        CancellationToken cancellationToken)
    {
        var domain = await _dbContext.Domains
            .SingleOrDefaultAsync(x => x.NameNormalized == message.DomainNameNormalized, cancellationToken);
        if (domain is not null)
            return domain;

        domain = new DomainEntity
        {
            NameOriginal = message.DomainNameOriginal,
            NameNormalized = message.DomainNameNormalized,
            NamePunycode = message.DomainNamePunycode,
            Tld = message.Tld,
            Sld = message.Sld,
        };
        _dbContext.Domains.Add(domain);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return domain;
    }

    private async Task<AuctionEntity> GetOrCreateAuction(
        NormalizedObservation message,
        long domainId,
        long sourcePlatformId,
        CancellationToken cancellationToken)
    {
        var auction = await _dbContext.Auctions
            .SingleOrDefaultAsync(
                x => x.SourcePlatformId == sourcePlatformId && x.ExternalAuctionId == message.ExternalAuctionId,
                cancellationToken);

        if (auction is not null)
            return auction;

        auction = new AuctionEntity
        {
            DomainId = domainId,
            SourcePlatformId = sourcePlatformId,
            ExternalAuctionId = message.ExternalAuctionId,
            Status = "unknown",
        };
        _dbContext.Auctions.Add(auction);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return auction;
    }

    private async Task<bool> IsSemanticDuplicateAsync(
        NormalizedObservation normalized,
        long auctionId,
        CancellationToken cancellationToken)
    {
        var from = normalized.ObservedAtUtc.AddMinutes(-SemanticDedupeWindowMinutes);
        var to = normalized.ObservedAtUtc.AddMinutes(SemanticDedupeWindowMinutes);

        var candidates = await _dbContext.AuctionObservations
            .AsNoTracking()
            .Where(x => x.AuctionId == auctionId && x.ObservedAt >= from && x.ObservedAt <= to)
            .Select(x => new
            {
                x.AuctionStatusNormalized,
                x.CurrentPrice,
                x.FinalPrice,
                x.CurrencyCode,
                x.ObservedAt,
            })
            .ToListAsync(cancellationToken);

        return candidates.Any(x =>
            BuildFingerprint(
                normalized.AuctionStatusNormalized,
                normalized.CurrentPrice,
                normalized.FinalPrice,
                normalized.CurrencyCode,
                normalized.ObservedAtUtc) ==
            BuildFingerprint(
                x.AuctionStatusNormalized,
                x.CurrentPrice,
                x.FinalPrice,
                x.CurrencyCode,
                x.ObservedAt));
    }

    private static bool ShouldUpdateSnapshot(AuctionEntity auction, NormalizedObservation normalized, bool isOutOfOrder)
    {
        if (!isOutOfOrder)
            return true;

        if (!normalized.IsTerminal)
            return false;

        if (string.IsNullOrWhiteSpace(auction.TerminalStatus))
            return true;

        return GetTerminalPriority(normalized.AuctionStatusNormalized) > GetTerminalPriority(auction.TerminalStatus);
    }

    private static void ApplyAuctionSnapshot(AuctionEntity auction, NormalizedObservation normalized)
    {
        auction.Status = normalized.AuctionStatusNormalized;
        auction.CurrentPrice = normalized.CurrentPrice ?? auction.CurrentPrice;
        auction.CurrencyCode = normalized.CurrencyCode ?? auction.CurrencyCode;
        auction.StartAt = normalized.AuctionStartAtUtc ?? auction.StartAt;
        auction.EndAt = normalized.AuctionEndAtUtc ?? auction.EndAt;
        auction.ExtendedEndAt = normalized.AuctionExtendedEndAtUtc ?? auction.ExtendedEndAt;
        auction.IsExtended = normalized.IsExtended ?? auction.IsExtended;
        auction.LotUrl = normalized.LotUrl ?? auction.LotUrl;
        if (!auction.LastObservedAt.HasValue || normalized.ObservedAtUtc > auction.LastObservedAt)
            auction.LastObservedAt = normalized.ObservedAtUtc;

        if (!normalized.IsTerminal)
            return;

        if (string.IsNullOrWhiteSpace(auction.TerminalStatus) ||
            GetTerminalPriority(normalized.AuctionStatusNormalized) >= GetTerminalPriority(auction.TerminalStatus))
        {
            auction.TerminalStatus = normalized.AuctionStatusNormalized;
            auction.FinalizedAt = normalized.ObservedAtUtc;
            auction.FinalPrice = ResolveFinalPrice(normalized, auction);
        }
    }

    private static decimal? ResolveFinalPrice(NormalizedObservation normalized, AuctionEntity auction)
    {
        if (normalized.FinalPrice.HasValue)
            return normalized.FinalPrice;

        if (normalized.AuctionStatusNormalized == "sold")
            return normalized.CurrentPrice ?? auction.CurrentPrice;

        return auction.FinalPrice;
    }

    private async Task SaveProcessingLog(
        NormalizedObservation message,
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

    private static void ValidateSchema(ObservationIngestionMessage message)
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

    private static void ValidateBusiness(ObservationIngestionMessage message)
    {
        if (message.CurrentPrice is < 0 || message.FinalPrice is < 0)
            throw new NonRetryableIngestionException("Price cannot be negative.");

        if (message.FinalPrice.HasValue && string.IsNullOrWhiteSpace(message.CurrencyCode))
            throw new NonRetryableIngestionException("CurrencyCode is required when FinalPrice is provided.");
    }

    private static NormalizedObservation Normalize(ObservationIngestionMessage message)
    {
        var normalizedDomain = (message.DomainNameNormalized ?? message.DomainNameOriginal).Trim().ToLowerInvariant().TrimEnd('.');
        var idn = new IdnMapping();
        var punycode = idn.GetAscii(normalizedDomain);

        var domainParts = normalizedDomain.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var tld = domainParts.Length > 1 ? domainParts[^1] : normalizedDomain;
        var sld = domainParts.Length > 1 ? domainParts[^2] : normalizedDomain;

        var status = NormalizeStatus(message.AuctionStatusNormalized ?? message.AuctionStatusRaw);
        var observedAtUtc = message.ObservedAt.ToUniversalTime();
        var normalized = new NormalizedObservation
        {
            MessageId = message.MessageId.Trim(),
            SchemaVersion = message.SchemaVersion.Trim(),
            ParserCode = message.ParserCode.Trim(),
            ParserVersion = message.ParserVersion.Trim(),
            SourcePlatformCode = message.SourcePlatformCode.Trim().ToLowerInvariant(),
            ObservedAtUtc = observedAtUtc,
            ExternalAuctionId = message.ExternalAuctionId.Trim(),
            DomainNameOriginal = message.DomainNameOriginal.Trim(),
            DomainNameNormalized = normalizedDomain,
            DomainNamePunycode = punycode.ToLowerInvariant(),
            Tld = tld,
            Sld = sld,
            AuctionStatusRaw = message.AuctionStatusRaw.Trim(),
            AuctionStatusNormalized = status,
            CurrentPrice = message.CurrentPrice,
            FinalPrice = message.FinalPrice,
            CurrencyCode = NormalizeCurrency(message.CurrencyCode),
            AuctionStartAtUtc = message.AuctionStartAt?.ToUniversalTime(),
            AuctionEndAtUtc = message.AuctionEndAt?.ToUniversalTime(),
            AuctionExtendedEndAtUtc = message.AuctionExtendedEndAt?.ToUniversalTime(),
            IsExtended = message.IsExtended,
            LotUrl = string.IsNullOrWhiteSpace(message.LotUrl) ? null : message.LotUrl.Trim(),
            AttributesJson = message.AttributesJson,
            RawPayload = message.RawPayload,
        };

        normalized.SemanticFingerprint = BuildFingerprint(
            normalized.AuctionStatusNormalized,
            normalized.CurrentPrice,
            normalized.FinalPrice,
            normalized.CurrencyCode,
            normalized.ObservedAtUtc);

        return normalized;
    }

    private static string NormalizeStatus(string status)
    {
        var value = status.Trim().ToLowerInvariant();
        return value switch
        {
            "active" or "running" or "open" => "active",
            "ending" => "ending",
            "extended" => "extended",
            "sold" or "completed" or "closed" => "sold",
            "not_sold" or "unsold" => "not_sold",
            "cancelled" or "canceled" => "cancelled",
            "disappeared" => "disappeared",
            _ => "unknown",
        };
    }

    private static string? NormalizeCurrency(string? currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode) ? null : currencyCode.Trim().ToUpperInvariant();

    private static string BuildFingerprint(
        string status,
        decimal? currentPrice,
        decimal? finalPrice,
        string? currencyCode,
        DateTimeOffset observedAt)
    {
        var observedSecond = new DateTimeOffset(
            observedAt.Year,
            observedAt.Month,
            observedAt.Day,
            observedAt.Hour,
            observedAt.Minute,
            observedAt.Second,
            TimeSpan.Zero);

        var raw = string.Join("|",
            status,
            currentPrice?.ToString(CultureInfo.InvariantCulture) ?? "null",
            finalPrice?.ToString(CultureInfo.InvariantCulture) ?? "null",
            currencyCode ?? "null",
            observedSecond.ToString("O", CultureInfo.InvariantCulture));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static bool IsTerminal(string status) => TerminalPrecedence.ContainsKey(status);

    private static int GetTerminalPriority(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return 0;
        return TerminalPrecedence.GetValueOrDefault(status, 0);
    }
}
