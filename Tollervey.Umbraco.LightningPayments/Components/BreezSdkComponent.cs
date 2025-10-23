using Microsoft.Extensions.Logging;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Umbraco.Cms.Core.Composing;

namespace Tollervey.Umbraco.LightningPayments.UI.Components
{
    public class BreezSdkComponent : IComponent
    {
        private readonly IBreezSdkService _breezSdkService;
        private readonly ILogger<BreezSdkComponent> _logger;

        public BreezSdkComponent(IBreezSdkService breezSdkService, ILogger<BreezSdkComponent> logger)
        {
            _breezSdkService = breezSdkService;
            _logger = logger;
        }

        public void Initialize()
        {
            // Fire-and-forget initialization to avoid blocking Umbraco startup
            _ = Task.Run(async () =>
            {
                try
                {
                    var connected = await _breezSdkService.IsConnectedAsync();
                    _logger.LogInformation("Breez SDK initial connection attempt result: {Connected}", connected);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Breez SDK failed to initialize during application startup.");
                }
            });
        }

        public void Terminate()
        {
            try
            {
                // Ensure graceful shutdown of the SDK
                _breezSdkService.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _logger.LogInformation("Breez SDK disposed during application shutdown.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while disposing Breez SDK on shutdown.");
            }
        }
    }
}