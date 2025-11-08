using Microsoft.AspNetCore.Mvc;
using Our.Umbraco.Bitcoin.LightningPayments.Services;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Api.Common.Attributes; // Add this for ApiExplorerSettings

namespace Our.Umbraco.Bitcoin.LightningPayments.Controllers
{
    [VersionedApiBackOfficeRoute("lightning-payments")]
    [ApiExplorerSettings(GroupName = "Lightning Payments")]
    public class LightningPaymentsApiController : ManagementApiControllerBase
    {
        private readonly ILightningService _lightningService;

        public LightningPaymentsApiController(ILightningService lightningService)
        {
            _lightningService = lightningService;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var status = await _lightningService.GetPaymentStatusAsync();
            return Ok(new { status = status });
        }
    }
}