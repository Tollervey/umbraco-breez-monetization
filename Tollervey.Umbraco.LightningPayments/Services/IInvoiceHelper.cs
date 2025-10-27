using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
 public interface IInvoiceHelper
 {
 (IPublishedContent? Content, PaywallConfig? Config) GetContentAndPaywallConfig(int contentId);
 Task<string?> TryGetPaymentHashAsync(string invoice);
 Task<(string invoice, string paymentHash)> CreateInvoiceAndHashAsync(ulong amountSat, string description);
 string EnsureSessionCookie(HttpRequest request, HttpResponse response, string? explicitState = null);
 IActionResult BuildLnurlPayInfo(int contentId, HttpRequest request, string callbackPath, ILogger logger);
 }

 internal sealed class InvoiceHelper : IInvoiceHelper
 {
 private static readonly Regex OfflineHashRegex = new(@"(?:^|-)p=([0-9a-f]{64})(?:-|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
 private readonly IBreezSdkService _breezSdkService;
 private readonly IUmbracoContextFactory _umbracoContextFactory;
 private readonly ILightningPaymentsRuntimeMode _runtimeMode;

 public InvoiceHelper(IBreezSdkService breezSdkService, IUmbracoContextFactory umbracoContextFactory, ILightningPaymentsRuntimeMode runtimeMode)
 {
 _breezSdkService = breezSdkService;
 _umbracoContextFactory = umbracoContextFactory;
 _runtimeMode = runtimeMode;
 }

 public (IPublishedContent? Content, PaywallConfig? Config) GetContentAndPaywallConfig(int contentId)
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

 public async Task<string?> TryGetPaymentHashAsync(string invoice)
 {
 var hash = await _breezSdkService.TryExtractPaymentHashAsync(invoice);
 if (!string.IsNullOrWhiteSpace(hash))
 {
 return hash.ToLowerInvariant();
 }
 if (_runtimeMode.IsOffline)
 {
 var m = OfflineHashRegex.Match(invoice);
 if (m.Success) return m.Groups[1].Value.ToLowerInvariant();
 }
 return null;
 }

 public async Task<(string invoice, string paymentHash)> CreateInvoiceAndHashAsync(ulong amountSat, string description)
 {
 var invoice = await _breezSdkService.CreateInvoiceAsync(amountSat, description);
 var hash = await TryGetPaymentHashAsync(invoice);
 if (string.IsNullOrWhiteSpace(hash)) throw new InvalidInvoiceRequestException("Failed to obtain invoice payment hash.");
 return (invoice, hash!);
 }

 public string EnsureSessionCookie(HttpRequest request, HttpResponse response, string? explicitState = null)
 {
 var sessionId = !string.IsNullOrWhiteSpace(explicitState)
 ? explicitState!
 : (request.Cookies[Middleware.PaywallMiddleware.PaywallCookieName] ?? Guid.NewGuid().ToString());
 response.Cookies.Append(Middleware.PaywallMiddleware.PaywallCookieName, sessionId, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
 return sessionId;
 }

 public IActionResult BuildLnurlPayInfo(int contentId, HttpRequest request, string callbackPath, ILogger logger)
 {
 if (contentId <=0)
 {
 return new BadRequestObjectResult("Invalid content ID.");
 }
 try
 {
 var (content, paywallConfig) = GetContentAndPaywallConfig(contentId);
 if (content == null || paywallConfig == null)
 {
 return new NotFoundObjectResult("Content or paywall configuration not found.");
 }
 if (!paywallConfig.Enabled || paywallConfig.Fee ==0)
 {
 return new BadRequestObjectResult("Paywall is not enabled or fee is not set.");
 }
 // ensure session and attach it as state for wallet callback
 var sessionId = EnsureSessionCookie(request, request.HttpContext.Response);
 var callback = $"{request.Scheme}://{request.Host}{callbackPath}?contentId={contentId}&state={Uri.EscapeDataString(sessionId)}";
 var metadata = $"""[[\"text/plain\",\"Access to {content.Name}\"]]""";
 ulong minSendable = paywallConfig.Fee *1000UL;
 ulong maxSendable = minSendable;
 return new OkObjectResult(new { tag = "payRequest", callback, minSendable, maxSendable, metadata });
 }
 catch (Exception ex)
 {
 logger.LogError(ex, "Error generating LNURL-Pay info for contentId {ContentId}", contentId);
 return new ObjectResult("An error occurred while generating LNURL-Pay info.") { StatusCode =500 };
 }
 }
 }
}
