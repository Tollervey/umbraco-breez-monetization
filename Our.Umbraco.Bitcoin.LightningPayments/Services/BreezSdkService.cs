using Breez.Sdk.Liquid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Our.Umbraco.Bitcoin.LightningPayments.Configuration;
using Our.Umbraco.Bitcoin.LightningPayments.Models;
using Our.Umbraco.Bitcoin.LightningPayments.Services;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
    /// <summary>
    /// Breez SDK integration service used by Umbraco controllers/components.
    /// Implements the receive-side flows and event wiring recommended by the Breez UX Guidelines.
    ///
    /// UX references:
    /// - Overall: https://sdk-doc-liquid.breez.technology/guide/uxguide.html
    /// - Receive: https://sdk-doc-liquid.breez.technology/guide/uxguide_receive.html
    /// - Display: https://sdk-doc-liquid.breez.technology/guide/uxguide_display.html
    /// - Seed/Key mgmt: https://sdk-doc-liquid.breez.technology/guide/uxguide_seed.html
    ///
    /// Notes for Umbraco UI implementers:
    /// - This service exposes high-level methods to create Lightning payment requests (BOLT11/BOLT12) and
    /// to look up payments by hash for display/history.
    /// - It also subscribes to SDK events and forwards them to an <see cref="IBreezEventProcessor"/> that you can
    /// implement to update your UI state (e.g., Pending ? Succeeded/Failed), matching the UX guideline on
    /// interacting with SDK events.
    /// - If a Webhook URL is configured, it is registered on connect to enable offline receiving flows required
    /// for LNURL-Pay in the Liquid implementation (see the Receive guidelines).
    /// </summary>
    public class BreezSdkService : IBreezSdkService, IBreezSdkHandleProvider, IAsyncDisposable
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
        private readonly IServiceProvider _serviceProvider;

        public BreezSdkService(IOptions<LightningPaymentsSettings> settings, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory, ILogger<BreezSdkService> logger, IBreezSdkWrapper wrapper, IServiceProvider serviceProvider)
        {
            _settings = settings.Value;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _wrapper = wrapper;
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
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

                // Determine working directory: use configured path if provided, otherwise default under content root.
                string workingDir;
                if (!string.IsNullOrWhiteSpace(_settings.WorkingDirectory))
                {
                    workingDir = _settings.WorkingDirectory!;
                    // If a relative path was provided, make it relative to content root for predictability
                    if (!Path.IsPathRooted(workingDir))
                    {
                        workingDir = Path.Combine(_hostEnvironment.ContentRootPath, workingDir);
                    }
                    _logger.LogInformation("Using configured Breez SDK working directory: {WorkingDir}", workingDir);
                }
                else
                {
                    workingDir = Path.Combine(_hostEnvironment.ContentRootPath, $"App_Data/{LightningPaymentsSettings.SectionName}/");
                    _logger.LogInformation("Using default Breez SDK working directory under content root: {WorkingDir}", workingDir);

                    // In production recommend configuring a dedicated secure path outside the webroot
                    if (_hostEnvironment.IsProduction())
                    {
                        _logger.LogWarning("Default SDK working directory is under the application's content root. For security, consider configuring 'LightningPayments:WorkingDirectory' to a dedicated secure path outside the webroot and apply restrictive filesystem ACLs.");
                    }
                }

                if (!Directory.Exists(workingDir))
                {
                    Directory.CreateDirectory(workingDir);
                }

                // Attempt to apply conservative filesystem permissions on the working directory
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(workingDir);
                            var dirSecurity = dirInfo.GetAccessControl();
                            // Protect ACL from inheritance and remove existing rules for Everyone
                            dirSecurity.SetAccessRuleProtection(true, false);

                            var identity = WindowsIdentity.GetCurrent();
                            var userSid = identity?.User;
                            if (userSid != null)
                            {
                                var rule = new FileSystemAccessRule(userSid, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
                                dirSecurity.AddAccessRule(rule);
                                dirInfo.SetAccessControl(dirSecurity);
                                _logger.LogInformation("Applied restrictive ACLs to Breez SDK working directory.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to apply Windows ACLs to Breez SDK working directory. Ensure the directory has appropriate permissions.");
                        }
                    }
                    else
                    {
                        // Try to chmod0700 on Unix-like systems. Best-effort: ignore failures.
                        try
                        {
                            var psi = new ProcessStartInfo("chmod", $"700 \"{workingDir}\"") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                            using var p = Process.Start(psi);
                            if (p != null)
                            {
                                await p.WaitForExitAsync(ct);
                                if (p.ExitCode == 0)
                                {
                                    _logger.LogInformation("Applied chmod700 to Breez SDK working directory.");
                                }
                                else
                                {
                                    _logger.LogDebug("chmod exited with code {ExitCode} when attempting to set permissions on {Dir}", p.ExitCode, workingDir);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Failed to apply chmod to Breez SDK working directory; ensure permissions are set appropriately.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Non-fatal error while attempting to secure working directory.");
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
                _eventListener = new SdkEventListener(_serviceProvider, _loggerFactory.CreateLogger<SdkEventListener>(), () => _disposed == 1);
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
                // Pre-check lightning receive limits for bolt11/bolt12
                if (paymentMethod is PaymentMethod.Bolt11Invoice or PaymentMethod.Bolt12Offer)
                {
                    var limits = await _wrapper.FetchLightningLimitsAsync(sdk, ct);
                    var min = limits.receive.minSat;
                    var max = limits.receive.maxSat;
                    if (amountSat < (ulong)min || amountSat > (ulong)max)
                    {
                        _logger.LogWarning("Requested amount {Amount} outside lightning receive limits [{Min}, {Max}]", amountSat, min, max);
                        throw new InvalidInvoiceRequestException($"Amount must be between {min} and {max} sats.");
                    }
                }

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

        /// <summary>
        /// Provides the connected SDK instance for internal/advanced operations via a facade.
        /// Not intended to be called directly by UI; prefer <see cref="IBreezPaymentsFacade"/>.
        /// </summary>
        public async Task<BindingLiquidSdk?> GetSdkAsync(CancellationToken ct = default)
        {
            return await _sdkInstance.Value.WaitAsync(ct);
        }

        /// <summary>
        /// Creates a Lightning payment request using the BOLT11 standard (one-off invoice with amount and optional description).
        ///
        /// UI guidance (Receive UX):
        /// - Prefer LNURL-Pay as the default reusable identifier in your UI and fall back to BOLT11 for a specified amount
        /// request when needed. This method covers that one-off flow.
        /// - Before showing the confirmation screen, display limits/fees to the user. This service pre-checks Lightning
        /// receive limits and will throw if outside the min/max. You can also surface limits to the UI using the wrapper
        /// (see <see cref="IBreezSdkWrapper.FetchLightningLimitsAsync"/>).
        /// - Show a QR and a Copy/Share affordance in the UI (copy the BOLT11 string, share payload as needed).
        ///
        /// References:
        /// - https://sdk-doc-liquid.breez.technology/guide/uxguide_receive.html
        /// - https://sdk-doc-liquid.breez.technology/guide/uxguide_display.html
        /// </summary>
        public Task<string> CreateInvoiceAsync(ulong amountSat, string description, CancellationToken ct = default)
        {
            return CreatePaymentAsync(amountSat, description, PaymentMethod.Bolt11Invoice, "invoice", ct);
        }

        /// <summary>
        /// Creates a Lightning payment request using BOLT12 offer (Liquid implementation only).
        ///
        /// UI guidance (Receive UX):
        /// - If you support BOLT12, surface the same Lightning address and enhance with BIP-353 as recommended.
        /// - Treat the returned string as a share/copy target, similar to BOLT11; ensure limits/fees are displayed.
        ///
        /// References:
        /// - https://sdk-doc-liquid.breez.technology/guide/uxguide_receive.html
        /// </summary>
        public Task<string> CreateBolt12OfferAsync(ulong amountSat, string description, CancellationToken ct = default)
        {
            return CreatePaymentAsync(amountSat, description, PaymentMethod.Bolt12Offer, "Bolt12 offer", ct);
        }

        /// <summary>
        /// Indicates whether the underlying Breez SDK is connected and ready.
        /// Use to gate UI features that require a connected wallet.
        /// </summary>
        public async Task<bool> IsConnectedAsync(CancellationToken ct = default)
        {
            var sdk = await _sdkInstance.Value.WaitAsync(ct);
            return sdk != null;
        }

        /// <summary>
        /// Attempts to parse a BOLT11 invoice to extract its payment hash using the SDK's <c>parse</c> facility.
        ///
        /// UI guidance (Display UX):
        /// - Use the payment hash to correlate local UI state with on-chain/Lightning payment entities.
        /// - When available, show technical metadata (invoice string, preimage) under a collapsible Details section.
        ///
        /// References:
        /// - https://sdk-doc-liquid.breez.technology/guide/uxguide_display.html
        /// - https://sdk-doc-liquid.breez.technology/guide/uxguide_send.html (Unified parser for pasted/scanned input)
        /// </summary>
        public async Task<string?> TryExtractPaymentHashAsync(string invoice, CancellationToken ct = default)
        {
            var sdk = await _sdkInstance.Value.WaitAsync(ct);
            if (sdk == null)
            {
                _logger.LogWarning("Breez SDK not connected; cannot parse invoice.");
                return null;
            }

            try
            {
                var parsed = await _wrapper.ParseAsync(sdk, invoice, ct);
                if (parsed is InputType.Bolt11 bolt11)
                {
                    // The Breez C# binding exposes `invoice` with fields like `paymentHash` (hex string)
                    var hash = bolt11.invoice.paymentHash;
                    return string.IsNullOrWhiteSpace(hash) ? null : hash.ToLowerInvariant();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse invoice using Breez SDK.");
                return null;
            }
        }

        /// <summary>
        /// Retrieves a single payment by its Lightning payment hash.
        ///
        /// UI guidance (Display UX):
        /// - Use this to populate a payment details screen. Display amount and fees separately, and expose invoice/preimage
        /// in a Details section. Represent current state (Pending/Succeeded/Failed) with distinct visuals.
        ///
        /// References:
        /// - https://sdk-doc-liquid.breez.technology/guide/uxguide_display.html
        /// </summary>
        public async Task<Payment?> GetPaymentByHashAsync(string paymentHash, CancellationToken ct = default)
        {
            var sdk = await _sdkInstance.Value.WaitAsync(ct);
            if (sdk == null)
            {
                throw new InvalidOperationException("Breez SDK is not connected.");
            }
            var req = new GetPaymentRequest.PaymentHash(paymentHash);
            return await _wrapper.GetPaymentAsync(sdk, req, ct);
        }

        /// <summary>
        /// Fetches a quote for the fees required to receive a payment of the specified amount.
        /// Uses the existing prepare policy to estimate the fees without creating a payment request.
        /// </summary>
        public async Task<long> GetReceiveFeeQuoteAsync(ulong amountSat, bool bolt12 = false, CancellationToken ct = default)
        {
            ValidateInvoiceAmount(amountSat);

            var sdk = await _sdkInstance.Value.WaitAsync(ct);
            if (sdk == null)
            {
                throw new InvalidOperationException("Breez SDK is not connected.");
            }

            try
            {
                var method = bolt12 ? PaymentMethod.Bolt12Offer : PaymentMethod.Bolt11Invoice;
                var optionalAmount = new ReceiveAmount.Bitcoin(amountSat);
                var prepareRequest = new PrepareReceiveRequest(method, optionalAmount);
                var prepareResponse = await _preparePolicy.ExecuteAsync((token) => _wrapper.PrepareReceivePaymentAsync(sdk, prepareRequest, token), ct);
                _logger.LogDebug("Receive fee quote for {Method}: {FeeSat} sats", bolt12 ? "BOLT12" : "BOLT11", prepareResponse.feesSat);
                return (long)prepareResponse.feesSat;
            }
            catch (Exception ex)
            {
                throw new InvoiceException("Failed to quote receive fees via Breez SDK.", ex);
            }
        }

        /// <summary>
        /// Retrieves recommended on-chain fees from the Breez SDK.
        /// It is a lightweight call that does not require a connected SDK instance.
        /// </summary>
        public async Task<RecommendedFees?> GetRecommendedFeesAsync(CancellationToken ct = default)
        {
            var sdk = await _sdkInstance.Value.WaitAsync(ct);
            if (sdk == null)
            {
                return null;
            }

            try
            {
                return await _wrapper.RecommendedFeesAsync(sdk, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch recommended on-chain fees.");
                return null;
            }
        }

        /// <summary>
        /// Disconnects and releases SDK resources. Called by Umbraco on app shutdown.
        /// Also removes the SDK event listener if supported by the underlying SDK.
        /// </summary>
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

            if (!LightningPaymentsSettings.DescriptionAllowed.IsMatch(description))
            {
                _logger.LogWarning("Invoice description contains invalid characters.");
                throw new InvalidInvoiceRequestException("Invoice description contains invalid characters.");
            }
        }

        /// <summary>
        /// Attempts to extract the expiry time of an invoice (if present) as a UTC DateTimeOffset.
        ///
        /// UI guidance:
        /// - Use this to show/hide a countdown timer for payment expiry in the UI.
        /// - Consider enhancing with on-chain confirmation checks for critical payments.
        ///
        /// References:
        /// - https://sdk-doc-liquid.breez.technology/guide/uxguide_display.html
        /// </summary>
        public async Task<DateTimeOffset?> TryExtractInvoiceExpiryAsync(string invoice, CancellationToken ct = default)
        {
            var sdk = await _sdkInstance.Value.WaitAsync(ct);
            if (sdk == null)
            {
                _logger.LogWarning("Breez SDK not connected; cannot parse invoice expiry.");
                return null;
            }
            try
            {
                var parsed = await _wrapper.ParseAsync(sdk, invoice, ct);
                if (parsed is InputType.Bolt11 bolt11)
                {
                    var inv = bolt11.invoice;
                    // Try common shapes via reflection to avoid tight coupling to binding versions
                    // Prefer absolute expiry (unix seconds)
                    var invType = inv.GetType();
                    long TryGetLongProp(string name)
                    {
                        var p = invType.GetProperty(name);
                        if (p == null) return 0;
                        var v = p.GetValue(inv);
                        if (v == null) return 0;
                        try { return Convert.ToInt64(v); } catch { return 0; }
                    }

                    var expiryUnix = TryGetLongProp("expiryTime"); // absolute epoch seconds
                    if (expiryUnix > 0)
                    {
                        return DateTimeOffset.FromUnixTimeSeconds(expiryUnix);
                    }

                    // Derive from creation timestamp + ttl
                    var createdUnix = TryGetLongProp("timestamp");
                    var ttl = TryGetLongProp("expiry"); // seconds from creation
                    if (createdUnix > 0 && ttl > 0)
                    {
                        return DateTimeOffset.FromUnixTimeSeconds(createdUnix + ttl);
                    }

                    // Some bindings may expose `expiresAt`
                    var expiresAt = TryGetLongProp("expiresAt");
                    if (expiresAt > 0)
                    {
                        return DateTimeOffset.FromUnixTimeSeconds(expiresAt);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse invoice expiry.");
            }
            return null;
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

        /// <summary>
        /// Listens to Breez SDK events and forwards them to <see cref="IBreezEventProcessor"/>.
        ///
        /// UI guidance:
        /// - Use these events to drive user-visible state transitions as recommended by the Send/Receive guidelines
        /// (e.g., show progress/pending, then success/failure with details). Keep technical details under an expandable
        /// section for power users.
        ///
        /// References:
        /// - Receive events: https://sdk-doc-liquid.breez.technology/guide/receive_payment.html#lightning-1
        /// - Send events: https://sdk-doc-liquid.breez.technology/guide/send_payment.html#lightning-1
        /// </summary>
        internal class SdkEventListener : EventListener
        {
            private readonly ILogger<SdkEventListener> _logger;
            private readonly IServiceProvider _serviceProvider;
            private readonly Func<bool> _isDisposed;

            public SdkEventListener(IServiceProvider serviceProvider, ILogger<SdkEventListener> logger, Func<bool> isDisposed)
            {
                _serviceProvider = serviceProvider;
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

                // Avoid logging the full event payload which may contain sensitive data (invoices, preimages, etc.).
                string eventType = e.GetType().Name;
                string? paymentHash = null;
                try
                {
                    // Try to extract a paymentHash if present using reflection. Keep this minimal and tolerant to binding changes.
                    var detailsProp = e.GetType().GetProperty("details");
                    if (detailsProp != null)
                    {
                        var details = detailsProp.GetValue(e);
                        if (details != null)
                        {
                            var paymentDetailsProp = details.GetType().GetProperty("details");
                            if (paymentDetailsProp != null)
                            {
                                var paymentDetails = paymentDetailsProp.GetValue(details);
                                if (paymentDetails != null)
                                {
                                    var hashProp = paymentDetails.GetType().GetProperty("paymentHash");
                                    if (hashProp != null)
                                    {
                                        paymentHash = hashProp.GetValue(paymentDetails) as string;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to extract paymentHash from SDK event for sanitized logging.");
                }

                if (!string.IsNullOrEmpty(paymentHash))
                {
                    _logger.LogInformation("BreezSDK: Received event {EventType} with paymentHash {PaymentHash}", eventType, paymentHash);
                }
                else
                {
                    _logger.LogInformation("BreezSDK: Received event {EventType}", eventType);
                }

                using var scope = _serviceProvider.CreateScope();
                var breezEventProcessor = scope.ServiceProvider.GetRequiredService<IBreezEventProcessor>();

                if (e is SdkEvent.PaymentSucceeded succeeded)
                {
                    _ = breezEventProcessor.EnqueueEvent(succeeded);
                }

                // Forward all events (including PaymentSucceeded) for generic broadcasting/logging. The processor will only broadcast sanitized fields.
                _ = breezEventProcessor.Enqueue(e);
            }
        }
    }
}

