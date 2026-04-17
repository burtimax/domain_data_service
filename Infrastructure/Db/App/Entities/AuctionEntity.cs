using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Db.App.Entities;

[Comment("Торги домена на конкретной площадке.")]
public class AuctionEntity : BaseEntity
{
    [Comment("Идентификатор домена.")]
    public long DomainId { get; set; }

    [Comment("Идентификатор площадки.")]
    public long SourcePlatformId { get; set; }

    [Comment("Внешний идентификатор лота на площадке.")]
    [MaxLength(128)]
    public string ExternalAuctionId { get; set; } = string.Empty;

    [Comment("Текущий нормализованный статус торгов.")]
    [MaxLength(32)]
    public string Status { get; set; } = "unknown";

    [Comment("Текущая цена.")]
    public decimal? CurrentPrice { get; set; }

    [Comment("Финальная цена.")]
    public decimal? FinalPrice { get; set; }

    [Comment("Код валюты.")]
    [MaxLength(8)]
    public string? CurrencyCode { get; set; }

    [Comment("Старт торгов.")]
    public DateTimeOffset? StartAt { get; set; }

    [Comment("Плановое завершение торгов.")]
    public DateTimeOffset? EndAt { get; set; }

    [Comment("Продленное завершение торгов.")]
    public DateTimeOffset? ExtendedEndAt { get; set; }

    [Comment("Торги продлены.")]
    public bool? IsExtended { get; set; }

    [Comment("Финальный terminal статус.")]
    [MaxLength(32)]
    public string? TerminalStatus { get; set; }

    [Comment("Время финализации.")]
    public DateTimeOffset? FinalizedAt { get; set; }

    [Comment("Ссылка на лот.")]
    [MaxLength(1024)]
    public string? LotUrl { get; set; }

    [Comment("Последнее обработанное время наблюдения.")]
    public DateTimeOffset? LastObservedAt { get; set; }

    public DomainEntity Domain { get; set; } = null!;
    public SourcePlatformEntity SourcePlatform { get; set; } = null!;
    public ICollection<AuctionObservationEntity> Observations { get; set; } = new List<AuctionObservationEntity>();
    public ICollection<AuctionStatusHistoryEntity> StatusHistory { get; set; } = new List<AuctionStatusHistoryEntity>();
}
