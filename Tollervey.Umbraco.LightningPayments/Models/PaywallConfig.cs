using System.Text.Json.Serialization;

namespace Tollervey.Umbraco.LightningPayments.UI.Models
{
    /// <summary>
    /// Configuration for enabling a paywall and specifying the fee in sats.
    /// </summary>
    public class PaywallConfig
    {
        /// <summary>
        /// Whether the paywall is enabled for the content.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// The amount in satoshis required to unlock the content.
        /// </summary>
        [JsonPropertyName("fee")]
        public ulong Fee { get; set; }
    }
}