using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Db.App.Entities;

[Comment("Площадка-источник торгов.")]
public class SourcePlatformEntity : BaseEntity
{
    [Comment("Код платформы (уникальный).")]
    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [Comment("Человекочитаемое название платформы.")]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public ICollection<AuctionEntity> Auctions { get; set; } = new List<AuctionEntity>();
}
