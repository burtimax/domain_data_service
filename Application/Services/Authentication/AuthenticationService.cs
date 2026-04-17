using Application.Configs;
using Application.Models.Auth;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shared.Contracts;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Services.StatEvent;
using Application.Services.User;
using FastEndpoints.Security;
using Shared.Const;
using Microsoft.Extensions.Logging;

namespace Application.Services.Authentication
{
    /// <summary>
    /// Сервис аутентификации пользователей
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly AppDbContext _context;
        private readonly JwtAuthTokenOption _tokenOptions = new();
        private readonly IUserService _userService;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IStatEventService _statEventService;

        /// <summary>
        /// Инициализирует новый экземпляр сервиса аутентификации
        /// </summary>
        public AuthenticationService(
            AppDbContext context,
            IConfiguration configuration,
            IUserService userService,
            ILogger<AuthenticationService> logger, IStatEventService statEventService)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
            _statEventService = statEventService;
            configuration.GetSection("Token").Bind(_tokenOptions);
        }

        /// <summary>
        /// Выполняет вход пользователя в систему и создает JWT токен
        /// </summary>
        /// <param name="request">Данные для входа</param>
        /// <returns>Данные пользователя и токен аутентификации</returns>
        /// <exception cref="InvalidOperationException">Если произошла ошибка при создании пользователя</exception>
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Попытка входа для Telegram ID: {TelegramId}", request.UserTelegramId);

                var user = await _context.Users
                    .AsNoTracking()
                    .Where(x => x.TelegramId == request.UserTelegramId)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogInformation("Создание нового пользователя с Telegram ID: {TelegramId}", request.UserTelegramId);

                    user = new()
                    {
                        TelegramId = request.UserTelegramId,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Role = AppConstants.UserRoles.User,
                        UserName = request.UserName,
                        PhotoUrl = request.PhotoUrl,
                        IsVerified = false,
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // Обновляем профиль пользователя если изменились данные
                if (request.PhotoUrl != null && user.PhotoUrl != request.PhotoUrl)
                    user.PhotoUrl = request.PhotoUrl;
                if (request.UserName != null && user.UserName != request.UserName)
                    user.UserName = request.UserName;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                long sessionId = DateTime.UtcNow.Ticks;
                var token = CreateAuthToken(sessionId, request, user);
                await _statEventService.CreateStatEventAsync(user.Id, sessionId, request.Utm,
                    "/login");

                _logger.LogInformation("Успешный вход для пользователя ID: {UserId}", user.Id);

                return new LoginResponse { User = user, Token = token };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при попытке входа для Telegram ID: {TelegramId}", request.UserTelegramId);
                throw new InvalidOperationException("Не удалось выполнить вход. Пожалуйста, попробуйте позже.", ex);
            }
        }

        /// <summary>
        /// Создает JWT токен для аутентифицированного пользователя
        /// </summary>
        /// <param name="user">Пользователь для которого создается токен</param>
        /// <returns>JWT токен в виде строки</returns>
        private string CreateAuthToken(long sessionId, LoginRequest r, UserEntity user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.FirstName ?? ""),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Role, user.Role),
                new("FirstName", user.FirstName ?? ""),
                new("LastName", user.LastName ?? ""),
                new("MiddleName", user.MiddleName ?? ""),
                new("UserTelegramId", user.TelegramId.ToString()),
                new("SessionId", sessionId.ToString()),
                new("Utm", r.Utm ?? ""),
            };

            // Пример формирования токена fastendpoints
            // https://gist.github.com/dj-nitehawk/27550c40475ea528f5c187050fca9fba
            var token = JwtBearer.CreateToken(o =>
            {
                o.SigningKey = _tokenOptions.SecretKey;
                o.Issuer = _tokenOptions.Issuer;
                o.Audience = _tokenOptions.Issuer; // Используем тот же Issuer для Audience
                o.ExpireAt = DateTime.UtcNow.AddMinutes(_tokenOptions.ExpiryMinutes);
                o.User.Claims.AddRange(claims);
                o.User.Roles.Add(user.Role);
            });

            return token;
        }
    }
}
