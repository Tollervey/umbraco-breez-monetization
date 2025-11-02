using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Tollervey.Umbraco.LightningPayments.UI.Middleware;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Tollervey.Umbraco.LightningPayments.UI.Components;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Tollervey.Umbraco.LightningPayments.UI.Composers
{
    /// <summary>
    /// Umbraco composer that wires up Lightning Payments services and middleware.
    /// Swagger is opt-in and should be configured by the consuming application.
    /// </summary>
    public class LightningPaymentsComposer : IComposer
    {
        /// <summary>
        /// Registers services and configures the Umbraco pipeline for Lightning Payments.
        /// </summary>
        public void Compose(IUmbracoBuilder builder)
        {
            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("LightningPayments.Startup");
            logger.LogInformation("--- Step 3: LightningPaymentsComposer.Compose() called. ---");

            // We are still using the minimal test component for this diagnostic.
            builder.Components().Append<MinimalTestComponent>();

            logger.LogInformation("--- Step 4: MinimalTestComponent appended. ---");
        }
    }
}