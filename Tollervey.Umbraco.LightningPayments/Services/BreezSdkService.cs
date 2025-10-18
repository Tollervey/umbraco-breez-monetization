using Breez.Sdk.Liquid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Tollervey.Umbraco.LightningPayments.Configuration;
using Tollervey.Umbraco.LightningPayments.Models;
using Umbraco.Cms.Core.Hosting;

namespace Tollervey.Umbraco.LightningPayments.Services
{
    public class BreezSdkService : IBreezSdkService
    {
        private readonly ILogger<BreezSdkService> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly LightningPaymentsSettings _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<Task<BindingLiquidSdk?>> _sdkInstance;
        private static readonly SemaphoreSlim _initSemaphore = new(1, 1);

        public BreezSdkService(IOptions<LightningPaymentsSettings> settings, IHostEnvironment hostEnvironment, IServiceProvider serviceProvider, ILogger<BreezSdkService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _serviceProvider = serviceProvider;
            _sdkInstance = new Lazy<Task<BindingLiquidSdk?>>(InitializeSdkAsync, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private async Task<BindingLiquidSdk?> InitializeSdkAsync()
        {
            await _initSemaphore.WaitAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(_settings.BreezApiKey) || string.IsNullOrWhiteSpace(_settings.Mnemonic))
                {
                    _logger.LogWarning("Breez SDK credentials are not configured. Service will not start.");
                    return null;
                }

                _logger.LogInformation("Initializing Breez SDK...");
                BreezSdkLiquidMethods.SetLogger(new SdkLogger(_logger));

                var workingDir = Path.Combine(_hostEnvironment.ContentRootPath, $"App_Data/{LightningPaymentsSettings.SectionName}/");
                if (!Directory.Exists(workingDir))
                {
                    Directory.CreateDirectory(workingDir);
                }

                LiquidNetwork network = _settings.Network switch
                {
                    LightningPaymentsSettings.LightningNetwork.Mainnet => LiquidNetwork.Mainnet,
                    LightningPaymentsSettings.LightningNetwork.Testnet => LiquidNetwork.Testnet,
                    LightningPaymentsSettings.LightningNetwork.Regtest => LiquidNetwork.Regtest,
                    _ => LiquidNetwork.Mainnet
                };

                if (network == LiquidNetwork.Mainnet && _settings.Network != LightningPaymentsSettings.LightningNetwork.Mainnet)
                {
                    _logger.LogWarning("Invalid network setting '{Network}', defaulting to Mainnet.", _settings.Network);
                }

                var config = BreezSdkLiquidMethods.DefaultConfig(network, _settings.BreezApiKey) with { workingDir = workingDir };
                var connectRequest = new ConnectRequest(config, _settings.Mnemonic);

                var sdk = await Task.Run(() => BreezSdkLiquidMethods.Connect(connectRequest));
                sdk.AddEventListener(new SdkEventListener(_serviceProvider));
                _logger.LogInformation("Breez SDK connected successfully.");

                if (!string.IsNullOrWhiteSpace(_settings.WebhookUrl))
                {
                    await Task.Run(() => sdk.RegisterWebhook(_settings.WebhookUrl));
                    _logger.LogInformation("Breez SDK webhook registered for URL: {WebhookUrl}", _settings.WebhookUrl);
                }

                return sdk;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Breez SDK.");
                return null;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        public async Task<string> CreateInvoiceAsync(ulong amountSat, string description)
        {
            var sdk = await _sdkInstance.Value;
            if (sdk == null)
            {
                throw new InvalidOperationException("Breez SDK is not connected.");
            }

            try
            {
                var optionalAmount = new ReceiveAmount.Bitcoin(amountSat);
                var prepareRequest = new PrepareReceiveRequest(PaymentMethod.Bolt11Invoice, optionalAmount);
                var prepareResponse = await Task.Run(() => sdk.PrepareReceivePayment(prepareRequest));
                _logger.LogInformation("Breez SDK invoice creation fee: {FeeSat} sats", prepareResponse.feesSat);

                var req = new ReceivePaymentRequest(prepareResponse, description);
                var res = await Task.Run(() => sdk.ReceivePayment(req));
                return res.destination;
            }
            catch (Exception ex)
            {
                throw new InvoiceException("Failed to create invoice via Breez SDK.", ex);
            }
        }

        public async Task<string> CreateBolt12OfferAsync(ulong amountSat, string description)
        {
            var sdk = await _sdkInstance.Value;
            if (sdk == null)
            {
                throw new InvalidOperationException("Breez SDK is not connected.");
            }

            try
            {
                var optionalAmount = new ReceiveAmount.Bitcoin(amountSat);
                var prepareRequest = new PrepareReceiveRequest(PaymentMethod.Bolt12Offer, optionalAmount);
                var prepareResponse = await Task.Run(() => sdk.PrepareReceivePayment(prepareRequest));
                _logger.LogInformation("Breez SDK offer creation fee: {FeeSat} sats", prepareResponse.feesSat);

                var req = new ReceivePaymentRequest(prepareResponse, description);
                var res = await Task.Run(() => sdk.ReceivePayment(req));
                return res.destination;
            }
            catch (Exception ex)
            {
                throw new InvoiceException("Failed to create Bolt12 offer via Breez SDK.", ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_sdkInstance.IsValueCreated)
            {
                var sdk = await _sdkInstance.Value;
                try
                {
                    sdk?.Disconnect();
                    _logger.LogInformation("Breez SDK disconnected.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting from Breez SDK.");
                }
            }
        }

        private class SdkLogger : Logger
        {
            private readonly ILogger<BreezSdkService> _logger;
            public SdkLogger(ILogger<BreezSdkService> logger) => _logger = logger;
            public void Log(LogEntry l) => _logger.LogInformation("BreezSDK: [{level}]: {line}", l.level, l.line);
        }

        private class SdkEventListener : EventListener
        {
            private readonly ILogger<BreezSdkService> _logger;
            private readonly IServiceProvider _serviceProvider;

            public SdkEventListener(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
                _logger = serviceProvider.GetRequiredService<ILogger<BreezSdkService>>();
            }

            public void OnEvent(SdkEvent e)
            {
                _logger.LogInformation("BreezSDK: Received event of type {EventType}: {EventDetails}", e.GetType().Name, e.ToString());

                if (e is SdkEvent.PaymentSucceeded succeeded)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentStateService>();
                            dynamic succeededDynamic = succeeded;
                            string paymentHash = succeededDynamic.details.paymentHash;
                            await paymentService.ConfirmPaymentAsync(paymentHash);
                            _logger.LogInformation("Confirmed payment in real-time for hash: {PaymentHash}", paymentHash);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to confirm payment from SDK event.");
                        }
                    });
                }
            }
        }
    }
}
