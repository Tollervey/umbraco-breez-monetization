using Microsoft.Extensions.Diagnostics.HealthChecks;
using Our.Umbraco.Bitcoin.LightningPayments.Services;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services;

public class LightningPaymentsHealthCheck : IHealthCheck
{
    private readonly IPaymentStateService _paymentStateService;

    public LightningPaymentsHealthCheck(IPaymentStateService paymentStateService)
    {
        _paymentStateService = paymentStateService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Example: Check if payment service is operational
        try
        {
            // Perform a lightweight check, e.g., verify SDK connection or recent payments
            var isHealthy = await _paymentStateService.IsServiceHealthyAsync();
            return isHealthy
                ? HealthCheckResult.Healthy("Lightning payments service is operational.")
                : HealthCheckResult.Unhealthy("Lightning payments service is not responding.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error checking lightning payments health.", ex);
        }
    }
  }