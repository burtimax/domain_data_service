using Application.Models.SMSGateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Shared.Configs;

namespace Application.Services.SMSGateway
{
    /// <summary>
    /// Сервис для работы с SMS Gateway API
    /// </summary>
    public class SMSGatewayService : ISMSGatewayService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppConfiguration _config;
        private readonly ILogger<SMSGatewayService> _logger;
        private readonly string _url;
        private readonly string _token;

        /// <summary>
        /// Инициализирует новый экземпляр SMS Gateway сервиса
        /// </summary>
        public SMSGatewayService(
            IHttpClientFactory httpClientFactory,
            AppConfiguration config,
            ILogger<SMSGatewayService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
            _url = _config.SMSGateway.Url;
            _token = _config.SMSGateway.Token;
        }

        /// <summary>
        /// Базовый метод для отправки HTTP запросов к SMS Gateway API
        /// </summary>
        /// <typeparam name="TRequest">Тип запроса</typeparam>
        /// <typeparam name="TResponse">Тип ответа</typeparam>
        /// <param name="endpoint">Endpoint API</param>
        /// <param name="request">Данные запроса</param>
        /// <returns>Ответ от API или null при ошибке</returns>
        private async Task<TResponse?> SendRequestAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request) where TResponse : class
        {
            try
            {
                var httpRequestMessage = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{_url}/{endpoint}")
                {
                    Headers =
                    {
                        { HeaderNames.Authorization, $"Bearer {_token}" }
                    },
                    Content = new StringContent(
                        JsonSerializer.Serialize(request),
                        Encoding.UTF8,
                        "application/json")
                };

                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.SendAsync(httpRequestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var result = await JsonSerializer.DeserializeAsync<TResponse>(
                        await response.Content.ReadAsStreamAsync());

                    _logger.LogInformation("Успешный запрос к SMS Gateway: {Endpoint}", endpoint);
                    return result;
                }

                _logger.LogWarning(
                    "SMS Gateway вернул код ошибки {StatusCode} для {Endpoint}",
                    response.StatusCode,
                    endpoint);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Ошибка при отправке запроса к SMS Gateway: {Endpoint}",
                    endpoint);
                return null;
            }
        }

        /// <summary>
        /// Отправляет код верификации пользователю
        /// </summary>
        /// <param name="sendCode">Данные для отправки кода</param>
        /// <returns>Ответ от SMS Gateway</returns>
        public async Task<TelegramGatewayResponse?> SendCodeAsync(SendCodeRequest sendCode)
        {
            return await SendRequestAsync<SendCodeRequest, TelegramGatewayResponse>(
                "sendVerificationMessage",
                sendCode);
        }

        /// <summary>
        /// Отзывает ранее отправленный код верификации
        /// </summary>
        /// <param name="dto">Данные для отзыва кода</param>
        /// <returns>Ответ об отзыве кода</returns>
        /// <exception cref="ArgumentException">Если RequestId отсутствует</exception>
        public async Task<RevokeCodeResponse?> RevokeCodeAsync(RevokeCodeRequest dto)
        {
            if (dto is null || string.IsNullOrEmpty(dto.RequestId))
            {
                _logger.LogWarning("Попытка отзыва кода без RequestId");
                throw new ArgumentException("RequestId обязателен для отзыва кода", nameof(dto));
            }

            return await SendRequestAsync<RevokeCodeRequest, RevokeCodeResponse>(
                "revokeVerificationMessage",
                dto);
        }

        /// <summary>
        /// Проверяет статус верификации
        /// </summary>
        /// <param name="checkVerificationStatus">Данные для проверки статуса</param>
        /// <returns>Статус верификации</returns>
        public async Task<TelegramGatewayResponse?> CheckVerificationStatus(
            CheckVerificationStatus checkVerificationStatus)
        {
            return await SendRequestAsync<CheckVerificationStatus, TelegramGatewayResponse>(
                "checkVerificationStatus",
                checkVerificationStatus);
        }

        /// <summary>
        /// Проверяет возможность отправки сообщения
        /// </summary>
        /// <param name="dto">Данные для проверки</param>
        /// <returns>Информация о возможности отправки</returns>
        public async Task<TelegramGatewayResponse?> CheckSendAbility(CheckSendAbilityRequest dto)
        {
            return await SendRequestAsync<CheckSendAbilityRequest, TelegramGatewayResponse>(
                "checkSendAbility",
                dto);
        }
    }
}
