using System.Text.Json.Serialization;

namespace Tollervey.LightningPayments.Breez.Models
{
    public class PaymentDetails
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; } = string.Empty; // This is expected to be the payment_hash
    }
}