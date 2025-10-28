using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Middleware;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Tollervey.Umbraco.LightningPayments.UI.Components;

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
            // Register services, options, health checks, etc.
            builder.AddLightningPayments();

            // Ensure BreezSdkService is tied to Umbraco app lifecycle
            builder.Components().Append<BreezSdkComponent>();

            // Swagger is intentionally not registered here to avoid forcing a transitive dependency.
            // Consumers can add Swagger in their host application if desired.

            // Register middleware using Umbraco pipeline filters (applies to both pipelines in v16).
            builder.Services.Configure<UmbracoPipelineOptions>(options =>
            {
                options.AddFilter(new UmbracoPipelineFilter("LightningPayments")
                {
                    // After routing and authentication but before endpoints.
                    PostRouting = app =>
                    {
                        // Order matters: exception handler first to wrap paywall.
                        app.UseMiddleware<ExceptionHandlingMiddleware>();
                        app.UseMiddleware<PaywallMiddleware>();
                    },
                    // Map health checks in the Endpoints stage.
                    Endpoints = app =>
                    {
                        var settings = app.ApplicationServices.GetRequiredService<IOptions<LightningPaymentsSettings>>().Value;
                        app.UseEndpoints(endpoints => endpoints.MapHealthChecks(settings.HealthCheckPath ?? "/health/ready"));
                    }
                });
            });

            // Remove deduper registrations if present (now unused). This is just a comment for future reference:
            // builder.Services.Remove(ServiceDescriptor.Singleton<IPaymentEventDeduper, MemoryPaymentEventDeduper>());
        }
    }
}