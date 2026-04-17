using Application.Models.User;
using Infrastructure.Db.App.Entities;
using Infrastructure.Models;
using Shared.Contracts;
using Pagination = Shared.Models.Pagination;

namespace Application.Services.User
{
    public interface IUserService
    {
        /// <summary>
        /// Создание пользователя.
        /// </summary>
        Task<UserEntity> CreateAsync(CreateUserRequest r, CancellationToken cancellation);

        /// <summary>
        /// Получение пользлователей.
        /// </summary>>
        Task<PagedList<UserEntity>> GetAsync(GetUserRequest r, CancellationToken cancellation);

        /// <summary>
        /// Удаление пользлователя.
        /// </summary>>
        Task DeleteAsync(long userId, CancellationToken cancellation);

        /// <summary>
        /// Номер телефона верифицирован.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="userId"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        Task<UserEntity> VerifyPhoneNumberAsync(long userId, string phoneNumber, CancellationToken cancellation);

        /// <summary>
        /// Обновление пользователя.
        /// </summary>
        Task<UserEntity> UpdateAsync(UpdateUserRequest r, CancellationToken cancellation);

        /// <summary>
        /// Обновлнние комментария к пациентю.
        /// </summary>
        Task<UserEntity> UpdateCommentAsync(long userId, string comment, CancellationToken cancellation);

        /// <summary>
        /// Получение моего пользователя.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Task<UserEntity?> GetByIdAsync(long userId);

    }
}
