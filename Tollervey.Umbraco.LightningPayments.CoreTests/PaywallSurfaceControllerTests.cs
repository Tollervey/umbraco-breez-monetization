using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Tollervey.Umbraco.LightningPayments.Core.Controllers;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;

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
            Mock.Of<IDistributedCache>(),
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
}
