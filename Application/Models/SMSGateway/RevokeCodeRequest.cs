using System.Text.Json.Serialization;

namespace Application.Models.SMSGateway
{
    public class RevokeCodeRequest
    {
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }
    }
}
