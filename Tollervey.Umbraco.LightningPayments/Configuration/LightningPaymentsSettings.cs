using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Tollervey.Umbraco.LightningPayments.UI.Configuration
{
    /// <summary>
    /// Configuration settings for the Lightning Payments extension.
    /// </summary>
    public class LightningPaymentsSettings
    {
        public enum LightningNetwork { Mainnet, Testnet, Regtest }

        public const string SectionName = "LightningPayments";

        // Shared constants for headers, defaults and cookie names used across the library.
        public const string IdempotencyKeyHeader = "Idempotency-Key";
        public const string BreezSignatureHeader = "X-Breez-Signature";
        public const string DefaultConnectionString = "Data Source=App_Data/LightningPayments/payment.db";
        public const string DefaultHealthPath = "/health/ready";
        public const string PaywallCookieName = "LightningPaymentsSession";
        public const long MaxWebhookBodyBytes = 64 * 1024;

        // Central invoice description validation regex (shared so both offline and live path use same rules)
        public static readonly Regex DescriptionAllowed = new(@"^[\w\s.,'?!@#$%^&*()_+\-\=\[\]{}|;:]*$", RegexOptions.Compiled);

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
        /// Optional webhook URL for confirmations resilience.
        /// </summary>
        [Url]
        public string? WebhookUrl { get; init; }

        /// <summary>
        /// The database connection string. Defaults to a local file under App_Data.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string ConnectionString { get; init; } = DefaultConnectionString;

        /// <summary>
        /// Optional admin email for notifications. Not required.
        /// </summary>
        public string AdminEmail { get; init; } = string.Empty;

        /// <summary>
        /// Optional webhook secret for HMAC verification (recommended when WebhookUrl is set).
        /// </summary>
        public string WebhookSecret { get; init; } = string.Empty;

        /// <summary>
        /// The Application Insights connection string for monitoring (optional).
        /// </summary>
        public string ApplicationInsightsConnectionString { get; init; } = string.Empty;

        /// <summary>
        /// Lightning network (Mainnet default).
        /// </summary>
        public LightningNetwork Network { get; init; } = LightningNetwork.Mainnet;

        /// <summary>
        /// Health check endpoint path. Defaults to "/health/ready".
        /// </summary>
        [MinLength(1)]
        public string HealthCheckPath { get; init; } = DefaultHealthPath;

        /// <summary>
        /// Session cookie SameSite mode. Defaults to Strict.
        /// </summary>
        public SameSiteMode SessionCookieSameSite { get; init; } = SameSiteMode.Strict;

        /// <summary>
        /// Optional cookie domain for subdomain scoping (e.g. .example.com).
        /// </summary>
        public string? SessionCookieDomain { get; init; }

        /// <summary>
        /// Optional working directory for the Breez SDK. If not set, defaults to '<content-root>/App_Data/LightningPayments/'.
        /// </summary>
        public string? WorkingDirectory { get; init; }

        // SMTP settings removed from "required" usage; email is disabled by default.
        public string SmtpHost { get; init; } = string.Empty;
        public int SmtpPort { get; init; } = 587;
        public string SmtpUsername { get; init; } = string.Empty;
        public string SmtpPassword { get; init; } = string.Empty;
        public string FromEmailAddress { get; init; } = string.Empty;

        /// <summary>
        /// Maximum invoice amount in satoshis.
        /// </summary>
        [Range(1, ulong.MaxValue)]
        public ulong MaxInvoiceAmountSat { get; init; } = 10_000_000;

        /// <summary>
        /// Maximum length of the invoice description.
        /// </summary>
        [Range(1, 1024)]
        public int MaxInvoiceDescriptionLength { get; init; } = 200;
    }
}
