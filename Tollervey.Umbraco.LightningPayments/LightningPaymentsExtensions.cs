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
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System;
using System.ComponentModel;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to register Lightning Payments services and related features with Umbraco.
    /// </summary>
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

            // Bind rate limiting options (optional)
            var rlSection = builder.Config.GetSection($"{LightningPaymentsSettings.SectionName}:RateLimiting");
            var rlOptions = rlSection.Get<RateLimitingOptions>() ?? new RateLimitingOptions();
            builder.Services.Configure<RateLimitingOptions>(rlSection);

            // If consumer wants to use ASP.NET Core RateLimiting middleware, register it according to options
            if (rlOptions.Enabled && rlOptions.UseAspNetRateLimiter)
            {
                builder.Services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = rlOptions.RejectionStatusCode;

                    options.AddPolicy("InvoiceGeneration", context =>
                    {
                        string partitionKey = rlOptions.PartitionByIp
                            ? (context.Connection.RemoteIpAddress?.ToString() ?? "unknown")
                            : "default";

                        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rlOptions.PermitLimit,
                            Window = TimeSpan.FromSeconds(Math.Max(1, rlOptions.WindowSeconds)),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = rlOptions.QueueLimit
                        });
                    });
                });
            }

            // Default runtime mode marker (online by default)
            builder.Services.AddSingleton<ILightningPaymentsRuntimeMode>(_ => new LightningPaymentsRuntimeMode(isOffline: false));

            // NOTE: Application Insights is intentionally NOT registered here automatically. Consumers should opt-in by calling
            // AddLightningPaymentsApplicationInsights on the IUmbracoBuilder if they want AI wired up for this library.

            // Register services
            builder.Services.AddDbContext<PaymentDbContext>((sp, options) =>
            {
                var settings = sp.GetRequiredService<IOptions<LightningPaymentsSettings>>().Value;
                options.UseSqlite(settings.ConnectionString);
            });
            builder.Services.AddScoped<IPaymentStateService, PersistentPaymentStateService>();
            // Email removed by default to simplify setup: no IEmailService registration.

            builder.Services.AddSingleton<IBreezSdkWrapper, BreezSdkWrapper>();
            builder.Services.AddSingleton<IBreezSdkService, BreezSdkService>();
            builder.Services.AddSingleton<IBreezSdkHandleProvider>(sp => (IBreezSdkHandleProvider)sp.GetRequiredService<IBreezSdkService>());
            builder.Services.AddScoped<IBreezPaymentsFacade, BreezPaymentsFacade>();
            builder.Services.AddSingleton<BreezEventProcessor>();
            builder.Services.AddSingleton<IBreezEventProcessor>(sp => sp.GetRequiredService<BreezEventProcessor>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<BreezEventProcessor>());
            builder.Services.AddHostedService<PaymentDbInitializer>();
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<IRuntimeSettingsService, RuntimeSettingsService>();

            builder.Services.AddSingleton<SseHub>();
            builder.Services.AddSingleton<IRateLimiter, MemoryRateLimiter>();
            builder.Services.AddScoped<IInvoiceHelper, InvoiceHelper>();

            builder.Services.AddHealthChecks().AddCheck<BreezSdkHealthCheck>("breez");

            return builder;
        }

        public static IUmbracoBuilder AddLightningPaymentsApplicationInsights(this IUmbracoBuilder builder, string connectionString)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                builder.Services.AddApplicationInsightsTelemetry(o => o.ConnectionString = connectionString);
            }
            return builder;
        }

        public static IUmbracoBuilder AddLightningPaymentsApplicationInsightsFromConfig(this IUmbracoBuilder builder)
        {
            var aiConnectionString = builder.Config.GetSection(LightningPaymentsSettings.SectionName)["ApplicationInsightsConnectionString"];
            return builder.AddLightningPaymentsApplicationInsights(aiConnectionString ?? string.Empty);
        }

        // Hide offline mode (still present for dev-internal use, but discouraged for consumers)
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Offline mode is not exposed in this release.", error: false)]
        public static IUmbracoBuilder UseLightningPaymentsOffline(this IUmbracoBuilder builder, Action<OfflineLightningPaymentsOptions>? configure = null)
        {
            var options = new OfflineLightningPaymentsOptions();
            configure?.Invoke(options);

            builder.Services.AddSingleton<ILightningPaymentsRuntimeMode>(_ => new LightningPaymentsRuntimeMode(isOffline: true));
            builder.Services.AddSingleton<Microsoft.Extensions.Options.IOptions<OfflineLightningPaymentsOptions>>(
                _ => Microsoft.Extensions.Options.Options.Create(options)
            );

            builder.Services.AddSingleton<IBreezSdkService, OfflineBreezSdkService>();
            builder.Services.AddSingleton<IBreezSdkHandleProvider>(sp => (IBreezSdkHandleProvider)sp.GetRequiredService<IBreezSdkService>());
            builder.Services.AddScoped<IBreezPaymentsFacade, BreezPaymentsFacade>();
                
            if (options.UseInMemoryStateService)
            {
                builder.Services.AddScoped<IPaymentStateService, InMemoryPaymentStateService>();
            }
                
            return builder;
        }
    }
}
