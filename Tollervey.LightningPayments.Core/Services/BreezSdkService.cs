using System;
using System.Threading;
using Breez.Sdk.Liquid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tollervey.LightningPayments.Breez.Configuration;
using Tollervey.LightningPayments.Breez.Models;
using Polly;
using System.Net.Http;
using System.Net.Sockets;

namespace Tollervey.LightningPayments.Breez.Services
{
    public class BreezSdkService : IBreezSdkService, IAsyncDisposable
    {
        private readonly ILogger<BreezSdkService> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly LightningPaymentsSettings _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly IBreezSdkWrapper _wrapper;
        private readonly Lazy<Task<BindingLiquidSdk?>> _sdkInstance;
        private static readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private int _disposed = 0;

        private readonly IAsyncPolicy<BindingLiquidSdk> _connectPolicy;
        private readonly IAsyncPolicy _webhookPolicy;
        private readonly IAsyncPolicy<PrepareReceiveResponse> _preparePolicy;
        private readonly IAsyncPolicy<ReceivePaymentResponse> _receivePolicy;

        public BreezSdkService(IOptions<LightningPaymentsSettings> settings, IHostEnvironment hostEnvironment, IServiceProvider serviceProvider, ILogger<BreezSdkService> logger, IBreezSdkWrapper wrapper)
        {
            _settings = settings.Value;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _serviceProvider = serviceProvider;
            _wrapper = wrapper;
            _sdkInstance = new Lazy<Task<BindingLiquidSdk?>>(() => InitializeSdkAsync(), LazyThreadSafetyMode.ExecutionAndPublication);

            // Initialize resiliency policies
            var retryDelay = (int attempt) => TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));

            // Connect policy
            var connectTimeout = Policy.TimeoutAsync<BindingLiquidSdk>(TimeSpan.FromSeconds(30));
            var connectRetry = Policy<BindingLiquidSdk>.Handle<Exception>()
                .WaitAndRetryAsync(3, retryDelay,
                onRetryAsync: (outcome, ts, attempt, ctx) => { _logger.LogWarning("Retry {Attempt} for connect after {TimeSpan.TotalSeconds}s due to {Message}", attempt, ts, outcome.Exception?.Message ?? "unknown error"); return Task.CompletedTask; });
            _connectPolicy = connectRetry.WrapAsync(connectTimeout);

            // Webhook policy
            var webhookTimeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(30));
            var webhookRetry = Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, retryDelay,
                onRetryAsync: (ex, ts, attempt, ctx) => { _logger.LogWarning("Retry {Attempt} for webhook registration after {TimeSpan.TotalSeconds}s due to {Message}", attempt, ts, ex.Message); return Task.CompletedTask; });
            _webhookPolicy = webhookRetry.WrapAsync(webhookTimeout);

            // Prepare policy (conservative)
            var prepareTimeout = Policy.TimeoutAsync<PrepareReceiveResponse>(TimeSpan.FromSeconds(10));
            var prepareRetry = Policy<PrepareReceiveResponse>.Handle<TimeoutException>().Or<HttpRequestException>().Or<SocketException>()
                .WaitAndRetryAsync(1, _ => TimeSpan.FromSeconds(2),
                onRetryAsync: (outcome, ts, attempt, ctx) => { _logger.LogWarning("Retry {Attempt} for prepare receive after {TimeSpan.TotalSeconds}s due to {Message}", attempt, ts, outcome.Exception?.Message ?? "unknown error"); return Task.CompletedTask; });
            _preparePolicy = prepareRetry.WrapAsync(prepareTimeout);

            // Receive policy (conservative)
            var receiveTimeout = Policy.TimeoutAsync<ReceivePaymentResponse>(TimeSpan.FromSeconds(10));
            var receiveRetry = Policy<ReceivePaymentResponse>.Handle<TimeoutException>().Or<HttpRequestException>().Or<SocketException>()
                .WaitAndRetryAsync(1, _ => TimeSpan.FromSeconds(2),
                onRetryAsync: (outcome, ts, attempt, ctx) => { _logger.LogWarning("Retry {Attempt} for receive payment after {TimeSpan.TotalSeconds}s due to {Message}", attempt, ts, outcome.Exception?.Message ?? "unknown error"); return Task.CompletedTask; });
            _receivePolicy = receiveRetry.WrapAsync(receiveTimeout);
        }

        private async Task<BindingLiquidSdk?> InitializeSdkAsync(CancellationToken ct = default)
        {
            bool acquired = false;
            try
            {
                await _initSemaphore.WaitAsync(ct);
                acquired = true;

                _logger.LogInformation("Initializing Breez SDK...");
                _wrapper.SetLogger(new SdkLogger(_logger));

                var workingDir = Path.Combine(_hostEnvironment.ContentRootPath, $"App_Data/{LightningPaymentsSettings.SectionName}/");
                if (!Directory.Exists(workingDir))
                {
                    Directory.CreateDirectory(workingDir);
                }

                LiquidNetwork network = _settings.Network switch
                {
                    LightningPaymentsSettings.LightningNetwork.Mainnet => LiquidNetwork.Mainnet,
                    LightningPaymentsSettings.LightningNetwork.Testnet => LiquidNetwork.Testnet,
                    LightningPaymentsSettings.LightningNetwork.Regtest => LiquidNetwork.Regtest
                };

                var config = _wrapper.DefaultConfig(network, _settings.BreezApiKey) with { workingDir = workingDir };
                var connectRequest = new ConnectRequest(config, _settings.Mnemonic);

                ct.ThrowIfCancellationRequested();
                var sdk = await _connectPolicy.ExecuteAsync((token) => Task.Run(() => _wrapper.Connect(connectRequest), token), ct);
                _wrapper.AddEventListener(sdk, new SdkEventListener(_serviceProvider, _cts.Token));
                _logger.LogInformation("Breez SDK connected successfully.");

                if (!string.IsNullOrWhiteSpace(_settings.WebhookUrl))
                {
                    ct.ThrowIfCancellationRequested();
                    await _webhookPolicy.ExecuteAsync((token) => Task.Run(() => _wrapper.RegisterWebhook(sdk, _settings.WebhookUrl), token), ct);
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
                if (acquired)
                {
                    _initSemaphore.Release();
                }
            }
        }

        public async Task<string> CreateInvoiceAsync(ulong amountSat, string description, CancellationToken ct = default)
        {
            var sdk = await _sdkInstance.Value.WaitAsync(ct);
            if (sdk == null)
            {
                throw new InvalidOperationException("Breez SDK is not connected.");
            }

            try
            {
                var optionalAmount = new ReceiveAmount.Bitcoin(amountSat);
                var prepareRequest = new PrepareReceiveRequest(PaymentMethod.Bolt11Invoice, optionalAmount);

                ct.ThrowIfCancellationRequested();
                var prepareResponse = await _preparePolicy.ExecuteAsync((token) => Task.Run(() => _wrapper.PrepareReceivePayment(sdk, prepareRequest), token), ct);
                _logger.LogInformation("Breez SDK invoice creation fee: {FeeSat} sats", prepareResponse.feesSat);

                var req = new ReceivePaymentRequest(prepareResponse, description);

                ct.ThrowIfCancellationRequested();
                var res = await _receivePolicy.ExecuteAsync((token) => Task.Run(() => _wrapper.ReceivePayment(sdk, req), token), ct);
                return res.destination;
            }
            catch (Exception ex)
            {
                throw new InvoiceException("Failed to create invoice via Breez SDK.", ex);
            }
        }

        public async Task<string> CreateBolt12OfferAsync(ulong amountSat, string description, CancellationToken ct = default)
        {
            var sdk = await _sdkInstance.Value.WaitAsync(ct);
            if (sdk == null)
            {
                throw new InvalidOperationException("Breez SDK is not connected.");
            }

            try
            {
                var optionalAmount = new ReceiveAmount.Bitcoin(amountSat);
                var prepareRequest = new PrepareReceiveRequest(PaymentMethod.Bolt12Offer, optionalAmount);

                ct.ThrowIfCancellationRequested();
                var prepareResponse = await _preparePolicy.ExecuteAsync((token) => Task.Run(() => _wrapper.PrepareReceivePayment(sdk, prepareRequest), token), ct);
                _logger.LogInformation("Breez SDK offer creation fee: {FeeSat} sats", prepareResponse.feesSat);

                var req = new ReceivePaymentRequest(prepareResponse, description);

                ct.ThrowIfCancellationRequested();
                var res = await _receivePolicy.ExecuteAsync((token) => Task.Run(() => _wrapper.ReceivePayment(sdk, req), token), ct);
                return res.destination;
            }
            catch (Exception ex)
            {
                throw new InvoiceException("Failed to create Bolt12 offer via Breez SDK.", ex);
            }
        }

        public async Task<bool> IsConnectedAsync(CancellationToken ct = default)
        {
            var sdk = await _sdkInstance.Value.WaitAsync(ct);
            return sdk != null;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            if (_sdkInstance.IsValueCreated)
            {
                var sdk = await _sdkInstance.Value;
                try
                {
                    if (sdk != null)
                    {
                        _wrapper.Disconnect(sdk);
                    }
                    _logger.LogInformation("Breez SDK disconnected.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting from Breez SDK.");
                }
            }

            _cts.Cancel();
            _cts.Dispose();
        }

        internal class SdkLogger : Logger
        {
            private readonly ILogger<BreezSdkService> _logger;
            public SdkLogger(ILogger<BreezSdkService> logger) => _logger = logger;
            public void Log(LogEntry l) => _logger.LogInformation("BreezSDK: [{level}]: {line}", l.level, l.line);
        }

        internal class SdkEventListener : EventListener
        {
            private readonly ILogger<BreezSdkService> _logger;
            private readonly IServiceProvider _serviceProvider;
            private readonly CancellationToken _token;

            public SdkEventListener(IServiceProvider serviceProvider, CancellationToken token)
            {
                _serviceProvider = serviceProvider;
                _token = token;
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
                            _token.ThrowIfCancellationRequested();
                            using var scope = _serviceProvider.CreateScope();
                            var deduper = scope.ServiceProvider.GetRequiredService<IPaymentEventDeduper>();
                            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentStateService>();

                            string? paymentHash = null;
                            try
                            {
                                var detailsProp = succeeded.GetType().GetProperty("details");
                                if (detailsProp != null)
                                {
                                    var details = detailsProp.GetValue(succeeded);
                                    if (details != null)
                                    {
                                        var hashProp = details.GetType().GetProperty("paymentHash");
                                        if (hashProp != null)
                                        {
                                            paymentHash = hashProp.GetValue(details) as string;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to extract paymentHash using reflection from SDK event.");
                            }

                            if (string.IsNullOrEmpty(paymentHash))
                            {
                                _logger.LogWarning("Unable to extract paymentHash from PaymentSucceeded event.");
                                return;
                            }

                            if (!deduper.TryBegin(paymentHash))
                            {
                                _logger.LogInformation("Duplicate payment succeeded event for hash: {PaymentHash}", paymentHash);
                                return;
                            }

                            _token.ThrowIfCancellationRequested();
                            await paymentService.ConfirmPaymentAsync(paymentHash);
                            _logger.LogInformation("Confirmed payment in real-time for hash: {PaymentHash}", paymentHash);
                            deduper.Complete(paymentHash);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to confirm payment from SDK event.");
                        }
                    }, _token);
                }
            }
        }
    }
}
