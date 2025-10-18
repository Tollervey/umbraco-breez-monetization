using System.Text.Json.Serialization;

namespace Tollervey.Umbraco.LightningPayments.Models
{

    // NOTE: This is a simplified model. The actual Breez webhook payload may be more complex.
    // Refer to the official Breez SDK documentation for the exact structure.
    public class BreezWebhookPayload
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("payment")]
        public PaymentDetails? Payment { get; set; }
    }
}
