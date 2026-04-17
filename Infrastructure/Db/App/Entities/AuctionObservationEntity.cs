using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Db.App.Entities;

[Comment("История observation-сообщений по торгам.")]
public class AuctionObservationEntity : BaseEntity
{
    [Comment("Связанные торги.")]
    public long AuctionId { get; set; }

    [Comment("Уникальный идентификатор сообщения.")]
    [MaxLength(64)]
    public string MessageId { get; set; } = string.Empty;

    [Comment("Версия схемы сообщения.")]
    [MaxLength(16)]
    public string SchemaVersion { get; set; } = string.Empty;

    [Comment("Код парсера/producer.")]
    [MaxLength(64)]
    public string ParserCode { get; set; } = string.Empty;

    [Comment("Версия парсера/producer.")]
    [MaxLength(32)]
    public string ParserVersion { get; set; } = string.Empty;

    [Comment("Время фактического наблюдения.")]
    public DateTimeOffset ObservedAt { get; set; }

    [Comment("Сырой статус источника.")]
    [MaxLength(128)]
    public string AuctionStatusRaw { get; set; } = string.Empty;

    [Comment("Нормализованный статус.")]
    [MaxLength(32)]
    public string AuctionStatusNormalized { get; set; } = "unknown";

    [Comment("Наблюдаемая цена.")]
    public decimal? CurrentPrice { get; set; }

    [Comment("Наблюдаемая финальная цена.")]
    public decimal? FinalPrice { get; set; }

    [Comment("Код валюты.")]
    [MaxLength(8)]
    public string? CurrencyCode { get; set; }

    [Comment("Сырой payload источника (json/string).")]
    public string? RawPayload { get; set; }

    [Comment("Расширяемые атрибуты (json/string).")]
    public string? AttributesJson { get; set; }

    [Comment("Признак out-of-order события.")]
    public bool IsOutOfOrder { get; set; }

    public AuctionEntity Auction { get; set; } = null!;
}
