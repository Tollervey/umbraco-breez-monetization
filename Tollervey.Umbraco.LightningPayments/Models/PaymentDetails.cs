using System.Text.Json.Serialization;

namespace Tollervey.Umbraco.LightningPayments.Models
{
    public class PaymentDetails
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; } = string.Empty; // This is expected to be the payment_hash
    }
}
