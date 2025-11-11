namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
    /// <summary>
    /// Service for handling paywall message operations.
    /// </summary>
    public interface IPaywallMessageService
    {
        /// <summary>
        /// Gets the default paywall message.
        /// </summary>
        string GetDefaultMessage();

        /// <summary>
        /// Validates a paywall message.
        /// </summary>
        bool IsValidMessage(string message);
    }
}