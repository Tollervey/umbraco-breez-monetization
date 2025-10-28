using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    /// <summary>
    /// Health check that reports whether the Breez SDK is connected.
    /// </summary>
    public class BreezSdkHealthCheck : IHealthCheck
    {
        private readonly IBreezSdkService _service;

        /// <summary>
        /// Creates a new <see cref="BreezSdkHealthCheck"/>.
        /// </summary>
        public BreezSdkHealthCheck(IBreezSdkService service)
        {
            _service = service;
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                bool isConnected = await _service.IsConnectedAsync(cancellationToken);
                return isConnected ? HealthCheckResult.Healthy("Breez SDK is connected.") : HealthCheckResult.Unhealthy("Breez SDK is not connected.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Error checking Breez SDK connection.", ex);
            }
        }
    }
}