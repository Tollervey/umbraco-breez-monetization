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
        /// Gets the paywall message.
        /// </summary>
        string GetMessage();

        /// <summary>
        /// Sets the paywall message.
        /// </summary>
        void SetMessage(string message);

        /// <summary>
        /// Validates a paywall message.
        /// </summary>
        bool IsValidMessage(string message);
    }
}