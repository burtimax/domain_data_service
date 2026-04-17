using Shared.Contracts;
using System.Text.Json.Serialization;

namespace Application.Models.SMSGateway
{
    public class RevokeCodeResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public bool? Result { get; set; }
    }
}
