using System.Globalization;
using System.Text.Json;
using ConsoleTest.NicsellParser.Models;
using Shared.Contracts.Ingestion;

namespace ParserNicsell.Ingestion;

public static class NicsellObservationMapper
{
    private static readonly JsonSerializerOptions AttrJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static ObservationIngestionMessage ToObservation(
        NicsellDomainRecord record,
        string parserCode,
        string parserVersion,
        DateTimeOffset observedAt)
    {
        var externalAuctionId = (record.DomainId?.ToString(CultureInfo.InvariantCulture) ?? record.Domain) + "_" + record.AuctionEndsAt;

        var attributes = new
        {
            record.BidCount,
            record.InTldCount,
            record.MajesticRefDomains,
            record.MajesticBacklinks,
            record.MajesticTrustFlow,
            record.MajesticCitationFlow,
            record.MajesticTfCfRatio,
            record.GoogleHits,
            record.ArchiveOrgResults,
            record.NameLength,
            record.IsQuarantine,
            record.IsPremium,
            record.AuctionEndsDisplay
        };

        return new ObservationIngestionMessage
        {
            MessageId = Guid.NewGuid().ToString("N"),
            SchemaVersion = "1.0",
            ParserCode = parserCode,
            ParserVersion = parserVersion,
            SourcePlatformCode = "nicksell",
            ObservedAt = observedAt,
            ExternalAuctionId = externalAuctionId,
            ExternalDomainId = record.DomainId?.ToString(CultureInfo.InvariantCulture),
            DomainNameOriginal = record.Domain,
            DomainNameNormalized = null,
            AuctionStatusRaw = "active",
            AuctionStatusNormalized = "active",
            CurrentPrice = record.CurrentBidEur,
            FinalPrice = null,
            CurrencyCode = "EUR",
            AuctionStartAt = null,
            AuctionEndAt = record.AuctionEndsAt,
            AuctionExtendedEndAt = null,
            IsExtended = null,
            LotUrl = record.DetailsUrl,
            AttributesJson = JsonSerializer.Serialize(attributes, AttrJsonOptions),
            RawPayload = JsonSerializer.Serialize(new { source = "nicsell-domainlist", record.Domain, record.DomainId }, AttrJsonOptions)
        };
    }
}
