using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Tollervey.LightningPayments.Breez.Models;
using Tollervey.Umbraco.LightningPayments.Core.Controllers;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Extensions;

namespace Tollervey.Umbraco.LightningPayments.CoreTests;

[TestClass]
public class PaywallSurfaceControllerTests
{
    private Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private Mock<IUmbracoDatabaseFactory> _databaseFactoryMock;
    private Mock<ServiceContext> _serviceContextMock;
    private Mock<AppCaches> _appCachesMock;
    private Mock<IProfilingLogger> _profilingLoggerMock;
    private Mock<IPublishedUrlProvider> _publishedUrlProviderMock;
    private PaywallSurfaceController _controller;

    [TestInitialize]
    public void Setup()
    {
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _databaseFactoryMock = new Mock<IUmbracoDatabaseFactory>();
        _serviceContextMock = new Mock<ServiceContext>();
        _appCachesMock = new Mock<AppCaches>(
            Mock.Of<IRequestCache>(),
            Mock.Of<DistributedCache>(),
            Mock.Of<ILogger<AppCaches>>(),
            Mock.Of<IAppPolicyCache>(),
            Mock.Of<IServerRoleAccessor>());
        _profilingLoggerMock = new Mock<IProfilingLogger>();
        _publishedUrlProviderMock = new Mock<IPublishedUrlProvider>();

        _controller = new PaywallSurfaceController(
            _umbracoContextAccessorMock.Object,
            _databaseFactoryMock.Object,
            _serviceContextMock.Object,
            _appCachesMock.Object,
            _profilingLoggerMock.Object,
            _publishedUrlProviderMock.Object);
    }

    private void SetupUmbracoContext(IPublishedContent? content)
    {
        var umbracoContextMock = new Mock<IUmbracoContext>();
        var contentCacheMock = new Mock<IPublishedContentCache>();
        contentCacheMock.Setup(c => c.GetById(It.IsAny<int>())).Returns(content);
        umbracoContextMock.Setup(c => c.Content).Returns(contentCacheMock.Object);
        IUmbracoContext? outContext = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(a => a.TryGetUmbracoContext(out outContext)).Returns(true);
    }

    [TestMethod]
    public void Index_InvalidContentId_ReturnsNotFoundResult()
    {
        // Act
        var result = _controller.Index(0);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public void Index_NoUmbracoContext_ReturnsNotFoundResult()
    {
        // Arrange
        IUmbracoContext? umbracoContext = null;
        _umbracoContextAccessorMock.Setup(x => x.TryGetUmbracoContext(out umbracoContext)).Returns(false);

        // Act
        var result = _controller.Index(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public void Index_ContentNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        SetupUmbracoContext(null);

        // Act
        var result = _controller.Index(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public void Index_ContentWithoutPaywallProperty_ReturnsNotFoundResult()
    {
        // Arrange
        var contentMock = new Mock<IPublishedContent>();
        contentMock.Setup(c => c.GetProperty("breezPaywall")).Returns((IPublishedProperty)null);
        SetupUmbracoContext(contentMock.Object);

        // Act
        var result = _controller.Index(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public void Index_PaywallDisabled_ReturnsNotFoundResult()
    {
        // Arrange
        var contentMock = new Mock<IPublishedContent>();
        var paywallConfig = new PaywallConfig { Enabled = false };
        var propertyMock = new Mock<IPublishedProperty>();
        propertyMock.Setup(p => p.HasValue(null, null)).Returns(true);
        propertyMock.Setup(p => p.GetValue(null, null)).Returns(JsonSerializer.Serialize(paywallConfig));
        contentMock.Setup(c => c.GetProperty("breezPaywall")).Returns(propertyMock.Object);
        SetupUmbracoContext(contentMock.Object);

        // Act
        var result = _controller.Index(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public void Index_ValidRequest_ReturnsViewResult()
    {
        // Arrange
        var contentMock = new Mock<IPublishedContent>();
        var paywallConfig = new PaywallConfig { Enabled = true, Fee = 100 };
        var paywallPropertyMock = new Mock<IPublishedProperty>();
        paywallPropertyMock.Setup(p => p.HasValue(null, null)).Returns(true);
        paywallPropertyMock.Setup(p => p.GetValue(null, null)).Returns(JsonSerializer.Serialize(paywallConfig));
        contentMock.Setup(c => c.GetProperty("breezPaywall")).Returns(paywallPropertyMock.Object);

        var previewPropertyMock = new Mock<IPublishedProperty>();
        previewPropertyMock.Setup(p => p.HasValue(null, null)).Returns(true);
        previewPropertyMock.Setup(p => p.GetValue(null, null)).Returns("Test Preview");
        contentMock.Setup(c => c.GetProperty("breezPaywallPreview")).Returns(previewPropertyMock.Object);

        SetupUmbracoContext(contentMock.Object);

        // Act
        var result = _controller.Index(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ViewResult));
        var viewResult = (ViewResult)result;
        Assert.AreEqual("Index", viewResult.ViewName);
        var model = viewResult.Model as PaywallViewModel;
        Assert.IsNotNull(model);
        Assert.AreEqual(0, model.ContentId);
        Assert.AreEqual("Test Preview", model.PreviewContent);
        Assert.AreEqual((ulong)100, model.Fee);
    }
}
