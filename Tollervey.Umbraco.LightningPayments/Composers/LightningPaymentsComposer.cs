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
            builder.AddLightningPayments();
        }
    }
}