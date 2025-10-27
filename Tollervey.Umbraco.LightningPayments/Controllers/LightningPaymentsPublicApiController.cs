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

namespace Tollervey.Umbraco.LightningPayments.UI.Controllers
{
 /// <summary>
 /// Public, anonymous endpoints for front-end UI to interact with Breez-backed paywalls and tips.
 /// </summary>
 [ApiController]
 [RequireHttps]
 [AllowAnonymous]
 [Route("api/public/lightning")] // Keep outside /umbraco so the website can call anonymously
 public class LightningPaymentsPublicApiController : ControllerBase
 {
 private readonly IBreezSdkService _breezSdkService;
 private readonly IPaymentStateService _paymentStateService;
 private readonly IUmbracoContextFactory _umbracoContextFactory;
 private readonly ILogger<LightningPaymentsPublicApiController> _logger;
 private readonly ILightningPaymentsRuntimeMode _runtimeMode;

 private static readonly Regex OfflineHashRegex = new(@"(?:^|-)p=([0-9a-f]{64})(?:-|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

 public LightningPaymentsPublicApiController(
 IBreezSdkService breezSdkService,
 IPaymentStateService paymentStateService,
 IUmbracoContextFactory umbracoContextFactory,
 ILogger<LightningPaymentsPublicApiController> logger,
 ILightningPaymentsRuntimeMode runtimeMode)
 {
 _breezSdkService = breezSdkService;
 _paymentStateService = paymentStateService;
 _umbracoContextFactory = umbracoContextFactory;
 _logger = logger;
 _runtimeMode = runtimeMode;
 }

 /// <summary>
 /// Generate a Bolt11 invoice for the content paywall (anonymous).
 /// </summary>
 [HttpGet("GetPaywallInvoice")]
 public async Task<IActionResult> GetPaywallInvoice([FromQuery] int contentId)
 {
 if (contentId <=0)
 {
 return BadRequest("Invalid content ID.");
 }

 try
 {
 var (content, paywallConfig) = GetContentAndPaywallConfig(contentId);
 if (content == null || paywallConfig == null)
 {
 return NotFound("Content or paywall configuration not found.");
 }
 if (!paywallConfig.Enabled || paywallConfig.Fee ==0)
 {
 return BadRequest("Paywall is not enabled or fee is not set.");
 }

 _logger.LogInformation("[Public] Invoice requested for ContentId {ContentId} and Fee {Fee}", contentId, paywallConfig.Fee);

 var invoice = await _breezSdkService.CreateInvoiceAsync(paywallConfig.Fee, $"Access to content ID {contentId}");
 var paymentHash = await TryGetPaymentHash(invoice);
 if (string.IsNullOrWhiteSpace(paymentHash))
 {
 return BadRequest("Failed to obtain invoice payment hash.");
 }

 var sessionId = Request.Cookies[PaywallMiddleware.PaywallCookieName] ?? Guid.NewGuid().ToString();
 Response.Cookies.Append(PaywallMiddleware.PaywallCookieName, sessionId, new CookieOptions
 {
 HttpOnly = true,
 Secure = true,
 SameSite = SameSiteMode.Strict
 });

 await _paymentStateService.AddPendingPaymentAsync(paymentHash!, contentId, sessionId);

 return Ok(new { invoice, paymentHash });
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error generating paywall invoice for contentId {ContentId}", contentId);
 return StatusCode(500, "An error occurred while generating the invoice.");
 }
 }

 /// <summary>
 /// Gets the payment status for the current session and content (anonymous).
 /// </summary>
 [HttpGet("GetPaymentStatus")]
 public async Task<IActionResult> GetPaymentStatus([FromQuery] int contentId)
 {
 var sessionId = Request.Cookies[PaywallMiddleware.PaywallCookieName];
 if (string.IsNullOrEmpty(sessionId)) return Unauthorized();

 var state = await _paymentStateService.GetPaymentStateAsync(sessionId, contentId);
 return Ok(new { status = state?.Status.ToString() });
 }

 /// <summary>
 /// LNURL-Pay metadata for a specific content item (anonymous).
 /// </summary>
 [HttpGet("GetLnurlPayInfo")]
 public IActionResult GetLnurlPayInfo([FromQuery] int contentId)
 {
 // Callback points back to this controller's public callback endpoint
 return LnurlPayHelper.GetLnurlPayInfo(contentId, _umbracoContextFactory, _logger, Request, "/api/public/lightning/GetLnurlInvoice");
 }

 /// <summary>
 /// LNURL-Pay callback to create a Bolt11 invoice (anonymous). Amount is in millisats.
 /// </summary>
 [HttpGet("GetLnurlInvoice")]
 public async Task<IActionResult> GetLnurlInvoice([FromQuery] long amount, [FromQuery] int contentId)
 {
 if (contentId <=0)
 {
 return BadRequest("Invalid content ID.");
 }
 if (amount <0 || amount %1000 !=0)
 {
 return BadRequest("Invalid amount. Must be positive and divisible by1000.");
 }

 ulong sats = (ulong)(amount /1000);

 try
 {
 var (content, paywallConfig) = GetContentAndPaywallConfig(contentId);
 if (content == null || paywallConfig == null)
 {
 return NotFound("Content or paywall configuration not found.");
 }
 if (!paywallConfig.Enabled || paywallConfig.Fee ==0)
 {
 return BadRequest("Paywall is not enabled or fee is not set.");
 }
 if (sats != paywallConfig.Fee)
 {
 return BadRequest("Amount does not match the required fee.");
 }

 string description = $"Access to {content.Name} (ID: {contentId})";
 var invoice = await _breezSdkService.CreateInvoiceAsync(sats, description);
 var paymentHash = await TryGetPaymentHash(invoice);
 if (string.IsNullOrWhiteSpace(paymentHash))
 {
 return BadRequest("Failed to obtain invoice payment hash.");
 }

 var sessionId = Request.Cookies[PaywallMiddleware.PaywallCookieName] ?? Guid.NewGuid().ToString();
 Response.Cookies.Append(PaywallMiddleware.PaywallCookieName, sessionId, new CookieOptions
 {
 HttpOnly = true,
 Secure = true,
 SameSite = SameSiteMode.Strict
 });

 await _paymentStateService.AddPendingPaymentAsync(paymentHash!, contentId, sessionId);

 return Ok(new { pr = invoice });
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error generating LNURL invoice for contentId {ContentId} and amount {Amount}", contentId, amount);
 return StatusCode(500, "An error occurred while generating the invoice.");
 }
 }

 /// <summary>
 /// Returns a Bolt12 offer for the content (anonymous). Optional; UI can choose Bolt12 instead of Bolt11.
 /// </summary>
 [HttpGet("GetBolt12Offer")]
 public async Task<IActionResult> GetBolt12Offer([FromQuery] int contentId)
 {
 if (contentId <=0)
 {
 return BadRequest("Invalid content ID.");
 }

 try
 {
 var (content, paywallConfig) = GetContentAndPaywallConfig(contentId);
 if (content == null || paywallConfig == null)
 {
 return NotFound("Content or paywall configuration not found.");
 }
 if (!paywallConfig.Enabled || paywallConfig.Fee ==0)
 {
 return BadRequest("Paywall is not enabled or fee is not set.");
 }

 _logger.LogInformation("[Public] Bolt12 offer requested for ContentId {ContentId} and Fee {Fee}", contentId, paywallConfig.Fee);
 var offer = await _breezSdkService.CreateBolt12OfferAsync(paywallConfig.Fee, $"Access to content ID {contentId}");
 return Ok(offer);
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error generating Bolt12 offer for contentId {ContentId}", contentId);
 return StatusCode(500, "An error occurred while generating the offer.");
 }
 }

 /// <summary>
 /// Create a tip-jar invoice (anonymous). Not tied to content or session state by default.
 /// </summary>
 [HttpPost("CreateTipInvoice")]
 public async Task<IActionResult> CreateTipInvoice([FromBody] TipInvoiceRequest request)
 {
 if (request == null || request.AmountSat <=0)
 {
 return BadRequest("Invalid amount.");
 }

 try
 {
 var description = string.IsNullOrWhiteSpace(request.Label)
 ? (request.ContentId.HasValue ? $"Tip for content {request.ContentId.Value}" : "Tip jar contribution")
 : request.Label!;

 var invoice = await _breezSdkService.CreateInvoiceAsync(request.AmountSat, description);
 var paymentHash = await TryGetPaymentHash(invoice);
 if (string.IsNullOrWhiteSpace(paymentHash))
 {
 return BadRequest("Failed to obtain invoice payment hash.");
 }

 return Ok(new { invoice, paymentHash });
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error creating tip invoice for amount {Amount}", request.AmountSat);
 return StatusCode(500, "An error occurred while creating the tip invoice.");
 }
 }

 /// <summary>
 /// Optional: Return a receive fee quote for the content's configured amount.
 /// </summary>
 [HttpGet("GetPaywallReceiveFeeQuote")]
 public async Task<IActionResult> GetPaywallReceiveFeeQuote([FromQuery] int contentId, [FromQuery] bool bolt12 = false)
 {
 if (contentId <=0)
 {
 return BadRequest("Invalid content ID.");
 }

 try
 {
 var (_, paywallConfig) = GetContentAndPaywallConfig(contentId);
 if (paywallConfig == null || !paywallConfig.Enabled || paywallConfig.Fee ==0)
 {
 return BadRequest("Paywall is not enabled or fee is not set.");
 }

 var feesSat = await _breezSdkService.GetReceiveFeeQuoteAsync(paywallConfig.Fee, bolt12);
 return Ok(new { amountSat = paywallConfig.Fee, feesSat, method = bolt12 ? "bolt12" : "bolt11" });
 }
 catch (InvalidInvoiceRequestException ex)
 {
 _logger.LogWarning(ex, "Invalid request for fee quote.");
 return BadRequest(ex.Message);
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error fetching receive fee quote for contentId {ContentId}", contentId);
 return StatusCode(500, "An error occurred while fetching the fee quote.");
 }
 }

 private (IPublishedContent? Content, PaywallConfig? Config) GetContentAndPaywallConfig(int contentId)
 {
 using var cref = _umbracoContextFactory.EnsureUmbracoContext();
 var umbracoContext = cref.UmbracoContext;
 if (umbracoContext == null)
 {
 return (null, null);
 }

 var content = umbracoContext.Content?.GetById(contentId);
 if (content == null || !content.HasValue("breezPaywall"))
 {
 return (content, null);
 }

 var paywallJson = content.Value<string>("breezPaywall");
 var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
 var paywallConfig = JsonSerializer.Deserialize<PaywallConfig>(paywallJson ?? "{}", options);

 return (content, paywallConfig);
 }

 private async Task<string?> TryGetPaymentHash(string invoice)
 {
 var hash = await _breezSdkService.TryExtractPaymentHashAsync(invoice);
 if (!string.IsNullOrWhiteSpace(hash))
 {
 return hash.ToLowerInvariant();
 }

 if (_runtimeMode.IsOffline)
 {
 var m = OfflineHashRegex.Match(invoice);
 if (m.Success)
 {
 return m.Groups[1].Value.ToLowerInvariant();
 }
 }

 return null;
 }
 }

 public class TipInvoiceRequest
 {
 public ulong AmountSat { get; set; }
 public int? ContentId { get; set; }
 public string? Label { get; set; }
 }
}
