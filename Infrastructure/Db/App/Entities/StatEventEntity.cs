using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Сущность события статистики использования приложения
/// </summary>
public class StatEventEntity : BaseEntity
{
    /// <summary>
    /// Тип события
    /// </summary>
    [Comment("Тип события")]
    [MaxLength(30)]
    public string? Type { get; set; }
}
