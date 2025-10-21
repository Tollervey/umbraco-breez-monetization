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
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly ILogger<WellKnownController> _logger;

        public WellKnownController(
            IUmbracoContextAccessor umbracoContextAccessor,
            ILogger<WellKnownController> logger)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _logger = logger;
        }

        [HttpGet("/.well-known/lnurlp/{name}")]
        public IActionResult GetLightningAddress(string name, [FromQuery] int contentId)
        {
            // Ignore name for now
            return LnurlPayHelper.GetLnurlPayInfo(contentId, _umbracoContextAccessor, _logger, Request, "/umbraco/api/LightningPaymentsApi/GetLnurlInvoice");
        }
    }
}