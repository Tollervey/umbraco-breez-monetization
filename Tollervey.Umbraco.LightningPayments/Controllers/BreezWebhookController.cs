using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Models;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Umbraco.Cms.Web.Common.Controllers;

namespace Tollervey.Umbraco.LightningPayments.UI.Controllers
{
    /// <summary>
    /// Receives webhook callbacks from Breez to update local payment state.
    /// </summary>
    [ApiController]
    [Route("umbraco/api/[controller]")]
    [RequireHttps]
    public class BreezWebhookController : UmbracoApiControllerBase
    {
        private const long MaxWebhookBodyBytes = 64 * 1024; // 64 KB

        private readonly IPaymentStateService _paymentStateService;
        private readonly ILogger<BreezWebhookController> _logger;
        private readonly LightningPaymentsSettings _settings;
        private readonly IEmailService _emailService;

        public BreezWebhookController(
            IPaymentStateService paymentStateService,
            ILogger<BreezWebhookController> logger,
            IOptions<LightningPaymentsSettings> settings,
            IEmailService emailService)
        {
            _paymentStateService = paymentStateService;
            _logger = logger;
            _settings = settings.Value;
            _emailService = emailService;
        }

        /// <summary>
        /// Handles incoming webhooks from the Breez payment service to confirm payments.
        /// </summary>
        /// <param name="payload">The webhook payload containing payment details.</param>
        /// <returns>An OK response if payment is confirmed, or an error response.</returns>
        [HttpPost]
        [RequestSizeLimit(MaxWebhookBodyBytes)]
        public async Task<IActionResult> HandleWebhook([FromBody] BreezWebhookPayload payload)
        {
            // Quick reject oversized bodies even before buffering
            if (Request.ContentLength.HasValue && Request.ContentLength.Value > MaxWebhookBodyBytes)
            {
                _logger.LogWarning("Breez webhook rejected: payload too large ({ContentLength} bytes)", Request.ContentLength.Value);
                return StatusCode(StatusCodes.Status413PayloadTooLarge, "Payload too large.");
            }

            Request.EnableBuffering();

            // Verify webhook signature
            var signatureHeader = Request.Headers["X-Breez-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
            {
                _logger.LogWarning("Breez webhook received but webhook secret is not configured. Rejecting request.");
                return Unauthorized("Webhook secret not configured.");
            }

            if (!VerifyWebhookSignature(Request.Body, signatureHeader))
            {
                _logger.LogWarning("Invalid webhook signature.");
                return Unauthorized("Invalid signature.");
            }

            if (payload == null || string.IsNullOrWhiteSpace(payload.Payment?.Id) || string.IsNullOrWhiteSpace(payload.Type))
            {
                _logger.LogWarning("Received invalid webhook payload: missing or empty fields.");
                return BadRequest("Invalid payload.");
            }

            var paymentHash = payload.Payment.Id;

            try
            {
                bool updated;
                string status;

                switch (payload.Type)
                {
                    case "payment_succeeded":
                        var result = await _paymentStateService.ConfirmPaymentAsync(paymentHash);
                        switch (result)
                        {
                            case PaymentConfirmationResult.Confirmed:
                                status = "confirmed";
                                updated = true;
                                break;
                            case PaymentConfirmationResult.AlreadyConfirmed:
                                _logger.LogInformation("Payment already confirmed for hash: {PaymentHash}", paymentHash);
                                return Ok(new { status = "already_confirmed" });
                            case PaymentConfirmationResult.NotFound:
                                status = "not_found";
                                updated = false;
                                break;
                            default:
                                throw new InvalidOperationException("Unexpected confirmation result.");
                        }
                        break;
                    case "payment_failed":
                        updated = await _paymentStateService.MarkAsFailedAsync(paymentHash);
                        status = "failed";
                        break;
                    case "invoice_expired":
                        updated = await _paymentStateService.MarkAsExpiredAsync(paymentHash);
                        status = "expired";
                        break;
                    case "refund_initiated":
                        updated = await _paymentStateService.MarkAsRefundPendingAsync(paymentHash);
                        status = "refund_pending";
                        break;
                    case "refund_succeeded":
                        updated = await _paymentStateService.MarkAsRefundedAsync(paymentHash);
                        status = "refunded";
                        break;
                    default:
                        _logger.LogWarning("Unknown webhook type: {Type} for hash: {PaymentHash}", payload.Type, paymentHash);
                        return BadRequest("Unknown webhook type.");
                }

                if (updated)
                {
                    _logger.LogInformation("Payment {Status} for hash: {PaymentHash}", status, paymentHash);
                    // Notify admin
                    if (!string.IsNullOrEmpty(_settings.AdminEmail))
                    {
                        await _emailService.SendEmailAsync(_settings.AdminEmail, $"Payment {status.ToUpperInvariant()}", $"A payment has been {status} for content ID associated with hash {paymentHash}.");
                    }
                    return Ok(new { status });
                }
                else
                {
                    _logger.LogWarning("Payment update failed for hash: {PaymentHash}. Payment not found.", paymentHash);
                    return NotFound(new { status = "not_found" });
                }
            }
            catch (PaymentException ex)
            {
                _logger.LogError(ex, "Payment processing error for hash: {PaymentHash}: {Message}", paymentHash, ex.Message);
                return BadRequest(new { status = "error", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook for hash: {PaymentHash}.", paymentHash);
                return StatusCode(500, "An unexpected error occurred while processing the webhook.");
            }
        }

        private bool VerifyWebhookSignature(Stream body, string signature)
        {
            if (string.IsNullOrEmpty(_settings.WebhookSecret) || string.IsNullOrEmpty(signature))
            {
                return false;
            }

            // Normalize signature: trim whitespace and remove optional 0x prefix
            var sig = signature.Trim();
            if (sig.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                sig = sig.Substring(2);
            }

            body.Position = 0;
            using var reader = new StreamReader(body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var bodyContent = reader.ReadToEnd();
            body.Position = 0; // Reset for further reading if needed

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(bodyContent));

            byte[] signatureBytes = Array.Empty<byte>();

            // Try parse as hex first
            try
            {
                // Reject odd length hex early
                if (sig.Length % 2 == 0 && sig.Length <= 128) // limit to reasonable length
                {
                    signatureBytes = Convert.FromHexString(sig);
                }
            }
            catch (FormatException)
            {
                // fall through to try base64
                signatureBytes = Array.Empty<byte>();
            }

            if (signatureBytes.Length == 0)
            {
                // Try base64
                try
                {
                    signatureBytes = Convert.FromBase64String(sig);
                }
                catch (FormatException)
                {
                    return false;
                }
            }

            // signature must be same length as hash
            if (signatureBytes.Length != hash.Length)
            {
                _logger.LogWarning("Webhook signature length mismatch: expected {Expected} bytes but got {Actual} bytes.", hash.Length, signatureBytes.Length);
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(hash, signatureBytes);
        }
    }
}