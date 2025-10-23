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

            // Default runtime mode marker (online by default)
            builder.Services.AddSingleton<ILightningPaymentsRuntimeMode>(_ => new LightningPaymentsRuntimeMode(isOffline: false));

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
            builder.Services.AddSingleton<BreezEventProcessor>();
            builder.Services.AddSingleton<IBreezEventProcessor>(sp => sp.GetRequiredService<BreezEventProcessor>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<BreezEventProcessor>());
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IPaymentEventDeduper, MemoryPaymentEventDeduper>();

            builder.Services.AddHealthChecks().AddCheck<BreezSdkHealthCheck>("breez");

            // Middleware and endpoint registration moved to Composer to ensure correct Umbraco pipeline ordering.

            return builder;
        }

        /// <summary>
        /// Enables offline mode: no calls to Breez SDK are made. 
        /// A mocked service returns synthetic invoices and simulates confirmations.
        /// </summary>
        public static IUmbracoBuilder UseLightningPaymentsOffline(this IUmbracoBuilder builder, Action<OfflineLightningPaymentsOptions>? configure = null)
        {
            var options = new OfflineLightningPaymentsOptions();
            configure?.Invoke(options);

            // Replace the runtime mode marker
            builder.Services.AddSingleton<ILightningPaymentsRuntimeMode>(_ => new LightningPaymentsRuntimeMode(isOffline: true));
            builder.Services.AddSingleton<IOptions<OfflineLightningPaymentsOptions>>(_ => Microsoft.Extensions.Options.Options.Create(options));

            // Replace the SDK service with the offline implementation
            builder.Services.AddSingleton<IBreezSdkService, OfflineBreezSdkService>();

            // Optionally replace persistent payment state with in-memory state to avoid touching storage
            if (options.UseInMemoryStateService)
            {
                builder.Services.AddScoped<IPaymentStateService, InMemoryPaymentStateService>();
            }

            return builder;
        }
    }
}
