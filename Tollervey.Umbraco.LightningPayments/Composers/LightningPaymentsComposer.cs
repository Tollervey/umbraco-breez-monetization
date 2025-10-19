using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tollervey.LightningPayments.Breez.Configuration;
using Tollervey.LightningPayments.Breez.Services;
using Tollervey.Umbraco.LightningPayments.Middleware;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Tollervey.Umbraco.LightningPayments.Composers
{


    public class LightningPaymentsComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)

        {
            // Bind the "LightningPayments" section of appsettings to the settings model
            builder.Services.AddOptions<LightningPaymentsSettings>()
                .Bind(builder.Config.GetSection(LightningPaymentsSettings.SectionName))
                .ValidateDataAnnotations();

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
            builder.Services.AddSingleton<IBreezSdkService, BreezSdkService>();

            // Register middleware
            builder.Services.AddTransient<ExceptionHandlingMiddleware>();
            builder.Services.AddTransient<PaywallMiddleware>();
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
            });
        }
    }
}
