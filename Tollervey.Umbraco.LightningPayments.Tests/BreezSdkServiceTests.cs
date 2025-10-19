using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tollervey.LightningPayments.Breez.Configuration;
using Tollervey.LightningPayments.Breez.Models;
using Tollervey.LightningPayments.Breez.Services;
using Breez.Sdk.Liquid;
using System.Threading.Tasks;
using System;
using System.IO;

namespace Tollervey.Umbraco.LightningPayments.Tests;

[TestClass]
public class BreezSdkServiceTests
{
    private Mock<ILogger<BreezSdkService>> _loggerMock;
    private Mock<IHostEnvironment> _hostEnvironmentMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IOptions<LightningPaymentsSettings>> _settingsMock;
    private Mock<IBreezSdkWrapper> _wrapperMock;
    private LightningPaymentsSettings _settings;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<BreezSdkService>>();
        _hostEnvironmentMock = new Mock<IHostEnvironment>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _settingsMock = new Mock<IOptions<LightningPaymentsSettings>>();
        _wrapperMock = new Mock<IBreezSdkWrapper>();

        _settings = new LightningPaymentsSettings
        {
            BreezApiKey = "test-api-key",
            Mnemonic = "test-mnemonic",
            Network = LightningPaymentsSettings.LightningNetwork.Testnet,
            WebhookUrl = "https://test-webhook.com"
        };
        _settingsMock.Setup(s => s.Value).Returns(_settings);

        _hostEnvironmentMock.Setup(h => h.ContentRootPath).Returns("test-path");

        // Setup service provider to return logger when requested
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<BreezSdkService>))).Returns(_loggerMock.Object);
    }

    [TestMethod]
    public void Constructor_InitializesLazySdk()
    {
        // Act
        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object, _wrapperMock.Object);

        // Assert
        Assert.IsNotNull(service);
    }

    [TestMethod]
    public async Task InitializeSdkAsync_WithInvalidCredentials_ReturnsNull()
    {
        var invalidSettings = new LightningPaymentsSettings
        {
            BreezApiKey = "",
            Mnemonic = ""
        };
        _settingsMock.Setup(s => s.Value).Returns(invalidSettings);

        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object, _wrapperMock.Object);

        // Use reflection to call private method
        var method = typeof(BreezSdkService).GetMethod("InitializeSdkAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<BindingLiquidSdk?>)method.Invoke(service, null);
        var result = await task;

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task InitializeSdkAsync_WithValidCredentials_ConnectsAndRegistersWebhook()
    {
        var config = BreezSdkLiquidMethods.DefaultConfig(LiquidNetwork.Testnet, "test-api-key");
        _wrapperMock.Setup(w => w.DefaultConfig(LiquidNetwork.Testnet, "test-api-key")).Returns(config);

        var sdkMock = new Mock<BindingLiquidSdk>();
        _wrapperMock.Setup(w => w.Connect(It.IsAny<ConnectRequest>())).Returns(sdkMock.Object);

        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object, _wrapperMock.Object);

        var method = typeof(BreezSdkService).GetMethod("InitializeSdkAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<BindingLiquidSdk?>)method.Invoke(service, null);
        var result = await task;

        Assert.IsNotNull(result);
        _wrapperMock.Verify(w => w.SetLogger(It.IsAny<Logger>()), Times.Once);
        _wrapperMock.Verify(w => w.Connect(It.IsAny<ConnectRequest>()), Times.Once);
        _wrapperMock.Verify(w => w.AddEventListener(sdkMock.Object, It.IsAny<EventListener>()), Times.Once);
        _wrapperMock.Verify(w => w.RegisterWebhook(sdkMock.Object, "https://test-webhook.com"), Times.Once);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task CreateInvoiceAsync_ThrowsWhenSdkNotConnected()
    {
        var invalidSettings = new LightningPaymentsSettings
        {
            BreezApiKey = "",
            Mnemonic = ""
        };
        _settingsMock.Setup(s => s.Value).Returns(invalidSettings);

        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object, _wrapperMock.Object);

        await service.CreateInvoiceAsync(100, "test");
    }

    [TestMethod]
    public async Task CreateInvoiceAsync_CreatesInvoiceSuccessfully()
    {
        var sdkMock = new Mock<BindingLiquidSdk>();
        SetupSdkInitialization(sdkMock.Object);

        var payerAmount = new ReceiveAmount.Bitcoin(1000);
        var prepareResponse = new PrepareReceiveResponse(PaymentMethod.Bolt11Invoice, 100, payerAmount, 1000, 10000, 0.1);
        _wrapperMock.Setup(w => w.PrepareReceivePayment(sdkMock.Object, It.IsAny<PrepareReceiveRequest>())).Returns(prepareResponse);

        var receiveResponse = new ReceivePaymentResponse("bolt11-invoice", null, null);
        _wrapperMock.Setup(w => w.ReceivePayment(sdkMock.Object, It.IsAny<ReceivePaymentRequest>())).Returns(receiveResponse);

        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object, _wrapperMock.Object);

        // Trigger initialization
        var initMethod = typeof(BreezSdkService).GetMethod("InitializeSdkAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)initMethod.Invoke(service, null);

        var result = await service.CreateInvoiceAsync(1000, "test description");

        Assert.AreEqual("bolt11-invoice", result);
    }

    [TestMethod]
    [ExpectedException(typeof(InvoiceException))]
    public async Task CreateInvoiceAsync_ThrowsOnSdkFailure()
    {
        var sdkMock = new Mock<BindingLiquidSdk>();
        SetupSdkInitialization(sdkMock.Object);

        _wrapperMock.Setup(w => w.PrepareReceivePayment(sdkMock.Object, It.IsAny<PrepareReceiveRequest>())).Throws(new Exception("SDK error"));

        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object, _wrapperMock.Object);

        // Trigger initialization
        var initMethod = typeof(BreezSdkService).GetMethod("InitializeSdkAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)initMethod.Invoke(service, null);

        await service.CreateInvoiceAsync(1000, "test");
    }

    [TestMethod]
    public async Task CreateBolt12OfferAsync_CreatesOfferSuccessfully()
    {
        var sdkMock = new Mock<BindingLiquidSdk>();
        SetupSdkInitialization(sdkMock.Object);

        var payerAmount = new ReceiveAmount.Bitcoin(1000);
        var prepareResponse = new PrepareReceiveResponse(PaymentMethod.Bolt12Offer, 100, payerAmount, 1000, 10000, 0.1);
        _wrapperMock.Setup(w => w.PrepareReceivePayment(sdkMock.Object, It.IsAny<PrepareReceiveRequest>())).Returns(prepareResponse);

        var receiveResponse = new ReceivePaymentResponse("bolt12-offer", null, null);
        _wrapperMock.Setup(w => w.ReceivePayment(sdkMock.Object, It.IsAny<ReceivePaymentRequest>())).Returns(receiveResponse);

        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object, _wrapperMock.Object);

        // Trigger initialization
        var initMethod = typeof(BreezSdkService).GetMethod("InitializeSdkAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)initMethod.Invoke(service, null);

        var result = await service.CreateBolt12OfferAsync(1000, "test description");

        Assert.AreEqual("bolt12-offer", result);
    }

    [TestMethod]
    public async Task DisposeAsync_DisconnectsSdkIfInitialized()
    {
        var sdkMock = new Mock<BindingLiquidSdk>();
        SetupSdkInitialization(sdkMock.Object);

        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object, _wrapperMock.Object);

        // Trigger initialization by calling a method
        await service.CreateInvoiceAsync(100, "test");

        await service.DisposeAsync();

        _wrapperMock.Verify(w => w.Disconnect(sdkMock.Object), Times.Once);
    }

    [TestMethod]
    public async Task InitializeSdkAsync_HandlesException()
    {
        _wrapperMock.Setup(w => w.Connect(It.IsAny<ConnectRequest>())).Throws(new Exception("Connect failed"));

        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object, _wrapperMock.Object);

        var method = typeof(BreezSdkService).GetMethod("InitializeSdkAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<BindingLiquidSdk?>)method.Invoke(service, null);
        var result = await task;

        Assert.IsNull(result);
        _loggerMock.Verify(l => l.LogError(It.IsAny<Exception>(), "Failed to connect to Breez SDK."), Times.Once);
    }

    [TestMethod]
    public async Task InitializeSdkAsync_WithoutWebhookUrl_DoesNotRegisterWebhook()
    {
        _settings = new LightningPaymentsSettings
        {
            BreezApiKey = "test-api-key",
            Mnemonic = "test-mnemonic",
            Network = LightningPaymentsSettings.LightningNetwork.Testnet,
            WebhookUrl = ""
        };
        _settingsMock.Setup(s => s.Value).Returns(_settings);

        var sdkMock = new Mock<BindingLiquidSdk>();
        SetupSdkInitialization(sdkMock.Object);

        var service = new BreezSdkService(_settingsMock.Object, _hostEnvironmentMock.Object, _serviceProviderMock.Object, _loggerMock.Object, _wrapperMock.Object);

        var method = typeof(BreezSdkService).GetMethod("InitializeSdkAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<BindingLiquidSdk?>)method.Invoke(service, null);
        await task;

        _wrapperMock.Verify(w => w.RegisterWebhook(It.IsAny<BindingLiquidSdk>(), It.IsAny<string>()), Times.Never);
    }

    private void SetupSdkInitialization(BindingLiquidSdk sdk)
    {
        var config = BreezSdkLiquidMethods.DefaultConfig(LiquidNetwork.Testnet, "test-api-key");
        _wrapperMock.Setup(w => w.DefaultConfig(LiquidNetwork.Testnet, "test-api-key")).Returns(config);
        _wrapperMock.Setup(w => w.Connect(It.IsAny<ConnectRequest>())).Returns(sdk);
    }
}