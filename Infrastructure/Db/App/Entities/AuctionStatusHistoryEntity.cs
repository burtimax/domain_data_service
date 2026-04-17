using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Db.App.Entities;

[Comment("История смены статусов торгов.")]
public class AuctionStatusHistoryEntity : BaseEntity
{
    [Comment("Связанные торги.")]
    public long AuctionId { get; set; }

    [Comment("Статус до изменения.")]
    [MaxLength(32)]
    public string? PreviousStatus { get; set; }

    [Comment("Новый статус.")]
    [MaxLength(32)]
    public string NewStatus { get; set; } = string.Empty;

    [Comment("Признак terminal статуса.")]
    public bool IsTerminal { get; set; }

    [Comment("Время наблюдения статуса.")]
    public DateTimeOffset ObservedAt { get; set; }

    [Comment("Источник изменения статуса.")]
    [MaxLength(32)]
    public string Source { get; set; } = "observation";

    public AuctionEntity Auction { get; set; } = null!;
}
