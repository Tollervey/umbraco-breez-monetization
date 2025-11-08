using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

namespace Our.Umbraco.Bitcoin.LightningPayments.Controllers
{
    [ApiVersion("1.0")]
    [ApiExplorerSettings(GroupName = "Our.Umbraco.Bitcoin.LightningPayments")]
    public class OurUmbracoBitcoinLightningPaymentsApiController : OurUmbracoBitcoinLightningPaymentsApiControllerBase
    {
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

        public OurUmbracoBitcoinLightningPaymentsApiController(IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        {
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        }

        [HttpGet("ping")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        public string Ping() => "Pong";

        [HttpGet("whatsTheTimeMrWolf")]
        [ProducesResponseType(typeof(DateTime), 200)]
        public DateTime WhatsTheTimeMrWolf() => DateTime.Now;

        [HttpGet("whatsMyName")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        public string WhatsMyName()
        {
            // So we can see a long request in the dashboard with a spinning progress wheel
            Thread.Sleep(2000);

            var currentUser = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
            return currentUser?.Name ?? "I have no idea who you are";
        }

        [HttpGet("whoAmI")]
        [ProducesResponseType<IUser>(StatusCodes.Status200OK)]
        public IUser? WhoAmI() => _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
    }
}
