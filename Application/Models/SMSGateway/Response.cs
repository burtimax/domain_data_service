using Shared.Contracts;
using System.Text.Json.Serialization;

namespace Application.Models.SMSGateway
{
    public class TelegramGatewayResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public RequestStatus? Result { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
