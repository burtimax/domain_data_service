using System.Text.Json.Serialization;

namespace Application.Models.SMSGateway
{
    public class CheckVerificationStatus
    {
        [JsonPropertyName("request_id")]
        public string RequestId { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }
}
