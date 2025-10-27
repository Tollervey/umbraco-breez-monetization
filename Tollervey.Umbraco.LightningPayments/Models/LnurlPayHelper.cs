using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Tollervey.Umbraco.LightningPayments.UI.Models
{
    public static class LnurlPayHelper
    {
        public static IActionResult GetLnurlPayInfo(int contentId, IUmbracoContextFactory umbracoContextFactory, ILogger logger, HttpRequest request, string callbackPath)
        {
            if (contentId <= 0)
            {
                return new BadRequestObjectResult("Invalid content ID.");
            }

            try
            {
                using var cref = umbracoContextFactory.EnsureUmbracoContext();
                var umbracoContext = cref.UmbracoContext;
                if (umbracoContext == null)
                {
                    return new BadRequestObjectResult("Could not get Umbraco context.");
                }

                var content = umbracoContext.Content?.GetById(contentId);
                if (content == null || !content.HasValue("breezPaywall"))
                {
                    return new NotFoundObjectResult("Content or paywall configuration not found.");
                }

                var paywallJson = content.Value<string>("breezPaywall");
                var paywallConfig = JsonSerializer.Deserialize<PaywallConfig>(paywallJson ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (paywallConfig is not { Enabled: true } || paywallConfig.Fee == 0)
                {
                    return new BadRequestObjectResult("Paywall is not enabled or fee is not set.");
                }

                // Ensure we have a stable browser session identifier and put it on the callback as `state`.
                // This is required because LNURL callbacks are performed by the wallet (not the browser),
                // so cookies will not be sent back. Passing `state` lets us associate the pending payment
                // with the user's browser session for later unlock checks.
                var existingSession = request.Cookies[Middleware.PaywallMiddleware.PaywallCookieName];
                var sessionId = string.IsNullOrWhiteSpace(existingSession) ? Guid.NewGuid().ToString() : existingSession;
                if (string.IsNullOrWhiteSpace(existingSession))
                {
                    request.HttpContext.Response.Cookies.Append(
                        Middleware.PaywallMiddleware.PaywallCookieName,
                        sessionId,
                        new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict }
                    );
                }

                var callback = $"{request.Scheme}://{request.Host}{callbackPath}?contentId={contentId}&state={Uri.EscapeDataString(sessionId)}";
                var metadata = $"""[[\"text/plain\",\"Access to {content.Name}\"]]""";
                ulong minSendable = paywallConfig.Fee * 1000UL;
                ulong maxSendable = minSendable;

                return new OkObjectResult(new
                {
                    tag = "payRequest",
                    callback,
                    minSendable,
                    maxSendable,
                    metadata
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating LNURL-Pay info for contentId {ContentId}", contentId);
                return new ObjectResult("An error occurred while generating LNURL-Pay info.") { StatusCode = 500 };
            }
        }
    }
}