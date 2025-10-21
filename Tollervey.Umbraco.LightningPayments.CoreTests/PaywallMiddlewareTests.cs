using Microsoft.AspNetCore.Http;
using Moq;
using System.Text.Json;
using Tollervey.Umbraco.LightningPayments.UI.Middleware;
using Tollervey.Umbraco.LightningPayments.UI.Models;
using Tollervey.Umbraco.LightningPayments.UI.Services;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Tollervey.Umbraco.LightningPayments.CoreTests;

[TestClass]
public class PaywallMiddlewareTests
{
    private Mock<RequestDelegate> _nextMock;
    private Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private Mock<IPaymentStateService> _paymentStateServiceMock;
    private DefaultHttpContext _httpContext;
    private PaywallMiddleware _middleware;

    [TestInitialize]
    public void Setup()
    {
        _nextMock = new Mock<RequestDelegate>();
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _paymentStateServiceMock = new Mock<IPaymentStateService>();
        _httpContext = new DefaultHttpContext();
        _middleware = new PaywallMiddleware(_nextMock.Object);
    }

    private void SetupUmbracoContext(IPublishedContent? content)
    {
        var umbracoContextMock = new Mock<IUmbracoContext>();
        var publishedRequestMock = new Mock<IPublishedRequest>();
        publishedRequestMock.Setup(r => r.PublishedContent).Returns(content);
        umbracoContextMock.Setup(c => c.PublishedRequest).Returns(publishedRequestMock.Object);
        IUmbracoContext? outContext = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(a => a.TryGetUmbracoContext(out outContext)).Returns(true);
    }

    [TestMethod]
    public async Task InvokeAsync_NoUmbracoContext_CallsNext()
    {
        // Arrange
        IUmbracoContext? outContext = null;
        _umbracoContextAccessorMock.Setup(a => a.TryGetUmbracoContext(out outContext)).Returns(false);

        // Act
        await _middleware.InvokeAsync(_httpContext, _umbracoContextAccessorMock.Object, _paymentStateServiceMock.Object);

        // Assert
        _nextMock.Verify(next => next(_httpContext), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_NoPublishedContent_CallsNext()
    {
        // Arrange
        SetupUmbracoContext(null);

        // Act
        await _middleware.InvokeAsync(_httpContext, _umbracoContextAccessorMock.Object, _paymentStateServiceMock.Object);

        // Assert
        _nextMock.Verify(next => next(_httpContext), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_ContentWithoutPaywallProperty_CallsNext()
    {
        // Arrange
        var contentMock = new Mock<IPublishedContent>();
        var propertyMock = new Mock<IPublishedProperty>();
        propertyMock.Setup(p => p.Alias).Returns("otherProperty");
        contentMock.Setup(c => c.Properties).Returns(new[] { propertyMock.Object });
        SetupUmbracoContext(contentMock.Object);

        // Act
        await _middleware.InvokeAsync(_httpContext, _umbracoContextAccessorMock.Object, _paymentStateServiceMock.Object);

        // Assert
        _nextMock.Verify(next => next(_httpContext), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_PaywallDisabled_CallsNext()
    {
        // Arrange
        var contentMock = new Mock<IPublishedContent>();
        var paywallConfig = new PaywallConfig { Enabled = false };
        var propertyMock = new Mock<IPublishedProperty>();
        propertyMock.Setup(p => p.Alias).Returns("breezPaywall");
        propertyMock.Setup(p => p.HasValue(null, null)).Returns(true);
        propertyMock.Setup(p => p.GetValue(null, null)).Returns(JsonSerializer.Serialize(paywallConfig));
        contentMock.Setup(c => c.GetProperty("breezPaywall")).Returns(propertyMock.Object);
        SetupUmbracoContext(contentMock.Object);

        // Act
        await _middleware.InvokeAsync(_httpContext, _umbracoContextAccessorMock.Object, _paymentStateServiceMock.Object);

        // Assert
        _nextMock.Verify(next => next(_httpContext), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_AccessGranted_CallsNext()
    {
        // Arrange
        var contentMock = new Mock<IPublishedContent>();
        contentMock.Setup(c => c.Id).Returns(1);
        var paywallConfig = new PaywallConfig { Enabled = true };
        var propertyMock = new Mock<IPublishedProperty>();
        propertyMock.Setup(p => p.Alias).Returns("breezPaywall");
        propertyMock.Setup(p => p.HasValue(null, null)).Returns(true);
        propertyMock.Setup(p => p.GetValue(null, null)).Returns(JsonSerializer.Serialize(paywallConfig));
        contentMock.Setup(c => c.GetProperty("breezPaywall")).Returns(propertyMock.Object);
        SetupUmbracoContext(contentMock.Object);

        var sessionId = "test-session";
        _httpContext.Request.Headers.Cookie = $"{PaywallMiddleware.PaywallCookieName}={sessionId}";
        _paymentStateServiceMock.Setup(s => s.GetPaymentStateAsync(sessionId, 1)).ReturnsAsync(new PaymentState { Status = PaymentStatus.Paid });

        // Act
        await _middleware.InvokeAsync(_httpContext, _umbracoContextAccessorMock.Object, _paymentStateServiceMock.Object);

        // Assert
        _nextMock.Verify(next => next(_httpContext), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_AccessDenied_NoSession_Redirects()
    {
        // Arrange
        var contentMock = new Mock<IPublishedContent>();
        contentMock.Setup(c => c.Id).Returns(1);
        var paywallConfig = new PaywallConfig { Enabled = true };
        var propertyMock = new Mock<IPublishedProperty>();
        propertyMock.Setup(p => p.Alias).Returns("breezPaywall");
        propertyMock.Setup(p => p.HasValue(null, null)).Returns(true);
        propertyMock.Setup(p => p.GetValue(null, null)).Returns(JsonSerializer.Serialize(paywallConfig));
        contentMock.Setup(c => c.GetProperty("breezPaywall")).Returns(propertyMock.Object);
        SetupUmbracoContext(contentMock.Object);

        // Act
        await _middleware.InvokeAsync(_httpContext, _umbracoContextAccessorMock.Object, _paymentStateServiceMock.Object);

        // Assert
        _nextMock.Verify(next => next(_httpContext), Times.Never);
        Assert.AreEqual(302, _httpContext.Response.StatusCode);
        Assert.AreEqual("/umbraco/surface/PaywallSurface/Index?contentId=1", _httpContext.Response.Headers.Location.ToString());
    }

    [TestMethod]
    public async Task InvokeAsync_AccessDenied_PaymentNotPaid_Redirects()
    {
        // Arrange
        var contentMock = new Mock<IPublishedContent>();
        contentMock.Setup(c => c.Id).Returns(1);
        var paywallConfig = new PaywallConfig { Enabled = true };
        var propertyMock = new Mock<IPublishedProperty>();
        propertyMock.Setup(p => p.Alias).Returns("breezPaywall");
        propertyMock.Setup(p => p.HasValue(null, null)).Returns(true);
        propertyMock.Setup(p => p.GetValue(null, null)).Returns(JsonSerializer.Serialize(paywallConfig));
        contentMock.Setup(c => c.GetProperty("breezPaywall")).Returns(propertyMock.Object);
        SetupUmbracoContext(contentMock.Object);

        var sessionId = "test-session";
        _httpContext.Request.Headers.Cookie = $"{PaywallMiddleware.PaywallCookieName}={sessionId}";
        _paymentStateServiceMock.Setup(s => s.GetPaymentStateAsync(sessionId, 1)).ReturnsAsync(new PaymentState { Status = PaymentStatus.Pending });

        // Act
        await _middleware.InvokeAsync(_httpContext, _umbracoContextAccessorMock.Object, _paymentStateServiceMock.Object);

        // Assert
        _nextMock.Verify(next => next(_httpContext), Times.Never);
        Assert.AreEqual(302, _httpContext.Response.StatusCode);
        Assert.AreEqual("/umbraco/surface/PaywallSurface/Index?contentId=1", _httpContext.Response.Headers.Location.ToString());
    }
}
