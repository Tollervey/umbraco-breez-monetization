using Microsoft.Extensions.Options;
using Tollervey.LightningPayments.Breez.Configuration;

namespace Tollervey.Umbraco.LightningPayments.UI.Configuration
{
    public class LightningPaymentsSettingsValidator : IValidateOptions<LightningPaymentsSettings>
    {
        public ValidateOptionsResult Validate(string? name, LightningPaymentsSettings options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.BreezApiKey))
                failures.Add("Breez API key must be provided.");

            if (string.IsNullOrWhiteSpace(options.Mnemonic))
                failures.Add("Mnemonic must be provided.");

            if (!Enum.IsDefined(typeof(LightningPaymentsSettings.LightningNetwork), options.Network))
                failures.Add($"Invalid network value: {options.Network}. Must be one of: Mainnet, Testnet, Regtest.");

            if (failures.Count > 0)
            {
                return ValidateOptionsResult.Fail(string.Join(Environment.NewLine, failures));
            }

            return ValidateOptionsResult.Success;
        }
    }
}