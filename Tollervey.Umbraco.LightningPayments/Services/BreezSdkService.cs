using Breez.Sdk.Liquid;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Models;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public class BreezSdkService : IBreezSdkService, IAsyncDisposable
    {
        private static readonly ActivitySource _activity = new("BreezSdkService");
        private readonly ILogger<BreezSdkService> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly LightningPaymentsSettings _settings;
        private readonly IBreezSdkWrapper _wrapper;
        private readonly Lazy<Task<BindingLiquidSdk?>> _sdkInstance;
        private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private int _disposed = 0;
        private bool _webhookRegistered = false;
        private SdkEventListener? _eventListener;

        private readonly IAsyncPolicy<BindingLiquidSdk> _connectPolicy;
        private readonly IAsyncPolicy _webhookPolicy;
        private readonly IAsyncPolicy<PrepareReceiveResponse> _preparePolicy;
        private readonly IAsyncPolicy<ReceivePaymentResponse> _receivePolicy;

        private readonly ILoggerFactory _loggerFactory;
        private readonly IBreezEventProcessor _breezEventProcessor;

        public BreezSdkService(IOptions<LightningPaymentsSettings> settings, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory, ILogger<BreezSdkService> logger, IBreezSdkWrapper wrapper, IBreezEventProcessor breezEventProcessor)
        {
            _settings = settings.Value;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _wrapper = wrapper;
            _loggerFactory = loggerFactory;
            _breezEventProcessor = breezEventProcessor;
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
            using var activity = _activity.StartActivity(nameof(InitializeSdkAsync));
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
                activity?.SetTag("network", network.ToString());

                var config = _wrapper.DefaultConfig(network, _settings.BreezApiKey) with { workingDir = workingDir };
                var connectRequest = new ConnectRequest(config, _settings.Mnemonic);

                ct.ThrowIfCancellationRequested();
                var sdk = await _connectPolicy.ExecuteAsync((token) => _wrapper.ConnectAsync(connectRequest, token), ct);
                _eventListener = new SdkEventListener(_breezEventProcessor, _loggerFactory.CreateLogger<SdkEventListener>(), () => _disposed == 1);
                _wrapper.AddEventListener(sdk, _eventListener);
                _logger.LogInformation("Breez SDK connected successfully.");

                if (!string.IsNullOrWhiteSpace(_settings.WebhookUrl))
                {
                    if (ValidateWebhookUrl(_settings.WebhookUrl) && !_webhookRegistered)
                    {
                        ct.ThrowIfCancellationRequested();
                        // TODO: Implement challenge/verification on the receiver side to confirm the endpoint is valid.
                        await _webhookPolicy.ExecuteAsync(async (token) =>
                        {
                            await _wrapper.RegisterWebhookAsync(sdk, _settings.WebhookUrl, token);
                            _webhookRegistered = true;
                        }, ct);
                        // TODO: The webhook receiver should validate incoming requests using HMAC signatures.
                        _logger.LogInformation("Breez SDK webhook registered for URL: {WebhookUrl}", _settings.WebhookUrl);
                    }
                }

                activity?.SetStatus(ActivityStatusCode.Ok);
                return sdk;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
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

        private async Task<string> CreatePaymentAsync(ulong amountSat, string description, PaymentMethod paymentMethod, string paymentType, CancellationToken ct)
        {
            using var activity = _activity.StartActivity(nameof(CreatePaymentAsync));
            activity?.SetTag("amountSat", amountSat);
            activity?.SetTag("description.length", description.Length);
            activity?.SetTag("paymentMethod", paymentMethod.ToString());

            ValidateInvoiceAmount(amountSat);
            ValidateInvoiceDescription(description);

            var sdk = await _sdkInstance.Value.WaitAsync(ct);
            if (sdk == null)
            {
                var ex = new InvalidOperationException("Breez SDK is not connected.");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw ex;
            }

            try
            {
                var optionalAmount = new ReceiveAmount.Bitcoin(amountSat);
                var prepareRequest = new PrepareReceiveRequest(paymentMethod, optionalAmount);

                ct.ThrowIfCancellationRequested();
                var prepareResponse = await _preparePolicy.ExecuteAsync((token) => _wrapper.PrepareReceivePaymentAsync(sdk, prepareRequest, token), ct);
                _logger.LogInformation("Breez SDK {PaymentType} creation fee: {FeeSat} sats", paymentType, prepareResponse.feesSat);
                activity?.SetTag("feesSat", prepareResponse.feesSat);

                var req = new ReceivePaymentRequest(prepareResponse, description);

                ct.ThrowIfCancellationRequested();
                var res = await _receivePolicy.ExecuteAsync((token) => _wrapper.ReceivePaymentAsync(sdk, req, token), ct);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return res.destination;
            }
            catch (Exception ex) when (ex is not InvalidInvoiceRequestException)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw new InvoiceException($"Failed to create {paymentType} via Breez SDK.", ex);
            }
        }

        public Task<string> CreateInvoiceAsync(ulong amountSat, string description, CancellationToken ct = default)
        {
            return CreatePaymentAsync(amountSat, description, PaymentMethod.Bolt11Invoice, "invoice", ct);
        }

        public Task<string> CreateBolt12OfferAsync(ulong amountSat, string description, CancellationToken ct = default)
        {
            return CreatePaymentAsync(amountSat, description, PaymentMethod.Bolt12Offer, "Bolt12 offer", ct);
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

            // Cancel any ongoing operations
            _cts.Cancel();

            if (_sdkInstance.IsValueCreated)
            {
                var sdk = await _sdkInstance.Value;
                try
                {
                    if (sdk != null)
                    {
                        if (_eventListener != null)
                        {
                            // Although the SDK may not support removal, we call it for future-proofing.
                            _wrapper.RemoveEventListener(sdk, _eventListener);
                        }
                        await _wrapper.DisconnectAsync(sdk, _cts.Token);
                    }
                    _logger.LogInformation("Breez SDK disconnected.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting from Breez SDK.");
                }
            }

            _cts.Dispose();
        }

        private bool ValidateWebhookUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                _logger.LogWarning("Webhook URL '{WebhookUrl}' is not a valid URI.", url);
                return false;
            }

            if (uri.Scheme != "https")
            {
                _logger.LogWarning("Webhook URL '{WebhookUrl}' must use https scheme.", url);
                return false;
            }

            // Optional: Add hostname validation here if you want to restrict to a specific domain.
            // For example: if(uri.Host != "your-expected-host.com") { ... }

            return true;
        }

        internal void ValidateInvoiceAmount(ulong amountSat)
        {
            if (amountSat == 0)
            {
                _logger.LogWarning("Invoice amount must be greater than 0.");
                throw new InvalidInvoiceRequestException("Invoice amount must be greater than 0.");
            }

            if (amountSat > _settings.MaxInvoiceAmountSat)
            {
                _logger.LogWarning("Invoice amount {AmountSat} exceeds maximum of {MaxAmountSat}.", amountSat, _settings.MaxInvoiceAmountSat);
                throw new InvalidInvoiceRequestException($"Invoice amount exceeds maximum of {_settings.MaxInvoiceAmountSat}.");
            }
        }

        internal void ValidateInvoiceDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                _logger.LogWarning("Invoice description cannot be empty.");
                throw new InvalidInvoiceRequestException("Invoice description cannot be empty.");
            }

            if (description.Length > _settings.MaxInvoiceDescriptionLength)
            {
                _logger.LogWarning("Invoice description length {Length} exceeds maximum of {MaxLength}.", description.Length, _settings.MaxInvoiceDescriptionLength);
                throw new InvalidInvoiceRequestException($"Invoice description length exceeds maximum of {_settings.MaxInvoiceDescriptionLength}.");
            }

            if (!Regex.IsMatch(description, @"^[\w\s.,'?!@#$%^&*()_+-=\[\]{}|;:]*$"))
            {
                _logger.LogWarning("Invoice description contains invalid characters.");
                throw new InvalidInvoiceRequestException("Invoice description contains invalid characters.");
            }
        }

        internal class SdkLogger : Logger
        {
            private readonly ILogger<BreezSdkService> _logger;
            public SdkLogger(ILogger<BreezSdkService> logger) => _logger = logger;

            public void Log(LogEntry l)
            {
                var logLevel = l.level switch
                {
                    "TRACE" => LogLevel.Trace,
                    "DEBUG" => LogLevel.Debug,
                    "INFO" => LogLevel.Information,
                    "WARN" => LogLevel.Warning,
                    "ERROR" => LogLevel.Error,
                    "CRITICAL" => LogLevel.Critical,
                    _ => LogLevel.Information
                };

                _logger.Log(logLevel, "BreezSDK: [{level}]: {line}", l.level, l.line);
            }

        }

        internal class SdkEventListener : EventListener
        {
            private readonly ILogger<SdkEventListener> _logger;
            private readonly IBreezEventProcessor _breezEventProcessor;
            private readonly Func<bool> _isDisposed;

            public SdkEventListener(IBreezEventProcessor breezEventProcessor, ILogger<SdkEventListener> logger, Func<bool> isDisposed)
            {
                _breezEventProcessor = breezEventProcessor;
                _logger = logger;
                _isDisposed = isDisposed;
            }

            public void OnEvent(SdkEvent e)
            {
                if (_isDisposed())
                {
                    _logger.LogWarning("BreezSDK: Ignoring event of type {EventType} because the service is disposed.", e.GetType().Name);
                    return;
                }

                _logger.LogInformation("BreezSDK: Received event of type {EventType}: {EventDetails}", e.GetType().Name, e.ToString());

                if (e is SdkEvent.PaymentSucceeded succeeded)
                {
                    _ = _breezEventProcessor.EnqueueEvent(succeeded);
                }
            }
        }
    }
}
