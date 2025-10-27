using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Hosting;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Middleware;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Tollervey.Umbraco.LightningPayments.UI;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Tollervey.Umbraco.LightningPayments.UI.Components;

namespace Tollervey.Umbraco.LightningPayments.UI.Composers
{
    public class LightningPaymentsComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Register services, options, health checks, etc.
            builder.AddLightningPayments();

            // Ensure BreezSdkService is tied to Umbraco app lifecycle
            builder.Components().Append<BreezSdkComponent>();

            // Swagger (dev-only): registers services; UI is enabled later in pipeline.
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Lightning Payments API",
                    Version = "v1",
                    Description = "Public and Management endpoints for Breez-powered Lightning paywalls and tips."
                });
            });

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

                        var env = app.ApplicationServices.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
                        if (env.IsDevelopment())
                        {
                            app.UseSwagger();
                            app.UseSwaggerUI(setup =>
                            {
                                setup.SwaggerEndpoint("/swagger/v1/swagger.json", "Lightning Payments API v1");
                                setup.RoutePrefix = "swagger";
                            });
                        }
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