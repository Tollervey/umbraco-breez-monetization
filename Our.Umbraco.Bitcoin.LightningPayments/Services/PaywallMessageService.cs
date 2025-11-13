using Microsoft.Extensions.Logging;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
    /// <summary>
    /// Implementation of IPaywallMessageService for POC paywall message handling.
    /// </summary>
    public class PaywallMessageService : IPaywallMessageService
    {
        private readonly ILogger<PaywallMessageService> _logger;
        private static string? _customMessage = "Default paywall message";

        public PaywallMessageService(ILogger<PaywallMessageService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the default paywall message.
        /// </summary>
        public string GetDefaultMessage() => "Default paywall message";

        /// <summary>
        /// Gets the paywall message.
        /// </summary>
        public string GetMessage() => _customMessage ?? GetDefaultMessage();

        /// <summary>
        /// Sets the paywall message.
        /// </summary>
        public void SetMessage(string? message)
        {
            if (IsValidMessage(message))
            {
                _customMessage = message;
                _logger.LogInformation("Paywall message updated to: {Message}", message);
            }
        }

        /// <summary>
        /// Validates a paywall message (e.g., not null/empty, length check).
        /// </summary>
        public bool IsValidMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("Paywall message is null or empty.");
                return false;
            }
            if (message.Length > 500) // Arbitrary limit for POC
            {
                _logger.LogWarning("Paywall message exceeds maximum length of 500 characters.");
                return false;
            }
            return true;
        }
    }
}