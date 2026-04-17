using Application.Models.SMSGateway;

namespace Application.Services.SMSGateway
{
    /// <summary>
    /// Интерфейс сервиса для работы с SMS Gateway API
    /// </summary>
    public interface ISMSGatewayService
    {
        /// <summary>
        /// Отправляет код верификации пользователю
        /// </summary>
        Task<TelegramGatewayResponse?> SendCodeAsync(SendCodeRequest sendCode);

        /// <summary>
        /// Проверяет статус верификации
        /// </summary>
        Task<TelegramGatewayResponse?> CheckVerificationStatus(CheckVerificationStatus checkVerificationStatus);

        /// <summary>
        /// Отзывает ранее отправленный код верификации
        /// </summary>
        Task<RevokeCodeResponse?> RevokeCodeAsync(RevokeCodeRequest dto);

        /// <summary>
        /// Проверяет возможность отправки сообщения
        /// </summary>
        Task<TelegramGatewayResponse?> CheckSendAbility(CheckSendAbilityRequest dto);
    }
}
