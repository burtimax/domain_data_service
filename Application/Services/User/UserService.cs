using Application.Extensions;
using Application.Models.User;
using Application.Utils;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Infrastructure.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Const;
using Shared.Contracts;
using Shared.Extensions;
using Pagination = Shared.Models.Pagination;

namespace Application.Services.User
{
    /// <summary>
    /// Сервис для управления пользователями
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly AppDbContext _db;

        /// <summary>
        /// Инициализирует новый экземпляр сервиса пользователей
        /// </summary>
        /// <param name="db">Контекст базы данных</param>
        /// <param name="mapper">Маппер объектов</param>
        public UserService(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        /// <summary>
        /// Создаёт нового пользователя
        /// </summary>
        /// <param name="r">Данные для создания пользователя</param>
        /// <param name="cancellation">Токен отмены операции</param>
        /// <returns>Созданная сущность пользователя</returns>
        /// <exception cref="Exception">Выбрасывается при неверном формате телефона или если пользователь с таким номером уже существует</exception>
        public async Task<UserEntity> CreateAsync(
            CreateUserRequest r,
            CancellationToken cancellation)
        {
            if (!string.IsNullOrEmpty(r.PhoneNumber))
            {
                var normalizedPhone = PhoneNumberUtils.NormalizePhoneNumber(r.PhoneNumber);
                if (normalizedPhone == null)
                {
                    throw new Exception("Неверный формат номера телефона");
                }

                var existingUser = await _db.Users
                    .FirstOrDefaultAsync(x => x.PhoneNumber == normalizedPhone, cancellation);

                if (existingUser != null)
                {
                    throw new Exception($"Пользователь с номером телефона {normalizedPhone} уже существует");
                }

                r.PhoneNumber = normalizedPhone;
            }

            var user = _mapper.Map<UserEntity>(r);
            user.TelegramId = AppConstants.UndefinedTelegramId;
            user.Role = AppConstants.UserRoles.User;

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellation);

            return user;
        }

        /// <summary>
        /// Удаляет пользователя из базы данных
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="cancellation">Токен отмены операции</param>
        public async Task DeleteAsync(
            long userId,
            CancellationToken cancellation)
        {
            var user = await GetByIdAsync(userId);

            if (user is null) return;

            _db.Users.Remove(user);
            await _db.SaveChangesAsync(cancellation);
        }

        /// <summary>
        /// Подтверждает номер телефона пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="phoneNumber">Номер телефона для подтверждения</param>
        /// <param name="cancellation">Токен отмены операции</param>
        /// <returns>Обновлённая сущность пользователя</returns>
        /// <exception cref="Exception">Выбрасывается, если пользователь не найден</exception>
        public async Task<UserEntity> VerifyPhoneNumberAsync(long userId, string phoneNumber, CancellationToken cancellation)
        {
            var user = await GetByIdAsync(userId);
            if (user is null) throw new Exception($"Пользователь не найден [{userId}]");

            user.PhoneNumber = phoneNumber;
            user.IsVerified = true;
            _db.Users.Update(user);
            await _db.SaveChangesAsync(cancellation);

            return user;
        }

        /// <summary>
        /// Получает список пользователей с фильтрацией и пагинацией
        /// </summary>
        /// <param name="r">Параметры запроса (фильтры, пагинация, сортировка)</param>
        /// <param name="cancellation">Токен отмены операции</param>
        /// <returns>Постраничный список пользователей</returns>
        public async Task<PagedList<UserEntity>> GetAsync(
            GetUserRequest r,
            CancellationToken cancellation)
        {
            var query = _db.Users
                .AsNoTracking()
                .WhereIf(r.Ids is not null && r.Ids.Any(), x => r.Ids!.Contains(x.Id))
                .WhereIf(r.TelegramIds is not null && r.TelegramIds.Any(), x => r.TelegramIds!.Contains(x.TelegramId))
                .WhereIf(r.Roles is not null && r.Roles.Any(), x => r.Roles!.Contains(x.Role))
                .OrderByStr(r.Order)
                .WhereIf(!string.IsNullOrEmpty(r.Search), u =>
                    EF.Functions.ILike(u.FirstName + " " + u.LastName + " " + u.MiddleName + " " + u.PhoneNumber,
                        r.Search.ToILikePattern(StringExtensions.ILikePatternType.Contains)));

            return await PagedList<UserEntity>.ToPagedListAsync(query, r.PageNumber, r.PageSize);
        }

        /// <summary>
        /// Получает пользователя по идентификатору
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns>Сущность пользователя или null, если пользователь не найден</returns>
        public async Task<UserEntity?> GetByIdAsync(long userId)
        {
            return await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id.Equals(userId));
        }

        /// <summary>
        /// Обновляет данные пользователя
        /// </summary>
        /// <param name="r">Данные для обновления пользователя</param>
        /// <param name="cancellation">Токен отмены операции</param>
        /// <returns>Обновлённая сущность пользователя</returns>
        /// <exception cref="Exception">Выбрасывается, если пользователь не найден или формат телефона неверный</exception>
        public async Task<UserEntity> UpdateAsync(
            UpdateUserRequest r,
            CancellationToken cancellation)
        {
            var existed = await GetByIdAsync(r.Id);

            if (existed is null) throw new Exception($"User not found [id = {r.Id}].");

            // Если пользователь поменял телефон, убираем флаг верификации
            if (r.PhoneNumber is not null && r.PhoneNumber != existed.PhoneNumber)
            {
                string? phoneNormalized = PhoneNumberUtils.NormalizePhoneNumber(r.PhoneNumber);
                if (phoneNormalized == null) throw new Exception("Неверный формат номера телефона");

                if (phoneNormalized != existed.PhoneNumber)
                {
                    existed.PhoneNumber = phoneNormalized;
                    r.PhoneNumber = null;
                    existed.IsVerified = false;
                }
            }

            _mapper.Map(r, existed);
            _db.Users.Update(existed);
            await _db.SaveChangesAsync(cancellation);
            return existed;
        }

        /// <summary>
        /// Обновляет комментарий пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="comment">Новый комментарий</param>
        /// <param name="cancellation">Токен отмены операции</param>
        /// <returns>Обновлённая сущность пользователя</returns>
        /// <exception cref="Exception">Выбрасывается, если пользователь не найден</exception>
        public async Task<UserEntity> UpdateCommentAsync(long userId, string comment, CancellationToken cancellation)
        {
            var user = await GetByIdAsync(userId);

            if (user is null) throw new Exception($"User not found [id = {userId}].");

            _db.Users.Update(user);
            await _db.SaveChangesAsync(cancellation);

            return user;
        }
    }
}
