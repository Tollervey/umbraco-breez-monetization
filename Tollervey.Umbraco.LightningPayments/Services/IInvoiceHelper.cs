using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
 /// <summary>
 /// Helper for common invoice-related flows including LNURL-P responses, session cookie management,
 /// and extracting metadata such as payment hash or expiry from invoices.
 ///
 /// NOTE: This helper now supports idempotency by accepting an optional `idempotencyKey` and
 /// using the configured `IPaymentStateService` to atomically persist and lookup mappings.
 /// The behavior: if a mapping exists for the key, the stored invoice/paymentHash is returned.
 /// Otherwise a new invoice is created via Breez SDK and the mapping is persisted atomically.
 /// </summary>
 public interface IInvoiceHelper
 {
 /// <summary>
 /// Loads the published content and deserializes the paywall configuration for the given content id.
 /// </summary>
 (IPublishedContent? Content, PaywallConfig? Config) GetContentAndPaywallConfig(int contentId);

 /// <summary>
 /// Attempts to extract the Lightning payment hash from a BOLT11 invoice. Supports offline mode fallback.
 /// </summary>
 Task<string?> TryGetPaymentHashAsync(string invoice);

 /// <summary>
 /// Creates a new invoice and returns both the invoice string and extracted payment hash.
 /// Throws when the hash cannot be obtained.
 /// Optional `idempotencyKey` may be provided by the caller and is preserved for downstream idempotency handling (persistence is out of scope for this change).
 ///
 /// Guidance:
 /// - To avoid duplicate invoices, callers should send a client-generated idempotency key (e.g. a UUID).
 /// - The server should persist a mapping from idempotencyKey -> paymentHash (and invoice string) atomically when creating a new invoice.
 /// - If a subsequent request arrives with the same idempotencyKey, the server should return the previously-created invoice/paymentHash instead
 /// of creating a new one. If the previously-created payment has already been confirmed, the server may return the confirmed state and
 /// avoid presenting a new invoice.
 /// </summary>
 Task<(string invoice, string paymentHash)> CreateInvoiceAndHashAsync(ulong amountSat, string description, string? idempotencyKey = null);

 /// <summary>
 /// Ensures the session cookie used for paywall state exists, returning its value.
 /// </summary>
 string EnsureSessionCookie(HttpRequest request, HttpResponse response, string? explicitState = null);

 /// <summary>
 /// Builds a standard LNURL-Pay info response for the specified content id.
 /// </summary>
 IActionResult BuildLnurlPayInfo(int contentId, HttpRequest request, string callbackPath, ILogger logger);

 /// <summary>
 /// Attempts to extract the expiry timestamp (UTC) of an invoice, if present.
 /// </summary>
 Task<DateTimeOffset?> TryGetInvoiceExpiryAsync(string invoice, CancellationToken ct = default);
 }

 internal sealed class InvoiceHelper : IInvoiceHelper
 {
 private static readonly Regex OfflineHashRegex = new(@"(?:^|-)p=([0-9a-f]{64})(?:-|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
 private readonly IBreezSdkService _breezSdkService;
 private readonly IUmbracoContextFactory _umbracoContextFactory;
 private readonly ILightningPaymentsRuntimeMode _runtimeMode;
 private readonly LightningPaymentsSettings _settings;
 private readonly IPaymentStateService _paymentStateService;

 public InvoiceHelper(IBreezSdkService breezSdkService, IUmbracoContextFactory umbracoContextFactory, ILightningPaymentsRuntimeMode runtimeMode, IOptions<LightningPaymentsSettings> settings, IPaymentStateService paymentStateService)
 {
 _breezSdkService = breezSdkService;
 _umbracoContextFactory = umbracoContextFactory;
 _runtimeMode = runtimeMode;
 _settings = settings.Value;
 _paymentStateService = paymentStateService;
 }

 public (IPublishedContent? Content, PaywallConfig? Config) GetContentAndPaywallConfig(int contentId)
 {
 using var cref = _umbracoContextFactory.EnsureUmbracoContext();
 var umbracoContext = cref.UmbracoContext;
 if (umbracoContext == null) return (null, null);
 var content = umbracoContext.Content?.GetById(contentId);
 if (content == null || !content.HasValue("breezPaywall")) return (content, null);
 var paywallJson = content.Value<string>("breezPaywall");
 var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
 var paywallConfig = JsonSerializer.Deserialize<PaywallConfig>(paywallJson ?? "{}", options);
 return (content, paywallConfig);
 }

 public async Task<string?> TryGetPaymentHashAsync(string invoice)
 {
 var hash = await _breezSdkService.TryExtractPaymentHashAsync(invoice);
 if (!string.IsNullOrWhiteSpace(hash)) return hash.ToLowerInvariant();
 if (_runtimeMode.IsOffline)
 {
 var m = OfflineHashRegex.Match(invoice);
 if (m.Success) return m.Groups[1].Value.ToLowerInvariant();
 }
 return null;
 }

 public async Task<(string invoice, string paymentHash)> CreateInvoiceAndHashAsync(ulong amountSat, string description, string? idempotencyKey = null)
 {
 // If idempotencyKey provided, attempt to return existing mapping to avoid duplicate invoices on retries.
 if (!string.IsNullOrWhiteSpace(idempotencyKey))
 {
 var existing = await _paymentStateService.TryGetMappingByKeyAsync(idempotencyKey);
 if (existing != null)
 {
 // If the mapping exists and the payment is confirmed, return the stored invoice/paymentHash.
 // The mapping.Status is kept in sync when payments are confirmed through ConfirmPaymentAsync.
 return (existing.Invoice, existing.PaymentHash);
 }
 }

 // Not found or no idempotency key supplied — create a new invoice via Breez SDK
 var invoice = await _breezSdkService.CreateInvoiceAsync(amountSat, description);
 var hash = await TryGetPaymentHashAsync(invoice);
 if (string.IsNullOrWhiteSpace(hash)) throw new InvalidInvoiceRequestException("Failed to obtain invoice payment hash.");

 // If idempotency key supplied, attempt to persist mapping atomically. If another request inserted the mapping
 // concurrently, the TryCreateMappingAsync will return the existing mapping and we should use that instead.
 if (!string.IsNullOrWhiteSpace(idempotencyKey))
 {
 var (mapping, created) = await _paymentStateService.TryCreateMappingAsync(idempotencyKey, hash, invoice);
 if (!created)
 {
 // Another request already created a mapping; return the existing mapping instead of the newly-created invoice.
 return (mapping.Invoice, mapping.PaymentHash);
 }
 }

 return (invoice, hash!);
 }

 public string EnsureSessionCookie(HttpRequest request, HttpResponse response, string? explicitState = null)
 {
 var sessionId = !string.IsNullOrWhiteSpace(explicitState)
 ? explicitState!
 : (request.Cookies[Middleware.PaywallMiddleware.PaywallCookieName] ?? Guid.NewGuid().ToString());
 var options = new CookieOptions { HttpOnly = true, Secure = true, SameSite = _settings.SessionCookieSameSite };
 if (!string.IsNullOrWhiteSpace(_settings.SessionCookieDomain)) options.Domain = _settings.SessionCookieDomain;
 response.Cookies.Append(Middleware.PaywallMiddleware.PaywallCookieName, sessionId, options);
 return sessionId;
 }

 public IActionResult BuildLnurlPayInfo(int contentId, HttpRequest request, string callbackPath, ILogger logger)
 {
 if (contentId <=0)
 {
 return new BadRequestObjectResult(new { status = "ERROR", reason = "Invalid content ID." });
 }
 try
 {
 var (content, paywallConfig) = GetContentAndPaywallConfig(contentId);
 if (content == null || paywallConfig == null)
 {
 return new NotFoundObjectResult(new { status = "ERROR", reason = "Content or paywall configuration not found." });
 }
 if (!paywallConfig.Enabled || paywallConfig.Fee ==0)
 {
 return new BadRequestObjectResult(new { status = "ERROR", reason = "Paywall is not enabled or fee is not set." });
 }
 // ensure session and attach it as state for wallet callback
 var sessionId = EnsureSessionCookie(request, request.HttpContext.Response);
 var callback = $"{request.Scheme}://{request.Host}{callbackPath}?contentId={contentId}&state={Uri.EscapeDataString(sessionId)}";
 // build metadata and compute description_hash
 var name = content.Name;
 var metadataArray = new object[][]
 {
 new object[] { "text/plain", $"Access to {name}" },
 new object[] { "text/description_hash", "" } // placeholder to advertise presence; wallets usually compute it themselves
 };
 var metadata = JsonSerializer.Serialize(metadataArray);
 using var sha = SHA256.Create();
 var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(metadata));
 var descriptionHashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
 // replace placeholder value with actual hash
 metadataArray[1][1] = descriptionHashHex;
 metadata = JsonSerializer.Serialize(metadataArray);
 ulong minSendable = paywallConfig.Fee *1000UL;
 ulong maxSendable = minSendable;
 return new OkObjectResult(new { tag = "payRequest", callback, minSendable, maxSendable, metadata, descriptionHash = descriptionHashHex });
 }
 catch (Exception ex)
 {
 logger.LogError(ex, "Error generating LNURL-Pay info for contentId {ContentId}", contentId);
 return new ObjectResult(new { status = "ERROR", reason = "An error occurred while generating LNURL-Pay info." }) { StatusCode =500 };
 }
 }

 public Task<DateTimeOffset?> TryGetInvoiceExpiryAsync(string invoice, CancellationToken ct = default)
 {
 return _breezSdkService.TryExtractInvoiceExpiryAsync(invoice, ct);
 }
 }
}
