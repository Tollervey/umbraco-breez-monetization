using System.Text.Json.Serialization;

namespace Our.Umbraco.Bitcoin.LightningPayments.Models
{
    /// <summary>
    /// Configuration for enabling a tip jar and specifying default amounts and label.
    /// </summary>
    public class TipJarConfig
    {
        /// <summary>
        /// Whether the tip jar is enabled for the content.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// The default amounts in satoshis for the tip jar.
        /// </summary>
        [JsonPropertyName("defaultAmounts")]
        public ulong[] DefaultAmounts { get; set; } = [500, 1000, 2500];

        /// <summary>
        /// The label for the tip jar.
        /// </summary>
        [JsonPropertyName("label")]
        public string Label { get; set; } = "Send a tip";
    }
}