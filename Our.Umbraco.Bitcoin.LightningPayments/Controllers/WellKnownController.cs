using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Our.Umbraco.Bitcoin.LightningPayments.Services;
using Umbraco.Cms.Web.Common.Controllers;

namespace Our.Umbraco.Bitcoin.LightningPayments.Controllers
{
    /// <summary>
    /// Serves .well-known endpoints related to Lightning, such as LNURL-P discovery.
    /// </summary>
    [RequireHttps]
    public class WellKnownController : UmbracoApiControllerBase
    {
        private readonly ILogger<WellKnownController> _logger;
        private readonly IInvoiceHelper _invoiceHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="WellKnownController"/> class.
        /// </summary>
        public WellKnownController(
            ILogger<WellKnownController> logger,
            IInvoiceHelper invoiceHelper)
        {
            _logger = logger;
            _invoiceHelper = invoiceHelper;
        }

        /// <summary>
        /// LNURL-Pay discovery endpoint for Lightning address-style lookups.
        /// </summary>
        /// <param name="name">The Lightning address user name segment.</param>
        /// <param name="contentId">Optional content id to scope the paywall configuration.</param>
        /// <returns>Standard LNURL-Pay metadata response.</returns>
        [HttpGet("/.well-known/lnurlp/{name}")]
        public IActionResult GetLightningAddress(string name, [FromQuery] int contentId)
        {
            // Delegate to shared helper that encapsulates Umbraco access and cookie handling
            return _invoiceHelper.BuildLnurlPayInfo(contentId, Request, "/api/public/lightning/GetLnurlInvoice", _logger);
        }
    }
}
