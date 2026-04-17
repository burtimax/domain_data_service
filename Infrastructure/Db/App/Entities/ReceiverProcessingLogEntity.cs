using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Db.App.Entities;

[Comment("Лог обработки сообщения receiver-пайплайном.")]
public class ReceiverProcessingLogEntity : BaseEntity
{
    [Comment("MessageId сообщения.")]
    [MaxLength(64)]
    public string MessageId { get; set; } = string.Empty;

    [Comment("Внешний идентификатор торгов.")]
    [MaxLength(128)]
    public string? ExternalAuctionId { get; set; }

    [Comment("Код платформы.")]
    [MaxLength(64)]
    public string? SourcePlatformCode { get; set; }

    [Comment("Код результата обработки.")]
    [MaxLength(64)]
    public string ResultCode { get; set; } = string.Empty;

    [Comment("Описание результата или ошибки.")]
    [MaxLength(2048)]
    public string? Message { get; set; }

    [Comment("TraceId запроса/обработки.")]
    [MaxLength(64)]
    public string? TraceId { get; set; }

    [Comment("Время фактической обработки.")]
    public DateTimeOffset ProcessedAt { get; set; }
}
