using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Const;
using Shared.Extensions;

namespace Infrastructure.Db.App.Entities
{
    /// <summary>
    /// Сущность пользователя в базе данных
    /// </summary>
    public class UserEntity : BaseEntity
    {
        /// <summary>
        /// Идентификатор пользователя в Telegram
        /// </summary>
        [Comment("Телеграм ИД пользователя")]
        public long TelegramId { get; set; }

        /// <summary>
        /// Роль пользователя в системе (admin, user и т.д.)
        /// </summary>
        [Comment("Роль пользователя")]
        public string Role { get; set; } = null!;

        /// <summary>
        /// Username пользователя в Telegram
        /// </summary>
        [Comment("Telegram username")]
        public string? UserName { get; set; }

        /// <summary>
        /// Имя пользователя
        /// </summary>
        [Comment("Имя")]
        public string? FirstName { get; set; }

        /// <summary>
        /// Отчество пользователя
        /// </summary>
        [Comment("Отчество")]
        public string? MiddleName { get; set; }

        /// <summary>
        /// Фамилия пользователя
        /// </summary>
        [Comment("Фамилия")]
        public string? LastName { get; set; }

        /// <summary>
        /// Номер телефона пользователя (в нормализованном формате)
        /// </summary>
        [Comment("Номер телефона")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Флаг подтверждения номера телефона
        /// </summary>
        [Comment("Пользователь подтвердил номер телефона")]
        public bool? IsVerified { get; set; }

        /// <summary>
        /// URL фотографии профиля пользователя
        /// </summary>
        [Comment("URL фото пользователя")]
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// Дополнительные поля пользователя в формате JSON
        /// </summary>
        [Comment("Доп поля пользователя")]
        public JsonDocument? Additional { get; set; }
    }
}
