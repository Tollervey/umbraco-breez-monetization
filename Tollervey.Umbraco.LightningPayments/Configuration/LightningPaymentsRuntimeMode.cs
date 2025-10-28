namespace Tollervey.Umbraco.LightningPayments.UI.Configuration
{
    /// <summary>
    /// Exposes runtime mode information (online vs offline) for Lightning Payments services.
    /// </summary>
    public interface ILightningPaymentsRuntimeMode
    {
        /// <summary>
        /// Gets a value indicating whether offline mode is enabled.
        /// </summary>
        bool IsOffline { get; }
    }

    internal sealed class LightningPaymentsRuntimeMode : ILightningPaymentsRuntimeMode
    {
        public bool IsOffline { get; }

        public LightningPaymentsRuntimeMode(bool isOffline)
        {
            IsOffline = isOffline;
        }
    }
}