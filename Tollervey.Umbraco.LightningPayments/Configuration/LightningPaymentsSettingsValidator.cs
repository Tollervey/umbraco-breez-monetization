using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace Tollervey.Umbraco.LightningPayments.UI.Configuration
{
    public class LightningPaymentsSettingsValidator : IValidateOptions<LightningPaymentsSettings>
    {
        private readonly ILogger<LightningPaymentsSettingsValidator> _logger;

        public LightningPaymentsSettingsValidator(ILogger<LightningPaymentsSettingsValidator> logger)
        {
            _logger = logger;
        }

        public ValidateOptionsResult Validate(string? name, LightningPaymentsSettings options)
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(options);
            var isValid = Validator.TryValidateObject(options, context, validationResults, true);

            if (isValid)
            {
                return ValidateOptionsResult.Success;
            }

            var errors = new List<string>();
            foreach (var validationResult in validationResults)
            {
                // Log each validation error individually for clarity
                _logger.LogCritical("Configuration validation failed for '{MemberNames}': {ErrorMessage}",
                    string.Join(", ", validationResult.MemberNames),
                    validationResult.ErrorMessage);
                errors.Add(validationResult.ErrorMessage ?? "Unknown validation error.");
            }

            // Return a failure result that will cause startup to fail, as intended
            return ValidateOptionsResult.Fail(errors);
        }
    }
}