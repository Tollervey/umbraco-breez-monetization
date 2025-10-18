using Microsoft.AspNetCore.Mvc;
using Tollervey.Umbraco.LightningPayments.Models;
using System.Text.Json;
using Tollervey.LightningPayments.Core.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Extensions;

namespace Tollervey.Umbraco.LightningPayments.Controllers
{
    [RequireHttps]
    public class PaywallSurfaceController : SurfaceController
    {
        public PaywallSurfaceController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
        }

        /// <summary>
        /// Displays the paywall page for a specific content item, showing preview and payment options.
        /// </summary>
        /// <param name="contentId">The ID of the Umbraco content item.</param>
        /// <returns>A view with paywall details or a not found response.</returns>
        public IActionResult Index(int contentId)
        {
            if (contentId <= 0)
            {
                return NotFound();
            }

            if (!UmbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
            {
                return NotFound();
            }

            var content = umbracoContext.Content?.GetById(contentId);
            if (content == null || !content.HasValue("breezPaywall"))
            {
                return NotFound();
            }

            var paywallJson = content.Value<string>("breezPaywall");
            var paywallConfig = JsonSerializer.Deserialize<PaywallConfig>(paywallJson ?? "{}");

            if (paywallConfig is not { Enabled: true })
            {
                return NotFound();
            }

            var previewContent = content.HasValue("breezPaywallPreview")
                ? content.Value<string>("breezPaywallPreview") ?? "<h2>Content Locked</h2><p>This content is available after a one-time payment.</p>"
                : "<h2>Content Locked</h2><p>This content is available after a one-time payment.</p>";

            var model = new PaywallViewModel
            {
                ContentId = contentId,
                PreviewContent = previewContent,
                Fee = paywallConfig?.Fee ?? 0
            };

            return View("Index", model);
        }
    }
}