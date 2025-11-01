using Microsoft.AspNetCore.Mvc;
using Tollervey.Umbraco.LightningPayments.UI.Services;

namespace Tollervey.Umbraco.LightningPayments.UI.Controllers
{
    [ApiController]
    [Route("api/public/lightning/runtime")]
    public class PublicRuntimeController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get([FromServices] IRuntimeSettingsService runtime)
        {
            var flags = await runtime.GetAsync();
            // expose minimal set
            return Ok(new
            {
                enabled = flags.Enabled,
                hideUiWhenDisabled = flags.HideUiWhenDisabled,
                tipJarEnabled = flags.TipJarEnabled,
                paywallEnabled = flags.PaywallEnabled
            });
        }
    }
}