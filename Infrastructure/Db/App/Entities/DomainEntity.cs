using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Db.App.Entities;

[Comment("Домен в нормализованном виде.")]
public class DomainEntity : BaseEntity
{
    [Comment("Оригинальное имя домена.")]
    [MaxLength(255)]
    public string NameOriginal { get; set; } = string.Empty;

    [Comment("Нормализованное имя домена.")]
    [MaxLength(255)]
    public string NameNormalized { get; set; } = string.Empty;

    [Comment("Punycode представление домена.")]
    [MaxLength(255)]
    public string NamePunycode { get; set; } = string.Empty;

    [Comment("Домен верхнего уровня.")]
    [MaxLength(32)]
    public string Tld { get; set; } = string.Empty;

    [Comment("SLD часть доменного имени.")]
    [MaxLength(255)]
    public string Sld { get; set; } = string.Empty;

    public ICollection<AuctionEntity> Auctions { get; set; } = new List<AuctionEntity>();
}
