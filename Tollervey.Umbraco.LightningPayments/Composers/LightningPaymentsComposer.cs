using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Middleware;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Tollervey.Umbraco.LightningPayments.UI;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Tollervey.Umbraco.LightningPayments.UI.Composers
{
    public class LightningPaymentsComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Register services, options, health checks, etc.
            builder.AddLightningPayments();

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
                        app.UseEndpoints(endpoints => endpoints.MapHealthChecks("/health/ready"));
                    }
                });
            });
        }
    }
}