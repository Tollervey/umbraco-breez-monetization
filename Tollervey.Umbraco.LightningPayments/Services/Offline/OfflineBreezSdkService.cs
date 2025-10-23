using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Models;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    /// <summary>
    /// Offline mock implementation that never calls Breez SDK but behaves as if connected.
    /// It returns a synthetic "invoice" string that embeds a payment hash and confirms it after a configurable delay.
    /// </summary>
    internal sealed class OfflineBreezSdkService : IBreezSdkService
    {
        private readonly ILogger<OfflineBreezSdkService> _logger;
        private readonly LightningPaymentsSettings _settings;
        private readonly OfflineLightningPaymentsOptions _offlineOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _cts = new();

        private static readonly Regex DescriptionAllowed = new(@"^[\w\s.,'?!@#$%^&*()_+\-=\[\]{}|;:]*$", RegexOptions.Compiled);

        public OfflineBreezSdkService(
            IOptions<LightningPaymentsSettings> settings,
            IOptions<OfflineLightningPaymentsOptions> offlineOptions,
            ILogger<OfflineBreezSdkService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _settings = settings.Value;
            _offlineOptions = offlineOptions.Value;
            _serviceProvider = serviceProvider;

            _logger.LogInformation("LightningPayments running in OFFLINE mode. No Breez SDK calls will be made.");
        }

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task<string> CreateBolt12OfferAsync(ulong amountSat, string description, CancellationToken ct = default)
        {
            Validate(amountSat, description);

            // Not parsed by consumers in current codepath; keep format consistent with offline invoice.
            var paymentHash = GeneratePaymentHash();
            // A lightweight synthetic marker for Bolt12 offers in offline mode
            var offer = $"lnofflineoffer1-p={paymentHash}-a={amountSat}";
            // Optionally simulate success for offers too, just as a consistent behavior.
            _ = SimulateConfirmationAsync(paymentHash, _cts.Token);
            return Task.FromResult(offer);
        }

        public Task<string> CreateInvoiceAsync(ulong amountSat, string description, CancellationToken ct = default)
        {
            Validate(amountSat, description);

            if (ShouldSimulateFailure())
            {
                _logger.LogWarning("Simulating invoice creation failure (offline mode).");
                throw new InvoiceException("Simulated invoice creation failure (offline).");
            }

            var paymentHash = GeneratePaymentHash();
            // Offline invoice format (not real BOLT11). Controllers will fallback to extract `p=` when offline.
            var encodedDesc = Base64UrlEncode(description);
            var invoice = $"lnoffline1-p={paymentHash}-a={amountSat}-d={encodedDesc}";

            _ = SimulateConfirmationAsync(paymentHash, _cts.Token);

            return Task.FromResult(invoice);
        }

        public ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _cts.Dispose();
            return ValueTask.CompletedTask;
        }

        private void Validate(ulong amountSat, string description)
        {
            if (amountSat == 0 || amountSat > _settings.MaxInvoiceAmountSat)
            {
                throw new InvalidInvoiceRequestException($"Invoice amount must be between 1 and {_settings.MaxInvoiceAmountSat} sats.");
            }
            if (string.IsNullOrWhiteSpace(description) ||
                description.Length > _settings.MaxInvoiceDescriptionLength ||
                !DescriptionAllowed.IsMatch(description))
            {
                throw new InvalidInvoiceRequestException("Invalid invoice description.");
            }
        }

        private static string GeneratePaymentHash()
        {
            // 32 bytes => 64 hex chars
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string Base64UrlEncode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var b64 = Convert.ToBase64String(bytes);
            return b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private bool ShouldSimulateFailure()
        {
            var r = _offlineOptions.SimulatedFailureRate;
            if (r <= 0) return false;
            return Random.Shared.NextDouble() < r;
        }

        private async Task SimulateConfirmationAsync(string paymentHash, CancellationToken token)
        {
            try
            {
                var delay = Math.Max(0, _offlineOptions.SimulatedConfirmationDelayMs);
                await Task.Delay(delay, token);

                using var scope = _serviceProvider.CreateScope();
                var paymentState = scope.ServiceProvider.GetRequiredService<IPaymentStateService>();
                await paymentState.ConfirmPaymentAsync(paymentHash);

                _logger.LogInformation("Offline mode: payment confirmed for hash {PaymentHash}", paymentHash);
            }
            catch (OperationCanceledException) { /* shutting down */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Offline mode: failed to simulate confirmation for {PaymentHash}", paymentHash);
            }
        }
    }
}