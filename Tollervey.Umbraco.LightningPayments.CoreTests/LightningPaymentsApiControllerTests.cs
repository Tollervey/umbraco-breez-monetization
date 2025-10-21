using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Security.Principal;
using Tollervey.LightningPayments.Breez.Services;
using Tollervey.Umbraco.LightningPayments.UI.Controllers;
using Tollervey.Umbraco.LightningPayments.UI.Models;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Tollervey.Umbraco.LightningPayments.CoreTests;

[TestClass]
public class LightningPaymentsApiControllerTests
{
    private Mock<IBreezSdkService> _breezSdkServiceMock;
    private Mock<IPaymentStateService> _paymentStateServiceMock;
    private Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private Mock<ILogger<LightningPaymentsApiController>> _loggerMock;
    private Mock<IUserService> _userServiceMock;
    private LightningPaymentsApiController _controller;

    [TestInitialize]
    public void Setup()
    {
        _breezSdkServiceMock = new Mock<IBreezSdkService>();
        _paymentStateServiceMock = new Mock<IPaymentStateService>();
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _loggerMock = new Mock<ILogger<LightningPaymentsApiController>>();
        _userServiceMock = new Mock<IUserService>();

        _controller = new LightningPaymentsApiController(
            _breezSdkServiceMock.Object,
            _paymentStateServiceMock.Object,
            _umbracoContextAccessorMock.Object,
            _loggerMock.Object,
            _userServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [TestMethod]
    public async Task GetPaymentStatus_NoSession_ReturnsUnauthorized()
    {
        // Act
        var result = await _controller.GetPaymentStatus(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task GetPaymentStatus_WithSession_ReturnsOk()
    {
        // Arrange
        var sessionId = "test-session";
        _controller.Request.Headers.Cookie = $"LightningPaymentsSession={sessionId}";
        _paymentStateServiceMock.Setup(s => s.GetPaymentStateAsync(sessionId, 1))
            .ReturnsAsync(new PaymentState { Status = PaymentStatus.Paid });

        // Act
        var result = await _controller.GetPaymentStatus(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task GetAllPayments_NotAdmin_ReturnsUnauthorized()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "1"));
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = principal;

        var userMock = new Mock<IUser>();
        var userGroup = new Mock<IReadOnlyUserGroup>();
        userGroup.Setup(g => g.Name).Returns("Editors");
        userMock.Setup(u => u.Groups).Returns(new[] { userGroup.Object });
        _userServiceMock.Setup(s => s.GetUserById(1)).Returns(userMock.Object);

        // Act
        var result = await _controller.GetAllPayments();

        // Assert
        Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task GetAllPayments_Admin_ReturnsOk()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "1"));
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = principal;

        var userMock = new Mock<IUser>();
        var userGroup = new Mock<IReadOnlyUserGroup>();
        userGroup.Setup(g => g.Name).Returns("Administrators");
        userMock.Setup(u => u.Groups).Returns(new[] { userGroup.Object });
        _userServiceMock.Setup(s => s.GetUserById(1)).Returns(userMock.Object);

        _paymentStateServiceMock.Setup(s => s.GetAllPaymentsAsync()).ReturnsAsync(new List<PaymentState>());

        // Act
        var result = await _controller.GetAllPayments();

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }
}
