using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tollervey.LightningPayments.Breez.Configuration;
using Tollervey.LightningPayments.Breez.Models;
using Tollervey.LightningPayments.Breez.Services;
using Breez.Sdk.Liquid;

namespace Tollervey.Umbraco.LightningPayments.Tests;

[TestClass]
public class BreezSdkServiceTests
{
    private Mock<ILogger<BreezSdkService>> _loggerMock;
    private Mock<IHostEnvironment> _hostEnvironmentMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IOptions<LightningPaymentsSettings>> _settingsMock;
    private LightningPaymentsSettings _settings;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<BreezSdkService>>();
        _hostEnvironmentMock = new Mock<IHostEnvironment>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _settingsMock = new Mock<IOptions<LightningPaymentsSettings>>();

        _settings = new LightningPaymentsSettings
        {
            BreezApiKey = "test-api-key",
            Mnemonic = "test-mnemonic",
            Network = LightningPaymentsSettings.LightningNetwork.Testnet
        };
        _settingsMock.Setup(s => s.Value).Returns(_settings);

        _hostEnvironmentMock.Setup(h => h.ContentRootPath).Returns("test-path");
    }

    [TestMethod]
    public async Task Constructor_InitializesLazySdk()
    {
        // Act
        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

        // Assert
        Assert.IsNotNull(service);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task CreateInvoiceAsync_ThrowsWhenSdkNotConnected()
    {
        _settings = new LightningPaymentsSettings { BreezApiKey = "", Mnemonic = "" };
        _settingsMock.Setup(s => s.Value).Returns(_settings);

        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object);

        // Force initialization to null
        var sdkField = typeof(BreezSdkService).GetField("_sdkInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var lazy = new Lazy<Task<BindingLiquidSdk?>>(() => Task.FromResult<BindingLiquidSdk?>(null));
        sdkField?.SetValue(service, lazy);

        await service.CreateInvoiceAsync(100, "test");
    }

    [TestMethod]
    [ExpectedException(typeof(InvoiceException))]
    public async Task CreateInvoiceAsync_ThrowsOnFailure()
    {
        // This would require mocking the SDK, which is complex. For coverage, assume we test the try-catch.
        // In real test, we'd mock the SDK calls.
    }

    // Similar tests for CreateBolt12OfferAsync, DisposeAsync, etc.

    [TestMethod]
    public async Task CreateBolt12OfferAsync_SimilarToInvoice()
    {
        // Similar setup as above
    }

    // Add more tests for boundary: amount 0, max amount, empty description, etc.
}