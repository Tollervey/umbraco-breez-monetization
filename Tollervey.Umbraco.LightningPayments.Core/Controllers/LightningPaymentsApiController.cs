using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BTCPayServer.Lightning;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tollervey.Umbraco.LightningPayments.Core.Middleware;
using Tollervey.Umbraco.LightningPayments.Core.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Core.Services;
using Tollervey.LightningPayments.Breez.Models;
using Umbraco.Extensions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Tollervey.LightningPayments.Breez.Services; // Ensure this is added if not present

namespace Tollervey.Umbraco.LightningPayments.Core.Controllers
{
    [RequireHttps]
    public class LightningPaymentsApiController : UmbracoAuthorizedApiController
    {
        private readonly IBreezSdkService _breezSdkService;
        private readonly IPaymentStateService _paymentStateService;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly ILogger<LightningPaymentsApiController> _logger;
        private readonly IUserService _userService;

        public LightningPaymentsApiController(
        IBreezSdkService breezSdkService,
        IPaymentStateService paymentStateService,
        IUmbracoContextAccessor umbracoContextAccessor,
        ILogger<LightningPaymentsApiController> logger,
        IUserService userService)
        {
            _breezSdkService = breezSdkService;
            _paymentStateService = paymentStateService;
            _umbracoContextAccessor = umbracoContextAccessor;
            _logger = logger;
            _userService = userService;
        }

        /// <summary>
        /// Generates a Lightning Network invoice for paywall access to a specific content item.
        /// </summary>
        /// <param name="contentId">The ID of the Umbraco content item requiring payment.</param>
        /// <returns>A JSON object containing the invoice and payment hash, or an error response.</returns>
        [HttpGet]
        public async Task<IActionResult> GetPaywallInvoice([FromQuery] int contentId)
        {
            if (contentId <= 0)
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

                if (!paywallConfig.Enabled || paywallConfig.Fee == 0)
                {
                    return BadRequest("Paywall is not enabled or fee is not set.");
                }

                _logger.LogInformation("Invoice requested for ContentId {ContentId} and Fee {Fee}", contentId, paywallConfig.Fee);

                var invoice = await _breezSdkService.CreateInvoiceAsync(paywallConfig.Fee, $"Access to content ID {contentId}");

                var bolt11 = BOLT11PaymentRequest.Parse(invoice, NBitcoin.Network.Main);
                if (bolt11?.PaymentHash == null)
                {
                    return BadRequest("Failed to parse invoice payment hash.");
                }
                var paymentHash = bolt11.PaymentHash.ToString();

                var sessionId = Request.Cookies[PaywallMiddleware.PaywallCookieName] ??
                                Guid.NewGuid().ToString();
                Response.Cookies.Append(PaywallMiddleware.PaywallCookieName, sessionId, new CookieOptions
                { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });

                await _paymentStateService.AddPendingPaymentAsync(paymentHash, contentId, sessionId);

                return Ok(new { invoice, paymentHash });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating paywall invoice for contentId {ContentId}", contentId);
                return StatusCode(500, "An error occurred while generating the invoice.");
            }
        }

        /// <summary>
        /// Gets the payment status for a user and content.
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
        /// Gets all payment transactions for admin view.
        /// </summary>
        [HttpGet("GetAllPayments")]
        public async Task<IActionResult> GetAllPayments()
        {
            var userId = int.Parse(User.Identity.GetUserId());
            var currentUser = _userService.GetUserById(userId);
            if (currentUser == null || !currentUser.Groups.Any(g => g.Name == "Administrators"))
            {
                return Unauthorized();
            }

            var payments = await _paymentStateService.GetAllPaymentsAsync();
            return Ok(payments);
        }

        /// <summary>
        /// Provides LNURL-Pay metadata for a specific content item.
        /// </summary>
        /// <param name="contentId">The ID of the Umbraco content item.</param>
        /// <returns>JSON object with LNURL-Pay details.</returns>
        [HttpGet]
        public IActionResult GetLnurlPayInfo([FromQuery] int contentId)
        {
            return LnurlPayHelper.GetLnurlPayInfo(contentId, _umbracoContextAccessor, _logger, Request, "/umbraco/api/LightningPaymentsApi/GetLnurlInvoice");
        }

        /// <summary>
        /// Callback to generate a BOLT11 invoice for LNURL-Pay.
        /// </summary>
        /// <param name="amount">Amount in millisatoshis.</param>
        /// <param name="contentId">The ID of the Umbraco content item.</param>
        /// <returns>JSON object with the payment request (pr).</returns>
        [HttpGet]
        public async Task<IActionResult> GetLnurlInvoice([FromQuery] long amount, [FromQuery] int contentId)
        {
            if (contentId <= 0)
            {
                return BadRequest("Invalid content ID.");
            }
            if (amount < 0 || amount % 1000 != 0)
            {
                return BadRequest("Invalid amount. Must be positive and divisible by 1000.");
            }

            ulong sats = (ulong)(amount / 1000);

            try
            {
                var (content, paywallConfig) = GetContentAndPaywallConfig(contentId);

                if (content == null || paywallConfig == null)
                {
                    return NotFound("Content or paywall configuration not found.");
                }

                if (!paywallConfig.Enabled || paywallConfig.Fee == 0)
                {
                    return BadRequest("Paywall is not enabled or fee is not set.");
                }

                if (sats != paywallConfig.Fee)
                {
                    return BadRequest("Amount does not match the required fee.");
                }

                string description = $"Access to {content.Name} (ID: {contentId})";
                var invoice = await _breezSdkService.CreateInvoiceAsync(sats, description);

                var bolt11 = BOLT11PaymentRequest.Parse(invoice, NBitcoin.Network.Main);
                if (bolt11?.PaymentHash == null)
                {
                    return BadRequest("Failed to parse invoice payment hash.");
                }
                var paymentHash = bolt11.PaymentHash.ToString();

                var sessionId = Request.Cookies[PaywallMiddleware.PaywallCookieName] ??
                                Guid.NewGuid().ToString();
                Response.Cookies.Append(PaywallMiddleware.PaywallCookieName, sessionId, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

                await _paymentStateService.AddPendingPaymentAsync(paymentHash, contentId, sessionId);

                return Ok(new { pr = invoice });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating LNURL invoice for contentId {ContentId} and amount {Amount}", contentId, amount);
                return StatusCode(500, "An error occurred while generating the invoice.");
            }
        }

        /// <summary>
        /// Gets the Bolt12 offer for a specific content item.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBolt12Offer([FromQuery] int contentId)
        {
            if (contentId <= 0)
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

                if (!paywallConfig.Enabled || paywallConfig.Fee == 0)
                {
                    return BadRequest("Paywall is not enabled or fee is not set.");
                }

                _logger.LogInformation("Bolt12 offer requested for ContentId {ContentId} and Fee {Fee}", contentId, paywallConfig.Fee);

                var offer = await _breezSdkService.CreateBolt12OfferAsync(paywallConfig.Fee, $"Access to content ID {contentId}");

                return Ok(offer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Bolt12 offer for contentId {ContentId}", contentId);
                return StatusCode(500, "An error occurred while generating the offer.");
            }
        }

        private (IPublishedContent? Content, PaywallConfig? Config) GetContentAndPaywallConfig(int contentId)
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
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
    }
}