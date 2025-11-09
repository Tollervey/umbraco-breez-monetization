using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;

namespace Our.Umbraco.Bitcoin.LightningPayments.Configuration
{
    /// <summary>
    /// Validates <see cref="LightningPaymentsSettings"/> using data annotations at startup.
    /// Also enforces a fail-fast check in Production to prevent embedding secrets in appsettings.
    /// </summary>
    public class LightningPaymentsSettingsValidator : IValidateOptions<LightningPaymentsSettings>
    {
        private readonly ILogger<LightningPaymentsSettingsValidator> _logger;
        private readonly IHostEnvironment _env;

        /// <summary>
        /// Creates a new validator instance.
        /// </summary>
        public LightningPaymentsSettingsValidator(ILogger<LightningPaymentsSettingsValidator> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        /// <inheritdoc />
        public ValidateOptionsResult Validate(string? name, LightningPaymentsSettings options)
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(options);
            var isValid = Validator.TryValidateObject(options, context, validationResults, true);

            // Log annotation failures and prepare errors
            var errors = new List<string>();
            if (!isValid)
            {
                foreach (var validationResult in validationResults)
                {
                    // Log each validation error individually for clarity
                    _logger.LogCritical("Configuration validation failed for '{MemberNames}': {ErrorMessage}",
                        string.Join(", ", validationResult.MemberNames),
                        validationResult.ErrorMessage);
                    errors.Add(validationResult.ErrorMessage ?? "Unknown validation error.");
                }

                return ValidateOptionsResult.Fail(errors);
            }

            // Additional production-only checks to avoid placing secrets in appsettings.json
            if (_env.IsProduction())
            {
                var leaked = new List<string>();

                // Helper to check if value appears to come from environment variable
                static bool HasEnv(string key) => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key));

                if (!string.IsNullOrWhiteSpace(options.BreezApiKey) && !HasEnv("LightningPayments__BreezApiKey")) leaked.Add("BreezApiKey");
                if (!string.IsNullOrWhiteSpace(options.Mnemonic) && !HasEnv("LightningPayments__Mnemonic")) leaked.Add("Mnemonic");
                if (!string.IsNullOrWhiteSpace(options.WebhookSecret) && !HasEnv("LightningPayments__WebhookSecret")) leaked.Add("WebhookSecret");

                if (leaked.Any())
                {
                    var msg = $"Detected secret-like configuration values present in app configuration for: {string.Join(", ", leaked)}. " +
                        "In Production you must provide secrets via environment variables, a secret store (e.g., Azure Key Vault), or another secure configuration provider. " +
                        "Remove these values from appsettings.json and supply them via a secure provider. For local development, use 'dotnet user-secrets'.";
                    _logger.LogCritical(msg);
                    errors.Add(msg);
                    return ValidateOptionsResult.Fail(errors);
                }
            }

            return ValidateOptionsResult.Success;
        }
    }
}
