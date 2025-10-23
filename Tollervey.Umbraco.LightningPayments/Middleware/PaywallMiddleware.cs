using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Tollervey.Umbraco.LightningPayments.UI.Models;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Tollervey.Umbraco.LightningPayments.UI.Middleware
{
    public class PaywallMiddleware
    {
        private readonly RequestDelegate _next;
        public const string PaywallCookieName = "LightningPaymentsSession";

        // Common prefixes to bypass (backoffice + our own surface controller)
        private static readonly PathString BackofficePrefix = new("/umbraco");
        private static readonly PathString PaywallSurfacePrefix = new("/umbraco/surface/paywallsurface");

        public PaywallMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IUmbracoContextAccessor umbracoContextAccessor,
            IPaymentStateService paymentStateService)
        {
            // 1) Bypass for non-website requests (backoffice, APIs, static, non-HTML, non-GET, our own surface endpoint)
            if (ShouldBypass(context))
            {
                await _next(context);
                return;
            }

            // 2) Require a valid Umbraco front-end request with published content
            if (!umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext) ||
                umbracoContext.PublishedRequest?.PublishedContent == null)
            {
                await _next(context);
                return;
            }

            // 2a) Don’t interfere with editor preview
            if (umbracoContext.InPreviewMode)
            {
                await _next(context);
                return;
            }

            var content = umbracoContext.PublishedRequest.PublishedContent;

            // 3) Only enforce paywall if property exists and has a value
            if (!content.HasProperty("breezPaywall") || !content.HasValue("breezPaywall"))
            {
                await _next(context);
                return;
            }

            // 4) Read config safely
            PaywallConfig? paywallConfig = null;
            var paywallJson = content.Value<string>("breezPaywall");
            try
            {
                paywallConfig = JsonSerializer.Deserialize<PaywallConfig>(paywallJson ?? "{}");
            }
            catch
            {
                // On bad JSON, don’t block the page
                await _next(context);
                return;
            }

            if (paywallConfig is not { Enabled: true })
            {
                await _next(context);
                return;
            }

            // 5) If we have a session cookie and it’s paid for this content, allow through
            var sessionId = context.Request.Cookies[PaywallCookieName];
            if (!string.IsNullOrEmpty(sessionId))
            {
                var paymentState = await paymentStateService.GetPaymentStateAsync(sessionId, content.Id);
                if (paymentState?.Status == PaymentStatus.Paid)
                {
                    await _next(context);
                    return;
                }
            }

            // 6) Access denied, redirect to paywall page (we already bypass this path above to avoid loops)
            var paywallUrl = $"/umbraco/surface/PaywallSurface/Index?contentId={content.Id}";
            context.Response.Redirect(paywallUrl);
        }

        private static bool ShouldBypass(HttpContext context)
        {
            var path = context.Request.Path;

            // Skip for backoffice/management/API and our own surface endpoint
            if (path.StartsWithSegments(PaywallSurfacePrefix, StringComparison.OrdinalIgnoreCase))
                return true;

            if (path.StartsWithSegments(BackofficePrefix, StringComparison.OrdinalIgnoreCase))
                return true;

            // Only act on GET/HEAD requests
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
                return true;

            // Only act for HTML requests
            if (!AcceptsHtml(context))
                return true;

            return false;
        }

        private static bool AcceptsHtml(HttpContext context)
        {
            // If no Accept header is present, assume it can accept HTML (browsers often send */*)
            var accept = context.Request.Headers["Accept"].ToString();
            if (string.IsNullOrWhiteSpace(accept)) return true;

            // Look for text/html or */*
            return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
                   || accept.Contains("*/*", StringComparison.OrdinalIgnoreCase);
        }
    }
}