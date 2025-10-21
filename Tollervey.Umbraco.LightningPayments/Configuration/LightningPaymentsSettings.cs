using System.ComponentModel.DataAnnotations;

namespace Tollervey.LightningPayments.Breez.Configuration
{
    /// <summary>
    /// Configuration settings for the Lightning Payments extension.
    /// </summary>
    public class LightningPaymentsSettings
    {
        public enum LightningNetwork { Mainnet, Testnet, Regtest }

        public const string SectionName = "LightningPayments";

        /// <summary>
        /// The API key for Breez SDK.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string BreezApiKey { get; init; } = string.Empty;

        /// <summary>
        /// The mnemonic for Breez SDK.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string Mnemonic { get; init; } = string.Empty;

        /// <summary>
        /// The URL for webhook notifications.
        /// </summary>
        [Url]
        public string? WebhookUrl { get; init; }

        /// <summary>
        /// The database connection string.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string ConnectionString { get; init; } = "Data Source=payment.db";

        /// <summary>
        /// The URL for the paywall page.
        /// </summary>
        public string PaywallUrl { get; init; } = "/paywall";

        /// <summary>
        /// The secret key for webhook signature verification.
        /// </summary>
        public string WebhookSecret { get; init; } = string.Empty;

        /// <summary>
        /// The Application Insights connection string for monitoring.
        /// </summary>
        public string ApplicationInsightsConnectionString { get; init; } = string.Empty;

        /// <summary>
        /// The admin email for payment notifications.
        /// </summary>
        public string AdminEmail { get; init; } = string.Empty;

        /// <summary>
        /// The network for the Lightning Payments, default is Mainnet.
        /// </summary>
        public LightningNetwork Network { get; init; } = LightningNetwork.Mainnet;

        /// <summary>
        /// The SMTP host for sending emails.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string SmtpHost { get; init; } = string.Empty;

        /// <summary>
        /// The SMTP port for sending emails.
        /// </summary>
        [Required]
        [Range(1, 65535)]
        public int SmtpPort { get; init; } = 587;

        /// <summary>
        /// The SMTP username for authentication.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string SmtpUsername { get; init; } = string.Empty;

        /// <summary>
        /// The SMTP password for authentication.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string SmtpPassword { get; init; } = string.Empty;

        /// <summary>
        /// The from email address for sent emails.
        /// </summary>
        [Required]
        [EmailAddress]
        [MinLength(1)]
        public string FromEmailAddress { get; init; } = string.Empty;

        /// <summary>
        /// The maximum invoice amount in satoshis.
        /// </summary>
        [Range(1, ulong.MaxValue)]
        public ulong MaxInvoiceAmountSat { get; init; } = 10_000_000;

        /// <summary>
        /// The maximum length of the invoice description.
        /// </summary>
        [Range(1, 1024)]
        public int MaxInvoiceDescriptionLength { get; init; } = 200;
    }

}
