using System.Text.Json.Serialization;

namespace Our.Umbraco.Bitcoin.LightningPayments.Models
{
    /// <summary>
    /// Simplified Breez webhook payload carrying event type and payment details.
    /// </summary>
    public class BreezWebhookPayload
    {
        /// <summary>
        /// The type of webhook event (e.g., payment_succeeded, payment_failed).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Minimal payment details, including the payment id/hash.
        /// </summary>
        [JsonPropertyName("payment")]
        public PaymentDetails? Payment { get; set; }
    }
}
