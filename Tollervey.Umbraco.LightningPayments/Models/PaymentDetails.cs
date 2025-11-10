using System.Text.Json.Serialization;

namespace Tollervey.Umbraco.LightningPayments.UI.Models
{
    /// <summary>
    /// Minimal payment detail returned by Breez webhook payloads.
    /// </summary>
    public class PaymentDetails
    {
        /// <summary>
        /// Payment identifier (payment hash for Lightning payments).
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; } = string.Empty; // This is expected to be the payment_hash
    }
}