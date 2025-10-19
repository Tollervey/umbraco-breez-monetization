using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tollervey.LightningPayments.Breez.Configuration;
using Tollervey.LightningPayments.Breez.Services;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Composing;
using Tollervey.Umbraco.LightningPayments.Core.Composers;

namespace Tollervey.Umbraco.LightningPayments.CoreTests;

[TestClass]
public class LightningPaymentsComposerTests
{
    [TestMethod]
    public void Compose_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{LightningPaymentsSettings.SectionName}:ConnectionString", "Data Source=:memory:" },
                { $"{LightningPaymentsSettings.SectionName}:BreezApiKey", "test" },
                { $"{LightningPaymentsSettings.SectionName}:Mnemonic", "test" },
                { $"{LightningPaymentsSettings.SectionName}:Network", "Testnet" }
                // Add other necessary settings if required
            })
            .Build();
        var typeLoaderMock = new Mock<TypeLoader>();
        var builder = new UmbracoBuilder(services, config, typeLoaderMock.Object);

        var composer = new LightningPaymentsComposer();

        // Act
        composer.Compose(builder);

        // Assert
        var provider = services.BuildServiceProvider();
        Assert.IsNotNull(provider.GetService<IBreezSdkService>());
        Assert.IsNotNull(provider.GetService<IPaymentStateService>());
        Assert.IsNotNull(provider.GetService<IEmailService>());
        // Add assertions for other registered services and middleware if needed
    }
}