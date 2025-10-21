using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Middleware;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LightningPaymentsExtensions
    {
        public static IUmbracoBuilder AddLightningPayments(this IUmbracoBuilder builder)
        {
            // Bind the "LightningPayments" section of appsettings to the settings model
            builder.Services.AddOptions<LightningPaymentsSettings>()
                .Bind(builder.Config.GetSection(LightningPaymentsSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddSingleton<IValidateOptions<LightningPaymentsSettings>, LightningPaymentsSettingsValidator>();

            // Add Application Insights if connection string is provided
            var aiConnectionString = builder.Config.GetSection(LightningPaymentsSettings.SectionName)["ApplicationInsightsConnectionString"];
            if (!string.IsNullOrEmpty(aiConnectionString))
            {
                builder.Services.AddApplicationInsightsTelemetry(options =>
                {
                    options.ConnectionString = aiConnectionString;
                });
            }

            // Register services
            builder.Services.AddDbContext<PaymentDbContext>((serviceProvider, options) =>
            {
                var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LightningPaymentsSettings>>().Value;
                options.UseSqlite(settings.ConnectionString);
            });
            builder.Services.AddScoped<IPaymentStateService, PersistentPaymentStateService>();
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();
            builder.Services.AddSingleton<IBreezSdkWrapper, BreezSdkWrapper>();
            builder.Services.AddSingleton<IBreezSdkService, BreezSdkService>();
            builder.Services.AddScoped<IBreezEventProcessor, BreezEventProcessor>();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IPaymentEventDeduper, MemoryPaymentEventDeduper>();

            builder.Services.AddHealthChecks().AddCheck<BreezSdkHealthCheck>("breez");

            // Register middleware
            builder.Services.Configure<UmbracoPipelineOptions>(options =>
            {
                options.AddFilter(new UmbracoPipelineFilter(nameof(ExceptionHandlingMiddleware))
                {
                    PreRouting = app => app.UseMiddleware<ExceptionHandlingMiddleware>()
                });
                options.AddFilter(new UmbracoPipelineFilter(nameof(PaywallMiddleware))
                {
                    PostRouting = app => app.UseMiddleware<PaywallMiddleware>()
                });
                options.AddFilter(new UmbracoPipelineFilter("HealthChecks")
                {
                    PostRouting = app => app.UseEndpoints(endpoints => endpoints.MapHealthChecks("/health/ready"))
                });
            });

            return builder;
        }
    }
}
