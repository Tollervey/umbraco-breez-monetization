using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Middleware;
using Tollervey.Umbraco.LightningPayments.UI.Models;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace Tollervey.Umbraco.LightningPayments.UI.Controllers
{
    [RequireHttps]
    [Authorize(Policy = AuthorizationPolicies.RequireAdminAccess)]
    [Route("api/lightningpayments")]
    [Produces("application/json")]
    public class LightningPaymentsApiController : ManagementApiControllerBase
    {
        private readonly IBreezSdkService _breezSdkService;
        private readonly IPaymentStateService _paymentStateService;
        private readonly ILogger<LightningPaymentsApiController> _logger;
        private readonly IUserService _userService;
        private readonly ILightningPaymentsRuntimeMode _runtimeMode;
        private readonly IBreezPaymentsFacade _paymentsFacade;
        private readonly IInvoiceHelper _invoiceHelper;
        private readonly IWebHostEnvironment _hostEnv;

        private IActionResult Error(int statusCode, string error, string message) => StatusCode(statusCode, new ApiError { error = error, message = message });

        public LightningPaymentsApiController(
        IBreezSdkService breezSdkService,
        IPaymentStateService paymentStateService,
        ILogger<LightningPaymentsApiController> logger,
        IUserService userService,
        ILightningPaymentsRuntimeMode runtimeMode,
        IBreezPaymentsFacade paymentsFacade,
        IInvoiceHelper invoiceHelper,
        IWebHostEnvironment hostEnv)
        {
            _breezSdkService = breezSdkService;
            _paymentStateService = paymentStateService;
            _logger = logger;
            _userService = userService;
            _runtimeMode = runtimeMode;
            _paymentsFacade = paymentsFacade;
            _invoiceHelper = invoiceHelper;
            _hostEnv = hostEnv;
        }

        // Helper to handle idempotency header lookup and short-circuit when a previous mapping exists.
        private async Task<(IActionResult? Result, string? IdempotencyKey)> TryGetIdempotencyMappingResultAsync()
        {
            var idempotencyKey = Request.Headers[LightningPaymentsSettings.IdempotencyKeyHeader].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(idempotencyKey)) return (null, null);

            var existing = await _paymentStateService.TryGetMappingByKeyAsync(idempotencyKey);
            if (existing != null)
            {
                return (Ok(new { invoice = existing.Invoice, paymentHash = existing.PaymentHash, status = existing.Status.ToString().ToLowerInvariant() }), idempotencyKey);
            }

            return (null, idempotencyKey);
        }

        /// <summary>
        /// Gets the connection status of the Breez SDK.
        /// </summary>
        [HttpGet("GetStatus")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatus()
        { 
            var connected = await _breezSdkService.IsConnectedAsync(); 
            return Ok(new { connected, offlineMode = _runtimeMode.IsOffline }); 
        }

        /// <summary>
        /// Gets the Lightning Network receive limits from the Breez SDK.
        /// </summary>
        [HttpGet("GetLightningReceiveLimits")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLightningReceiveLimits()
        { 
            var limits = await _paymentsFacade.FetchLightningLimitsAsync(); 
            if (limits == null) return Error(StatusCodes.Status400BadRequest, "invalid_state", "Breez SDK not connected."); 
            return Ok(new { minSat = limits.receive.minSat, maxSat = limits.receive.maxSat }); 
        }

        /// <summary>
        /// Gets the recommended fees from the Breez SDK.
        /// </summary>
        [HttpGet("GetRecommendedFees")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecommendedFees()
        { 
            var fees = await _breezSdkService.GetRecommendedFeesAsync(); 
            if (fees == null) return Ok(new { }); 
            return Ok(fees); 
        }

        /// <summary>
        /// Creates a test invoice with the specified amount and description.
        /// </summary>
        [HttpPost("CreateTestInvoice")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        [EnableRateLimiting("InvoiceGeneration")]
        public async Task<IActionResult> CreateTestInvoice([FromBody] TestInvoiceRequest request)
        {
            if (request == null || request.AmountSat <=0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid amount.");
            try
            {
                var (preResult, idempotencyKey) = await TryGetIdempotencyMappingResultAsync();
                if (preResult != null) return preResult;

                var (invoice, paymentHash) = await _invoiceHelper.CreateInvoiceAndHashAsync(request.AmountSat, string.IsNullOrWhiteSpace(request.Description) ? "Test invoice" : request.Description!, idempotencyKey);
                return Ok(new { invoice, paymentHash });
            }
            catch (InvalidInvoiceRequestException ex) { _logger.LogWarning(ex, "Invalid request for test invoice."); return Error(StatusCodes.Status400BadRequest, "invalid_request", ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error creating test invoice."); return Error(StatusCodes.Status500InternalServerError, "server_error", "An error occurred while creating the invoice."); }
        }

        /// <summary>
        /// Manually confirms a payment by its hash.
        /// </summary>
        [HttpPost("ConfirmPayment")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmPayment([FromBody] PaymentHashRequest request)
        { 
            if (request == null || string.IsNullOrWhiteSpace(request.PaymentHash)) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid payment hash.");
            try 
            { 
                var result = await _paymentStateService.ConfirmPaymentAsync(request.PaymentHash); 
                return result switch 
                { 
                    PaymentConfirmationResult.Confirmed => Ok(new { status = "confirmed" }),
                    PaymentConfirmationResult.AlreadyConfirmed => Ok(new { status = "already_confirmed" }),
                    _ => Error(StatusCodes.Status404NotFound, "not_found", "Payment not found or not confirmable.") 
                }; 
            } 
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Error confirming payment {PaymentHash}", request.PaymentHash); 
                return Error(StatusCodes.Status500InternalServerError, "server_error", "An error occurred while confirming the payment."); 
            } 
        }

        /// <summary>
        /// Marks a payment as failed by its hash.
        /// </summary>
        [HttpPost("MarkAsFailed")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAsFailed([FromBody] PaymentHashRequest request)
        { 
            if (request == null || string.IsNullOrWhiteSpace(request.PaymentHash)) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid payment hash.");
            try 
            { 
                var updated = await _paymentStateService.MarkAsFailedAsync(request.PaymentHash); 
                return updated ? Ok(new { status = "failed" }) : Error(StatusCodes.Status404NotFound, "not_found", "Payment not found."); 
            } 
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Error marking payment as failed {PaymentHash}", request.PaymentHash); 
                return Error(StatusCodes.Status500InternalServerError, "server_error", "An error occurred while updating the payment."); 
            } 
        }

        /// <summary>
        /// Marks a payment as expired by its hash.
        /// </summary>
        [HttpPost("MarkAsExpired")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAsExpired([FromBody] PaymentHashRequest request)
        { 
            if (request == null || string.IsNullOrWhiteSpace(request.PaymentHash)) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid payment hash.");
            try 
            { 
                var updated = await _paymentStateService.MarkAsExpiredAsync(request.PaymentHash); 
                return updated ? Ok(new { status = "expired" }) : Error(StatusCodes.Status404NotFound, "not_found", "Payment not found."); 
            } 
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Error marking payment as expired {PaymentHash}", request.PaymentHash); 
                return Error(StatusCodes.Status500InternalServerError, "server_error", "An error occurred while updating the payment."); 
            } 
        }

        /// <summary>
        /// Marks a payment as refund pending by its hash.
        /// </summary>
        [HttpPost("MarkAsRefundPending")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAsRefundPending([FromBody] PaymentHashRequest request)
        { 
            if (request == null || string.IsNullOrWhiteSpace(request.PaymentHash)) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid payment hash.");
            try 
            { 
                var updated = await _paymentStateService.MarkAsRefundPendingAsync(request.PaymentHash); 
                return updated ? Ok(new { status = "refund_pending" }) : Error(StatusCodes.Status404NotFound, "not_found", "Payment not found."); 
            } 
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Error marking payment as refund pending {PaymentHash}", request.PaymentHash); 
                return Error(StatusCodes.Status500InternalServerError, "server_error", "An error occurred while updating the payment."); 
            } 
        }

        /// <summary>
        /// Marks a payment as refunded by its hash.
        /// </summary>
        [HttpPost("MarkAsRefunded")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAsRefunded([FromBody] PaymentHashRequest request)
        { 
            if (request == null || string.IsNullOrWhiteSpace(request.PaymentHash)) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid payment hash.");
            try 
            { 
                var updated = await _paymentStateService.MarkAsRefundedAsync(request.PaymentHash); 
                return updated ? Ok(new { status = "refunded" }) : Error(StatusCodes.Status404NotFound, "not_found", "Payment not found."); 
            } 
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Error marking payment as refunded {PaymentHash}", request.PaymentHash); 
                return Error(StatusCodes.Status500InternalServerError, "server_error", "An error occurred while updating the payment."); 
            } 
        }

        /// <summary>
        /// Generates a Lightning Network invoice for paywall access to a specific content item.
        /// </summary>
        /// <param name="contentId">The ID of the Umbraco content item requiring payment.</param>
        /// <returns>A JSON object containing the invoice and payment hash, or an error response.</returns>
        [HttpGet("GetPaywallInvoice")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        [EnableRateLimiting("InvoiceGeneration")]
        public async Task<IActionResult> GetPaywallInvoice([FromQuery] int contentId)
        {
            if (contentId <=0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid content ID.");
            try
            {
                var (content, paywallConfig) = _invoiceHelper.GetContentAndPaywallConfig(contentId);
                if (content == null || paywallConfig == null) return Error(StatusCodes.Status404NotFound, "not_found", "Content or paywall configuration not found.");
                if (!paywallConfig.Enabled || paywallConfig.Fee ==0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Paywall is not enabled or fee is not set.");
                _logger.LogInformation("Invoice requested for ContentId {ContentId} and Fee {Fee}", contentId, paywallConfig.Fee);

                var (preResult, idempotencyKey) = await TryGetIdempotencyMappingResultAsync();
                if (preResult != null) return preResult;

                var (invoice, paymentHash) = await _invoiceHelper.CreateInvoiceAndHashAsync(paywallConfig.Fee, $"Access to content ID {contentId}", idempotencyKey);
                var sessionId = _invoiceHelper.EnsureSessionCookie(Request, Response);
                await _paymentStateService.AddPendingPaymentAsync(paymentHash, contentId, sessionId);
                return Ok(new { invoice, paymentHash });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error generating paywall invoice for contentId {ContentId}", contentId); return Error(StatusCodes.Status500InternalServerError, "server_error", "An error occurred while generating the invoice."); }
        }

        /// <summary>
        /// Gets the payment status for a user and content.
        /// </summary>
        [HttpGet("GetPaymentStatus")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPaymentStatus([FromQuery] int contentId)
        { 
            var sessionId = Request.Cookies[PaywallMiddleware.PaywallCookieName]; 
            if (string.IsNullOrEmpty(sessionId)) return Error(StatusCodes.Status401Unauthorized, "unauthorized", "Session cookie not found.");

            var state = await _paymentStateService.GetPaymentStateAsync(sessionId, contentId); 
            return Ok(new { status = state?.Status.ToString() }); 
        }

        /// <summary>
        /// Gets all payment transactions for admin view.
        /// </summary>
        [HttpGet("GetAllPayments")]
        [ProducesResponseType(typeof(IEnumerable<PaymentState>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllPayments()
        { 
            var userId = int.Parse(User.Identity.GetUserId()); 
            var currentUser = _userService.GetUserById(userId); 
            if (currentUser == null || !currentUser.Groups.Any(g => g.Name == "Administrators")) return Error(StatusCodes.Status401Unauthorized, "unauthorized", "Admin access required.");
            var payments = await _paymentStateService.GetAllPaymentsAsync(); 
            return Ok(payments); 
        }

        /// <summary>
        /// Provides LNURL-Pay metadata for a specific content item.
        /// </summary>
        /// <param name="contentId">The ID of the Umbraco content item.</param>
        /// <returns>JSON object with LNURL-Pay details.</returns>
        [HttpGet("GetLnurlPayInfo")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult GetLnurlPayInfo([FromQuery] int contentId)
        { 
            // Use the public LNURL callback to avoid duplicating endpoints
            return _invoiceHelper.BuildLnurlPayInfo(contentId, Request, "/api/public/lightning/GetLnurlInvoice", _logger); 
        }

        /// <summary>
        /// Callback to generate a BOLT11 invoice for LNURL-Pay.
        /// </summary>
        /// <param name="amount">Amount in millisatoshis.</param>
        /// <param name="contentId">The ID of the Umbraco content item.</param>
        /// <returns>JSON object with the payment request (pr).</returns>
        [HttpGet("GetLnurlInvoice")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        [EnableRateLimiting("InvoiceGeneration")]
        public async Task<IActionResult> GetLnurlInvoice([FromQuery] long amount, [FromQuery] int contentId)
        {
            if (contentId <=0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid content ID.");
            if (amount <0 || amount %1000 !=0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid amount. Must be positive and divisible by1000.");
            var sats = (ulong)(amount /1000);
            try
            {
                var (content, paywallConfig) = _invoiceHelper.GetContentAndPaywallConfig(contentId);
                if (content == null || paywallConfig == null) return Error(StatusCodes.Status404NotFound, "not_found", "Content or paywall configuration not found.");
                if (!paywallConfig.Enabled || paywallConfig.Fee ==0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Paywall is not enabled or fee is not set.");
                if (sats != paywallConfig.Fee) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Amount does not match the required fee.");

                var (preResult, idempotencyKey) = await TryGetIdempotencyMappingResultAsync();
                if (preResult != null) return preResult;

                var (invoice, paymentHash) = await _invoiceHelper.CreateInvoiceAndHashAsync(sats, $"Access to {content.Name} (ID: {contentId})", idempotencyKey);
                var sessionId = _invoiceHelper.EnsureSessionCookie(Request, Response);
                await _paymentStateService.AddPendingPaymentAsync(paymentHash, contentId, sessionId);
                return Ok(new { pr = invoice });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error generating LNURL invoice for contentId {ContentId} and amount {Amount}", contentId, amount); return Error(StatusCodes.Status500InternalServerError, "server_error", "An error occurred while generating the invoice."); }
        }

        /// <summary>
        /// Gets the Bolt12 offer for a specific content item.
        /// </summary>
        [HttpGet("GetBolt12Offer")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        [EnableRateLimiting("InvoiceGeneration")]
        public async Task<IActionResult> GetBolt12Offer([FromQuery] int contentId)
        {
            if (contentId <=0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid content ID.");
            try
            {
                var (content, paywallConfig) = _invoiceHelper.GetContentAndPaywallConfig(contentId);
                if (content == null || paywallConfig == null) return Error(StatusCodes.Status404NotFound, "not_found", "Content or paywall configuration not found.");
                if (!paywallConfig.Enabled || paywallConfig.Fee ==0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Paywall is not enabled or fee is not set.");
                _logger.LogInformation("Bolt12 offer requested for ContentId {ContentId} and Fee {Fee}", contentId, paywallConfig.Fee);
                var offer = await _breezSdkService.CreateBolt12OfferAsync(paywallConfig.Fee, $"Access to content ID {contentId}");
                return Ok(offer);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error generating Bolt12 offer for contentId {ContentId}", contentId); return Error(StatusCodes.Status500InternalServerError, "server_error", "An error occurred while generating the offer."); }
        }

        /// <summary>
        /// Returns a fee quote (in sats) for receiving the paywall amount for a content item.
        /// </summary>
        [HttpGet("GetPaywallReceiveFeeQuote")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaywallReceiveFeeQuote([FromQuery] int contentId, [FromQuery] bool bolt12 = false)
        {
            if (contentId <=0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid content ID.");
            try
            {
                var (_, paywallConfig) = _invoiceHelper.GetContentAndPaywallConfig(contentId);
                if (paywallConfig == null || !paywallConfig.Enabled || paywallConfig.Fee ==0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Paywall is not enabled or fee is not set.");
                var feesSat = await _breezSdkService.GetReceiveFeeQuoteAsync(paywallConfig.Fee, bolt12);
                return Ok(new { amountSat = paywallConfig.Fee, feesSat, method = bolt12 ? "bolt12" : "bolt11" });
            }
            catch (InvalidInvoiceRequestException ex) { _logger.LogWarning(ex, "Invalid request for fee quote."); return Error(StatusCodes.Status400BadRequest, "invalid_request", ex.Message); }
            catch (Exception ex) { _logger.LogError(ex, "Error fetching receive fee quote for contentId {ContentId}", contentId); return Error(StatusCodes.Status500InternalServerError, "server_error", "An error occurred while fetching the fee quote."); }
        }

        /// <summary>
        /// Diagnostic endpoint for admin to check plugin assets.
        /// </summary>
        [HttpGet("Diagnostics/Assets")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [Authorize(Policy = AuthorizationPolicies.RequireAdminAccess)]
        public IActionResult DiagnosticsAssets()
        {
            const string pluginDir = "App_Plugins/Tollervey.Umbraco.LightningPayments";
            var fp = _hostEnv.WebRootFileProvider;

            var probe = new []
            {
                $"{pluginDir}/umbraco-package.json",
                $"{pluginDir}/lightning-ui.js"
            }
            .Select(p =>
            {
                var fi = fp.GetFileInfo(p);
                return new
                {
                    path = "/" + p.Replace('\\','/'),
                    exists = fi.Exists,
                    length = fi.Exists ? (long?)fi.Length : null
                };
            })
            .ToArray();

            var listing = fp.GetDirectoryContents(pluginDir);
            var files = listing.Exists
                ? listing.Where(f => !f.IsDirectory)
                         .Select(f => new { f.Name, f.Length })
                         .OrderBy(f => f.Name)
                         .ToArray()
                : Array.Empty<object>();

            // Try read first 256 bytes of manifest to confirm content
            string? manifestHead = null;
            try
            {
                var mf = fp.GetFileInfo($"{pluginDir}/umbraco-package.json");
                if (mf.Exists)
                {
                    using var s = mf.CreateReadStream();
                    using var r = new StreamReader(s, leaveOpen:false);
                    var buf = new char[Math.Min(256, (int)mf.Length)];
                    var n = r.ReadBlock(buf, 0, buf.Length);
                    manifestHead = new string(buf, 0, n);
                }
            }
            catch { /* ignore */ }

            return Ok(new
            {
                webRootPath = _hostEnv.WebRootPath,
                probes = probe,
                directory = "/" + pluginDir,
                files,
                manifestPreview = manifestHead
            });
        }
    }

    public sealed class TestInvoiceRequest { public ulong AmountSat { get; set; } public string? Description { get; set; } }
    public sealed class PaymentHashRequest { public string PaymentHash { get; set; } = string.Empty; }
}