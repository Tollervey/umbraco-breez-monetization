using System.Text.Json.Serialization;

namespace Tollervey.Umbraco.LightningPayments.UI.Models
{
    public class PaywallConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("fee")]
        public ulong Fee { get; set; }
    }
}