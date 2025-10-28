using System.ComponentModel.DataAnnotations;

namespace Tollervey.Umbraco.LightningPayments.UI.Configuration
{
    /// <summary>
    /// Options controlling behavior when running in offline (mocked) mode.
    /// </summary>
    public class OfflineLightningPaymentsOptions
    {
        /// <summary>
        /// Delay before simulating a payment confirmation.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int SimulatedConfirmationDelayMs { get; set; } = 1200;

        /// <summary>
        /// When true, replace the persistent state service with an in-memory implementation.
        /// </summary>
        public bool UseInMemoryStateService { get; set; } = false;

        /// <summary>
        /// Optional failure rate (0.0 to 1.0). If > 0, random failures may be simulated for invoice creation.
        /// </summary>
        [Range(0.0, 1.0)]
        public double SimulatedFailureRate { get; set; } = 0.0;
    }
}