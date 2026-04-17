using System.Text.Json.Serialization;

namespace Application.Models.SMSGateway
{
    public class RequestStatus
    {
        [JsonPropertyName("request_id")]
        public string RequestId { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("request_cost")]
        public float RequestCost { get; set; }

        [JsonPropertyName("is_refunded")]
        public bool? IsRefunded { get; set; }

        [JsonPropertyName("remaining_balance")]
        public float? RemainingBalance { get; set; }

        [JsonPropertyName("delivery_status")]
        public DeliveryStatus? DeliveryStatus { get; set; }

        [JsonPropertyName("verification_status")]
        public VerificationStatus? VerificationStatus { get; set; }

        [JsonPropertyName("payload")]
        public string? Payload { get; set; }

    }

    public class DeliveryStatus
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("updated_at")]
        public int UpdatedAt { get; set; }
    }

    public class VerificationStatus
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("updated_at")]
        public int UpdatedAt { get; set; }

        [JsonPropertyName("code_entered")]
        public string? CodeEntered { get; set; }
    }
}
