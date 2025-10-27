using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Umbraco.Cms.Web.Common.Controllers;

namespace Tollervey.Umbraco.LightningPayments.UI.Controllers
{
    [RequireHttps]
    public class WellKnownController : UmbracoApiControllerBase
    {
        private readonly ILogger<WellKnownController> _logger;
        private readonly IInvoiceHelper _invoiceHelper;

        public WellKnownController(
            ILogger<WellKnownController> logger,
            IInvoiceHelper invoiceHelper)
        {
            _logger = logger;
            _invoiceHelper = invoiceHelper;
        }

        [HttpGet("/.well-known/lnurlp/{name}")]
        public IActionResult GetLightningAddress(string name, [FromQuery] int contentId)
        {
            // Delegate to shared helper that encapsulates Umbraco access and cookie handling
            return _invoiceHelper.BuildLnurlPayInfo(contentId, Request, "/api/public/lightning/GetLnurlInvoice", _logger);
        }
    }
}