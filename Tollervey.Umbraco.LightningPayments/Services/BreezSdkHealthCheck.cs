using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public class BreezSdkHealthCheck : IHealthCheck
    {
        private readonly IBreezSdkService _service;

        public BreezSdkHealthCheck(IBreezSdkService service)
        {
            _service = service;
        }

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