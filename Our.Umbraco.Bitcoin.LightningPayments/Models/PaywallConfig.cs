using System.Text.Json.Serialization;

namespace Our.Umbraco.Bitcoin.LightningPayments.Models
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

        /// <summary>
        /// The message to display for the paywall.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Custom paywall message for the POC.
        /// </summary>
        [JsonPropertyName("customMessage")]
        public string CustomMessage { get; set; } = "Default paywall message";
    }
}
