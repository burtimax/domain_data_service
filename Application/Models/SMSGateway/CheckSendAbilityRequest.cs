using System.Text.Json.Serialization;

namespace Application.Models.SMSGateway
{
    public class CheckSendAbilityRequest
    {
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }
    }
}
