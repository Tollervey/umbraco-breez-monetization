using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Tollervey.LightningPayments.Breez.Models;
using Tollervey.LightningPayments.Breez.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Tollervey.Umbraco.LightningPayments.Middleware
{
    public class PaywallMiddleware
    {
        private readonly RequestDelegate _next;
        public const string PaywallCookieName = "LightningPaymentsSession";

        public PaywallMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUmbracoContextAccessor umbracoContextAccessor, IPaymentStateService paymentStateService)
        {
            if (!umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext) ||
                umbracoContext.PublishedRequest?.PublishedContent == null)
            {
                await _next(context);
                return;
            }

            var content = umbracoContext.PublishedRequest.PublishedContent;
            if (!content.HasProperty("breezPaywall") || !content.HasValue("breezPaywall"))
            {
                await _next(context);
                return;
            }

            var paywallJson = content.Value<string>("breezPaywall");
            var paywallConfig = JsonSerializer.Deserialize<PaywallConfig>(paywallJson ?? "{}");

            if (paywallConfig is not { Enabled: true })
            {
                await _next(context);
                return;
            }

            var sessionId = context.Request.Cookies[PaywallCookieName];
            if (!string.IsNullOrEmpty(sessionId))
            {
                var paymentState = await paymentStateService.GetPaymentStateAsync(sessionId, content.Id);
                if (paymentState?.Status == PaymentStatus.Paid)
                {
                    // Access granted, proceed to normal Umbraco rendering
                    await _next(context);
                    return;
                }
            }

            // Access denied, redirect to paywall page
            var paywallUrl = $"/umbraco/surface/PaywallSurface/Index?contentId={content.Id}";
            context.Response.Redirect(paywallUrl);
        }
    }
}
