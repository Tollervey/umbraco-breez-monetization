using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tollervey.Umbraco.LightningPayments.Core.Controllers;
using Umbraco.Cms.Core.Web;

namespace Tollervey.Umbraco.LightningPayments.CoreTests;

[TestClass]
public class WellKnownControllerTests
{
    private Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private Mock<ILogger<WellKnownController>> _loggerMock;
    private WellKnownController _controller;

    [TestInitialize]
    public void Setup()
    {
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _loggerMock = new Mock<ILogger<WellKnownController>>();

        _controller = new WellKnownController(
            _umbracoContextAccessorMock.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public void GetLightningAddress_ReturnsOkResult()
    {
        // Arrange
        var request = new Mock<HttpRequest>();
        request.Setup(x => x.Scheme).Returns("https");
        request.Setup(x => x.Host).Returns(new HostString("example.com"));
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Request).Returns(request.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };

        // Act
        var result = _controller.GetLightningAddress("testuser", 1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }
}
