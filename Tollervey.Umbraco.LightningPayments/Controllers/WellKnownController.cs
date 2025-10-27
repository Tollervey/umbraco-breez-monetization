using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tollervey.Umbraco.LightningPayments.UI.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Tollervey.Umbraco.LightningPayments.UI.Controllers
{
    [RequireHttps]
    public class WellKnownController : UmbracoApiControllerBase
    {
        private readonly IUmbracoContextFactory _umbracoContextFactory;
        private readonly ILogger<WellKnownController> _logger;

        public WellKnownController(
            IUmbracoContextFactory umbracoContextFactory,
            ILogger<WellKnownController> logger)
        {
            _umbracoContextFactory = umbracoContextFactory;
            _logger = logger;
        }

        [HttpGet("/.well-known/lnurlp/{name}")]
        public IActionResult GetLightningAddress(string name, [FromQuery] int contentId)
        {
            // The LNURL metadata callback should point to the public controller now
            return LnurlPayHelper.GetLnurlPayInfo(contentId, _umbracoContextFactory, _logger, Request, "/api/public/lightning/GetLnurlInvoice");
        }
    }
}