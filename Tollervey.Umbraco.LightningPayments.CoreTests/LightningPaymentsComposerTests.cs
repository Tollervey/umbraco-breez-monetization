using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Tollervey.Umbraco.LightningPayments.CoreTests;

[TestClass]
public class LightningPaymentsComposerTests
{
    private Mock<IUmbracoBuilder> _builderMock;
    private ServiceCollection _services;
    private Mock<IConfiguration> _configMock;

    [TestInitialize]
    public void Setup()
    {
        _services = new ServiceCollection();
        _configMock = new Mock<IConfiguration>();
        _builderMock = new Mock<IUmbracoBuilder>();
        _builderMock.Setup(b => b.Services).Returns(_services);
        _builderMock.Setup(b => b.Config).Returns(_configMock.Object);
    }

    [TestMethod]
    public void Compose_WithConnectionString_RegistersPersistentPaymentStateService()
    {
        // Arrange
        var composer = new LightningPaymentsComposer();
        var configSectionMock = new Mock<IConfigurationSection>();
        configSectionMock.Setup(s => s.Value).Returns("DataSource=:memory:");
        _configMock.Setup(c => c.GetSection("ConnectionStrings:Tollervey.LightningPayments")).Returns(configSectionMock.Object);

        // Act
        composer.Compose(_builderMock.Object);

        // Assert
        var provider = _services.BuildServiceProvider();
        var paymentStateService = provider.GetService<IPaymentStateService>();
        Assert.IsNotNull(paymentStateService);
        Assert.IsInstanceOfType(paymentStateService, typeof(PersistentPaymentStateService));
    }

    [TestMethod]
    public void Compose_WithoutConnectionString_RegistersInMemoryPaymentStateService()
    {
        // Arrange
        var composer = new LightningPaymentsComposer();
        var configSectionMock = new Mock<IConfigurationSection>();
        configSectionMock.Setup(s => s.Value).Returns((string)null);
        _configMock.Setup(c => c.GetSection("ConnectionStrings:Tollervey.LightningPayments")).Returns(configSectionMock.Object);

        // Act
        composer.Compose(_builderMock.Object);

        // Assert
        var provider = _services.BuildServiceProvider();
        var paymentStateService = provider.GetService<IPaymentStateService>();
        Assert.IsNotNull(paymentStateService);
        Assert.IsInstanceOfType(paymentStateService, typeof(InMemoryPaymentStateService));
    }
}