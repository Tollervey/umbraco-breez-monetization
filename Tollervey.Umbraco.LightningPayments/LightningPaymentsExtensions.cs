using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;
using Tollervey.Umbraco.LightningPayments.UI.Middleware;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Tollervey.Umbraco.LightningPayments.UI.Services.Realtime;
using Tollervey.Umbraco.LightningPayments.UI.Services.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to register Lightning Payments services and related features with Umbraco.
    /// </summary>
    public static class LightningPaymentsExtensions
    {
        /// <summary>
        /// Adds Lightning Payments services, options, health checks, and infrastructure to the Umbraco builder.
        /// Call this in a composer during startup.
        /// </summary>
        /// <param name="builder">The Umbraco builder.</param>
        /// <returns>The same <paramref name="builder"/> for chaining.</returns>
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

            // NOTE: Application Insights is intentionally NOT registered here automatically. Consumers should opt-in by calling
            // AddLightningPaymentsApplicationInsights on the IUmbracoBuilder if they want AI wired up for this library.

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
            builder.Services.AddSingleton<IBreezSdkHandleProvider>(sp => (IBreezSdkHandleProvider)sp.GetRequiredService<IBreezSdkService>());
            builder.Services.AddScoped<IBreezPaymentsFacade, BreezPaymentsFacade>();
            builder.Services.AddSingleton<BreezEventProcessor>();
            builder.Services.AddSingleton<IBreezEventProcessor>(sp => sp.GetRequiredService<BreezEventProcessor>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<BreezEventProcessor>());
            builder.Services.AddHostedService<PaymentDbInitializer>();
            builder.Services.AddMemoryCache();

            // Realtime hub (SSE)
            builder.Services.AddSingleton<SseHub>();

            // Rate limiter
            builder.Services.AddSingleton<IRateLimiter, MemoryRateLimiter>();
            builder.Services.AddScoped<IInvoiceHelper, InvoiceHelper>();

            builder.Services.AddHealthChecks().AddCheck<BreezSdkHealthCheck>("breez");

            // Middleware and endpoint registration moved to Composer to ensure correct Umbraco pipeline ordering.

            return builder;
        }

        /// <summary>
        /// Registers Application Insights telemetry for the host when using the Lightning Payments package.
        /// Call this from the consuming application's startup if Application Insights is desired.
        /// </summary>
        /// <param name="builder">The Umbraco builder.</param>
        /// <param name="connectionString">Application Insights connection string.</param>
        /// <returns>The same <paramref name="builder"/> for chaining.</returns>
        public static IUmbracoBuilder AddLightningPaymentsApplicationInsights(this IUmbracoBuilder builder, string connectionString)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                builder.Services.AddApplicationInsightsTelemetry(options => { options.ConnectionString = connectionString; });
            }
            return builder;
        }

        /// <summary>
        /// Registers Application Insights telemetry using the configured value in the LightningPayments configuration section.
        /// This is a convenience method; the consumer can also call AddLightningPaymentsApplicationInsights explicitly.
        /// </summary>
        public static IUmbracoBuilder AddLightningPaymentsApplicationInsightsFromConfig(this IUmbracoBuilder builder)
        {
            var aiConnectionString = builder.Config.GetSection(LightningPaymentsSettings.SectionName)["ApplicationInsightsConnectionString"];
            return builder.AddLightningPaymentsApplicationInsights(aiConnectionString ?? string.Empty);
        }

        /// <summary>
        /// Enables offline mode where no calls to Breez SDK are made and a mocked service simulates invoices and confirmations.
        /// Useful for development or demos without network access.
        /// </summary>
        /// <param name="builder">The Umbraco builder.</param>
        /// <param name="configure">Optional configuration for offline behavior.</param>
        /// <returns>The same <paramref name="builder"/> for chaining.</returns>
        public static IUmbracoBuilder UseLightningPaymentsOffline(this IUmbracoBuilder builder, Action<OfflineLightningPaymentsOptions>? configure = null)
        {
            var options = new OfflineLightningPaymentsOptions();
            configure?.Invoke(options);

            // Replace the runtime mode marker
            builder.Services.AddSingleton<ILightningPaymentsRuntimeMode>(_ => new LightningPaymentsRuntimeMode(isOffline: true));
            builder.Services.AddSingleton<IOptions<OfflineLightningPaymentsOptions>>(_ => Microsoft.Extensions.Options.Options.Create(options));

            // Replace the SDK service with the offline implementation
            builder.Services.AddSingleton<IBreezSdkService, OfflineBreezSdkService>();
            builder.Services.AddSingleton<IBreezSdkHandleProvider>(sp => (IBreezSdkHandleProvider)sp.GetRequiredService<IBreezSdkService>());
            builder.Services.AddScoped<IBreezPaymentsFacade, BreezPaymentsFacade>();

            // Optionally replace persistent payment state with in-memory state to avoid touching storage
            if (options.UseInMemoryStateService)
            {
                builder.Services.AddScoped<IPaymentStateService, InMemoryPaymentStateService>();
            }

            return builder;
        }
    }
}
