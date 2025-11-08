using Microsoft.Extensions.DependencyInjection;
using Our.Umbraco.Bitcoin.LightningPayments.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Our.Umbraco.Bitcoin.LightningPayments.Composing
{
    public class LightningPaymentsComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Register our service with Umbraco's DI container [24, 25, 26]
            // This makes ILightningService available for injection in controllers.
            builder.Services.AddSingleton<ILightningService, LightningService>();
        }
    }
}