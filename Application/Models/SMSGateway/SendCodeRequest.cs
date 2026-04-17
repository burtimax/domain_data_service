using System.Text.Json.Serialization;

namespace Application.Models.SMSGateway
{
    public class SendCodeRequest
    {
        /// <summary>
        /// Без +7 и прочего, только цифры
        /// </summary>
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }
        
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        // [JsonPropertyName("sender_username")]
        // public string? SenderUsername { get; set; }
        
        // [JsonPropertyName("code")]
        // public string? Code { get; set; }

        [JsonPropertyName("code_length")]
        public int? CodeLength { get; set; } = 4;

        // [JsonPropertyName("callback_url")]
        // public string? CallbackUrl { get; set; }

        // [JsonPropertyName("payload")]
        // public string? Payload { get; set; }

        // значение в секундах
        [JsonPropertyName("ttl")]
        public int? TTL { get; set; } = 30;
    }
}
