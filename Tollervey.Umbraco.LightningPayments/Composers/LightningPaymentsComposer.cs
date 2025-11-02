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
            // Restore the original service registrations
            builder.AddLightningPayments();

            // Register services, options, health checks, etc.
            builder.Components().Append<MinimalTestComponent>();
        }
    }
}