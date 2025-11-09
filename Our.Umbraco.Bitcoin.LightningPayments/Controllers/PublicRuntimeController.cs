using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Our.Umbraco.Bitcoin.LightningPayments.Services;

namespace Our.Umbraco.Bitcoin.LightningPayments.Controllers
{
    [ApiController]
    [Route("api/public/lightning/runtime")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class PublicRuntimeController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get([FromServices] IRuntimeSettingsService runtime, CancellationToken ct)
        {
            var flags = await runtime.GetAsync(ct);
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
