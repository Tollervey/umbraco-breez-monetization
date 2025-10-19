using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tollervey.LightningPayments.Breez.Services;
using Tollervey.Umbraco.LightningPayments.Core.Controllers;
using Tollervey.Umbraco.LightningPayments.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache; // Added for IPublishedContentCache
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

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
    }

    [TestMethod]
    public async Task GetPaywallInvoice_InvalidContentId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetPaywallInvoice(0);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task GetPaywallInvoice_ContentNotFound_ReturnsNotFound()
    {
        // Arrange
        var umbracoContextMock = new Mock<IUmbracoContext>();
        var contentCacheMock = new Mock<IPublishedContentCache>();
        contentCacheMock.Setup(c => c.GetById(It.IsAny<int>())).Returns((IPublishedContent?)null);
        umbracoContextMock.Setup(c => c.Content).Returns(contentCacheMock.Object);
        IUmbracoContext outContext = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(a => a.TryGetUmbracoContext(out outContext)).Returns(true);

        // Act
        var result = await _controller.GetPaywallInvoice(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task GetPaywallInvoice_PaywallDisabled_ReturnsBadRequest()
    {
        // Arrange
        var umbracoContextMock = new Mock<IUmbracoContext>();
        var contentCacheMock = new Mock<IPublishedContentCache>();
        var contentMock = new Mock<IPublishedContent>();
        contentMock.Setup(c => c.HasValue("breezPaywall")).Returns(true);
        contentMock.Setup(c => c.Value<string>("breezPaywall")).Returns("{\"enabled\": false, \"fee\": 100}");
        contentCacheMock.Setup(c => c.GetById(1)).Returns(contentMock.Object);
        umbracoContextMock.Setup(c => c.Content).Returns(contentCacheMock.Object);
        IUmbracoContext outContext = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(a => a.TryGetUmbracoContext(out outContext)).Returns(true);

        // Act
        var result = await _controller.GetPaywallInvoice(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task GetPaywallInvoice_ValidRequest_ReturnsOkWithInvoice()
    {
        // Arrange
        var umbracoContextMock = new Mock<IUmbracoContext>();
        var contentCacheMock = new Mock<IPublishedContentCache>();
        var contentMock = new Mock<IPublishedContent>();
        contentMock.Setup(c => c.HasValue("breezPaywall")).Returns(true);
        contentMock.Setup(c => c.Value<string>("breezPaywall")).Returns("{\"enabled\": true, \"fee\": 100}");
        contentCacheMock.Setup(c => c.GetById(1)).Returns(contentMock.Object);
        umbracoContextMock.Setup(c => c.Content).Returns(contentCacheMock.Object);
        IUmbracoContext outContext = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(a => a.TryGetUmbracoContext(out outContext)).Returns(true);

        _breezSdkServiceMock.Setup(s => s.CreateInvoiceAsync(100, It.IsAny<string>())).ReturnsAsync("test-invoice");
        _paymentStateServiceMock.Setup(p => p.AddPendingPaymentAsync(It.IsAny<string>(), 1, It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetPaywallInvoice(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }
}
