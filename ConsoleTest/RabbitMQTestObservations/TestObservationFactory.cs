using Shared.Contracts.Ingestion;

namespace ConsoleTest.RabbitMQTestObservations;

public static class TestObservationFactory
{
    public static IReadOnlyList<ObservationIngestionMessage> Build(int count)
    {
        var now = DateTimeOffset.UtcNow;
        var messages = new List<ObservationIngestionMessage>(count);

        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid().ToString("N");
            messages.Add(new ObservationIngestionMessage
            {
                MessageId = id,
                SchemaVersion = "1.0",
                ParserCode = "console-test",
                ParserVersion = "1.0.0",
                SourcePlatformCode = "nicksell",
                ExternalAuctionId = $"auction-{1000 + i}",
                DomainNameOriginal = $"test-domain-{i}.com",
                DomainNameNormalized = $"test-domain-{i}.com",
                ObservedAt = now.AddSeconds(-i),
                AuctionStatusRaw = i % 5 == 0 ? "sold" : "active",
                AuctionStatusNormalized = i % 5 == 0 ? "sold" : "active",
                CurrentPrice = 100 + i,
                FinalPrice = i % 5 == 0 ? 100 + i : null,
                CurrencyCode = "USD",
                AuctionStartAt = now.AddDays(-1),
                AuctionEndAt = now.AddDays(1),
                IsExtended = false,
                LotUrl = $"https://example.com/lot/{1000 + i}",
                AttributesJson = """{"source":"console-test","kind":"mvp"}""",
                RawPayload = """{"mock":true}"""
            });
        }

        return messages;
    }
}
