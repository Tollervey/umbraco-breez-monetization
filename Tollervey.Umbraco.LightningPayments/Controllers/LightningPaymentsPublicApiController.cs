using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Middleware;
using Tollervey.Umbraco.LightningPayments.UI.Models;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using Tollervey.Umbraco.LightningPayments.UI.Services.RateLimiting;

namespace Tollervey.Umbraco.LightningPayments.UI.Controllers
{
 /// <summary>
 /// Public, anonymous endpoints for front-end UI to interact with Breez-backed paywalls and tips.
 /// </summary>
 [ApiController]
 [RequireHttps]
 [AllowAnonymous]
 [Route("api/public/lightning")] // Keep outside /umbraco so the website can call anonymously
 [Produces("application/json")]
 public class LightningPaymentsPublicApiController : ControllerBase
 {
 private readonly IBreezSdkService _breezSdkService;
 private readonly IPaymentStateService _paymentStateService;
 private readonly IUmbracoContextFactory _umbracoContextFactory;
 private readonly ILogger<LightningPaymentsPublicApiController> _logger;
 private readonly ILightningPaymentsRuntimeMode _runtimeMode;
 private readonly IRateLimiter _rateLimiter;
 private readonly IInvoiceHelper _invoiceHelper;

 private static readonly Regex OfflineHashRegex = new(@"(?:^|-)p=([0-9a-f]{64})(?:-|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

 public LightningPaymentsPublicApiController(
 IBreezSdkService breezSdkService,
 IPaymentStateService paymentStateService,
 IUmbracoContextFactory umbracoContextFactory,
 ILogger<LightningPaymentsPublicApiController> logger,
 ILightningPaymentsRuntimeMode runtimeMode,
 IRateLimiter rateLimiter,
 IInvoiceHelper invoiceHelper)
 {
 _breezSdkService = breezSdkService;
 _paymentStateService = paymentStateService;
 _umbracoContextFactory = umbracoContextFactory;
 _logger = logger;
 _runtimeMode = runtimeMode;
 _rateLimiter = rateLimiter;
 _invoiceHelper = invoiceHelper;
 }

 private IActionResult Error(int statusCode, string error, string message) => StatusCode(statusCode, new ApiError { error = error, message = message });

 private (bool Allowed, TimeSpan RetryAfter) CheckRate(string endpointKey)
 { var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"; var session = Request.Cookies[PaywallMiddleware.PaywallCookieName] ?? "anon"; var key = $"{endpointKey}:{ip}:{session}"; var allowed = _rateLimiter.TryConsume(key,5, TimeSpan.FromSeconds(30), out var retry); return (allowed, retry); }

 /// <summary>
 /// Generate a Bolt11 invoice for the content paywall (anonymous).
 /// </summary>
 [HttpGet("GetPaywallInvoice")]
 [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
 [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
 [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
 [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
 public async Task<IActionResult> GetPaywallInvoice([FromQuery] int contentId)
 {
 var rate = CheckRate("paywall");
 if (!rate.Allowed) { Response.Headers["Retry-After"] = Math.Ceiling(rate.RetryAfter.TotalSeconds).ToString(); return Error(StatusCodes.Status429TooManyRequests, "rate_limited", "Too many requests. Please try again shortly."); }
 if (contentId <=0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Invalid content ID.");
 try
 {
 var (content, paywallConfig) = _invoiceHelper.GetContentAndPaywallConfig(contentId);
 if (content == null || paywallConfig == null) return Error(StatusCodes.Status404NotFound, "not_found", "Content or paywall configuration not found.");
 if (!paywallConfig.Enabled || paywallConfig.Fee ==0) return Error(StatusCodes.Status400BadRequest, "invalid_request", "Paywall is not enabled or fee is not set.");
 _logger.LogInformation("[Public] Invoice requested for ContentId {ContentId} and Fee {Fee}", contentId, paywallConfig.Fee);
 var (invoice, paymentHash) = await _invoiceHelper.CreateInvoiceAndHashAsync(paywallConfig.Fee, $"Access to content ID {contentId}");
 var sessionId = _invoiceHelper.EnsureSessionCookie(Request, Response);
 await _paymentStateService.AddPendingPaymentAsync(paymentHash, contentId, sessionId);
 var expiry = await _invoiceHelper.TryGetInvoiceExpiryAsync(invoice);
 return Ok(new { invoice, paymentHash, expiry });
 }
 catch (InvalidInvoiceRequestException ex) { _logger.LogWarning(ex, "Invalid invoice request for content {ContentId}", contentId); return Error(StatusCodes.Status400BadRequest, "invalid_request", ex.Message); }
 catch (Exception ex) { _logger.LogError(ex, "Error generating paywall invoice for contentId {ContentId}", contentId); return Error(StatusCodes.Status500InternalServerError, "server_error", "An error occurred while generating the invoice."); }
 }

 /// <summary>
 /// Gets the payment status for the current session and content (anonymous).
 /// </summary>
 [HttpGet("GetPaymentStatus")]
 [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
 [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
 public async Task<IActionResult> GetPaymentStatus([FromQuery] int contentId)
 { var sessionId = Request.Cookies[PaywallMiddleware.PaywallCookieName]; if (string.IsNullOrEmpty(sessionId)) return Error(StatusCodes.Status401Unauthorized, "unauthorized", "Session cookie not found."); var state = await _paymentStateService.GetPaymentStateAsync(sessionId, contentId); return Ok(new { status = state?.Status.ToString() }); }

 /// <summary>
 /// LNURL-Pay metadata for a specific content item (anonymous).
 /// </summary>
 [HttpGet("GetLnurlPayInfo")]
 public IActionResult GetLnurlPayInfo([FromQuery] int contentId)
 { return _invoiceHelper.BuildLnurlPayInfo(contentId, Request, "/api/public/lightning/GetLnurlInvoice", _logger); }

 /// <summary>
 /// LNURL-Pay callback to create a Bolt11 invoice (anonymous). Amount is in millisats.
 /// </summary>
 [HttpGet("GetLnurlInvoice")]
 public async Task<IActionResult> GetLnurlInvoice([FromQuery] long amount, [FromQuery] int contentId, [FromQuery] string? state = null)
 {
 var rate = CheckRate("lnurl");
 if (!rate.Allowed) { Response.Headers["Retry-After"] = Math.Ceiling(rate.RetryAfter.TotalSeconds).ToString(); return StatusCode(StatusCodes.Status429TooManyRequests, "Too many requests. Please try again shortly."); }
 if (contentId <=0) return BadRequest("Invalid content ID.");
 if (amount <0 || amount %1000 !=0) return BadRequest("Invalid amount. Must be positive and divisible by1000.");
 var sats = (ulong)(amount /1000);
 try
 {
 var (content, paywallConfig) = _invoiceHelper.GetContentAndPaywallConfig(contentId);
 if (content == null || paywallConfig == null) return NotFound("Content or paywall configuration not found.");
 if (!paywallConfig.Enabled || paywallConfig.Fee ==0) return BadRequest("Paywall is not enabled or fee is not set.");
 if (sats != paywallConfig.Fee) return BadRequest("Amount does not match the required fee.");
 var (invoice, paymentHash) = await _invoiceHelper.CreateInvoiceAndHashAsync(sats, $"Access to {content.Name} (ID: {contentId})");
 var sessionId = _invoiceHelper.EnsureSessionCookie(Request, Response, state);
 await _paymentStateService.AddPendingPaymentAsync(paymentHash, contentId, sessionId);
 return Ok(new { pr = invoice });
 }
 catch (Exception ex) { _logger.LogError(ex, "Error generating LNURL invoice for contentId {ContentId} and amount {Amount}", contentId, amount); return StatusCode(500, "An error occurred while generating the invoice."); }
 }

 /// <summary>
 /// Returns a Bolt12 offer for the content (anonymous). Optional; UI can choose Bolt12 instead of Bolt11.
 /// </summary>
 [HttpGet("GetBolt12Offer")]
 public async Task<IActionResult> GetBolt12Offer([FromQuery] int contentId)
 {
 if (contentId <=0) return BadRequest("Invalid content ID.");
 try
 {
 var (content, paywallConfig) = _invoiceHelper.GetContentAndPaywallConfig(contentId);
 if (content == null || paywallConfig == null) return NotFound("Content or paywall configuration not found.");
 if (!paywallConfig.Enabled || paywallConfig.Fee ==0) return BadRequest("Paywall is not enabled or fee is not set.");
 _logger.LogInformation("[Public] Bolt12 offer requested for ContentId {ContentId} and Fee {Fee}", contentId, paywallConfig.Fee);
 var offer = await _breezSdkService.CreateBolt12OfferAsync(paywallConfig.Fee, $"Access to content ID {contentId}");
 return Ok(offer);
 }
 catch (Exception ex) { _logger.LogError(ex, "Error generating Bolt12 offer for contentId {ContentId}", contentId); return StatusCode(500, "An error occurred while generating the offer."); }
 }

 /// <summary>
 /// Create a tip-jar invoice (anonymous). Records a pending TIP payment with amount for stats.
 /// </summary>
 [HttpPost("CreateTipInvoice")]
 public async Task<IActionResult> CreateTipInvoice([FromBody] TipInvoiceRequest request)
 {
 var rate = CheckRate("tip");
 if (!rate.Allowed) { Response.Headers["Retry-After"] = Math.Ceiling(rate.RetryAfter.TotalSeconds).ToString(); return StatusCode(StatusCodes.Status429TooManyRequests, "Too many requests. Please try again shortly."); }
 if (request == null || request.AmountSat <=0) return BadRequest("Invalid amount.");
 try
 {
 var description = string.IsNullOrWhiteSpace(request.Label) ? (request.ContentId.HasValue ? $"Tip for content {request.ContentId.Value}" : "Tip jar contribution") : request.Label!;
 var (invoice, paymentHash) = await _invoiceHelper.CreateInvoiceAndHashAsync(request.AmountSat, description);
 var sessionId = _invoiceHelper.EnsureSessionCookie(Request, Response);
 await _paymentStateService.AddPendingPaymentAsync(paymentHash, request.ContentId ??0, sessionId);
 await _paymentStateService.SetPaymentMetadataAsync(paymentHash, request.AmountSat, PaymentKind.Tip);
 var expiry = await _invoiceHelper.TryGetInvoiceExpiryAsync(invoice);
 return Ok(new { invoice, paymentHash, expiry });
 }
 catch (Exception ex) { _logger.LogError(ex, "Error creating tip invoice for amount {Amount}", request.AmountSat); return StatusCode(500, "An error occurred while creating the tip invoice."); }
 }

 /// <summary>
 /// Read-only tip stats.
 /// </summary>
 [HttpGet("GetTipStats")]
 public async Task<IActionResult> GetTipStats([FromQuery] int? contentId = null)
 {
 try
 {
 var all = await _paymentStateService.GetAllPaymentsAsync();
 var paid = all.Where(p => p.Status == PaymentStatus.Paid && p.Kind == PaymentKind.Tip);
 if (contentId.HasValue && contentId.Value >0) { paid = paid.Where(p => p.ContentId == contentId.Value); }
 var count = paid.Count();
 var total = paid.Aggregate<PaymentState, ulong>(0, (acc, p) => acc + p.AmountSat);
 return Ok(new { count, totalSats = total });
 }
 catch (Exception ex) { _logger.LogError(ex, "Error getting tip stats for ContentId {ContentId}", contentId); return StatusCode(500, "An error occurred while fetching tip stats."); }
 }

 /// <summary>
 /// Gets the payment status by payment hash (anonymous).
 /// </summary>
 [HttpGet("GetPaymentStatusByHash")]
 [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
 public async Task<IActionResult> GetPaymentStatusByHash([FromQuery] string paymentHash)
 {
 if (string.IsNullOrWhiteSpace(paymentHash)) return BadRequest(new { error = "invalid_request", message = "Missing paymentHash." });
 var state = await _paymentStateService.GetByPaymentHashAsync(paymentHash);
 if (state == null) return Ok(new { status = "not_found" });
 return Ok(new { status = state.Status.ToString() });
 }
 }

 public class TipInvoiceRequest { public ulong AmountSat { get; set; } public int? ContentId { get; set; } public string? Label { get; set; } }
}
