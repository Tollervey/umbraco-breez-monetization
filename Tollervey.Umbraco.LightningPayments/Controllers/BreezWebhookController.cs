using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Tollervey.LightningPayments.Core.Configuration;
using Tollervey.LightningPayments.Core.Models;
using Tollervey.LightningPayments.Core.Services;
using Umbraco.Cms.Web.Common.Controllers;

namespace Tollervey.Umbraco.LightningPayments.Controllers
{
    [ApiController]
    [Route("umbraco/api/[controller]")]
    [RequireHttps]
    public class BreezWebhookController : UmbracoApiControllerBase
    {
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
        public async Task<IActionResult> HandleWebhook([FromBody] BreezWebhookPayload payload)
        {
            Request.EnableBuffering();

            // Verify webhook signature
            if (!VerifyWebhookSignature(Request.Body, Request.Headers["X-Breez-Signature"].ToString()))
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

            body.Position = 0;
            using var reader = new StreamReader(body);
            var bodyContent = reader.ReadToEnd();
            body.Position = 0; // Reset for further reading if needed

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(bodyContent));

            byte[] signatureBytes;
            try
            {
                signatureBytes = Convert.FromHexString(signature);
            }
            catch (FormatException)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(hash, signatureBytes);
        }
    }
}