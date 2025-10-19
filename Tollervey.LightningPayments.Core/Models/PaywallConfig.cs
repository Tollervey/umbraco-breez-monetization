using System.Text.Json.Serialization;

namespace Tollervey.LightningPayments.Breez.Models
{
    public class PaywallConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("fee")]
        public ulong Fee { get; set; }
    }
}