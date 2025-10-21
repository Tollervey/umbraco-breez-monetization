using Breez.Sdk.Liquid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Tollervey.Umbraco.LightningPayments.UI.Services;

namespace Tollervey.Umbraco.LightningPayments.Tests;

[TestClass]
public class BreezEventProcessorTests
{
    private Mock<ILogger<BreezEventProcessor>> _loggerMock;
    private Mock<IServiceScopeFactory> _scopeFactoryMock;
    private Mock<IServiceScope> _scopeMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IPaymentEventDeduper> _deduperMock;
    private Mock<IPaymentStateService> _paymentServiceMock;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<BreezEventProcessor>>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _deduperMock = new Mock<IPaymentEventDeduper>();
        _paymentServiceMock = new Mock<IPaymentStateService>();

        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(p => p.GetService(typeof(IPaymentEventDeduper))).Returns(_deduperMock.Object);
        _serviceProviderMock.Setup(p => p.GetService(typeof(IPaymentStateService))).Returns(_paymentServiceMock.Object);
    }

    private SdkEvent.PaymentSucceeded CreatePaymentSucceededEvent(string paymentHash)
    {
        // The SdkEvent.PaymentSucceeded constructor is internal. We need to use reflection to create it.
        // We also need to create a `Payment` object to pass to its constructor.

        // 1. Create an instance of PaymentDetails
        var paymentDetailsConstructor = typeof(PaymentDetails).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        var constructorParams = paymentDetailsConstructor.GetParameters();
        var paymentDetails = paymentDetailsConstructor.Invoke(new object[constructorParams.Length]);


        // 2. Create the Payment object
        var paymentConstructor = typeof(Payment).GetConstructors(BindingFlags.Public | BindingFlags.Instance).First();
        var payment = paymentConstructor.Invoke(new object[] {
            "payment_id",
            PaymentType.Receive,
            123456789,
            1000,
            100,
            true,
            PaymentState.Complete,
            null,
            paymentDetails,
            null,
            null,
            null,
            null,
            null,
            null
        });

        // 3. Set the payment hash on the internal details object
        var detailsProp = payment.GetType().GetProperty("details");
        var detailsObj = detailsProp.GetValue(payment);
        var receivedProp = detailsObj.GetType().GetProperty("received");
        var receivedObj = receivedProp.GetValue(detailsObj);
        var paymentHashProp = receivedObj.GetType().GetProperty("paymentHash");
        paymentHashProp.SetValue(receivedObj, paymentHash);


        // 4. Create the SdkEvent.PaymentSucceeded event
        var succeededConstructor = typeof(SdkEvent.PaymentSucceeded).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
        return (SdkEvent.PaymentSucceeded)succeededConstructor.Invoke(new object[] { payment });
    }

    [TestMethod]
    public async Task BreezEventProcessor_ProcessesEvent_HappyPath()
    {
        // Arrange
        var processor = new BreezEventProcessor(_loggerMock.Object, _scopeFactoryMock.Object);
        var paymentHash = "test_hash";
        var paymentSucceededEvent = CreatePaymentSucceededEvent(paymentHash);

        _deduperMock.Setup(d => d.TryBegin(paymentHash)).Returns(true);

        // Act
        await processor.StartAsync(CancellationToken.None);
        await processor.EnqueueEvent(paymentSucceededEvent);
        await Task.Delay(100); // Give consumer time to process
        await processor.StopAsync(CancellationToken.None);

        // Assert
        _deduperMock.Verify(d => d.TryBegin(paymentHash), Times.Once);
        _paymentServiceMock.Verify(p => p.ConfirmPaymentAsync(paymentHash), Times.Once);
        _deduperMock.Verify(d => d.Complete(paymentHash), Times.Once);
    }

    [TestMethod]
    public async Task BreezEventProcessor_HandlesDuplicateEvent()
    {
        // Arrange
        var processor = new BreezEventProcessor(_loggerMock.Object, _scopeFactoryMock.Object);
        var paymentHash = "duplicate_hash";
        var paymentSucceededEvent = CreatePaymentSucceededEvent(paymentHash);

        _deduperMock.Setup(d => d.TryBegin(paymentHash)).Returns(false);

        // Act
        await processor.StartAsync(CancellationToken.None);
        await processor.EnqueueEvent(paymentSucceededEvent);
        await Task.Delay(100);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        _deduperMock.Verify(d => d.TryBegin(paymentHash), Times.Once);
        _paymentServiceMock.Verify(p => p.ConfirmPaymentAsync(It.IsAny<string>()), Times.Never);
        _deduperMock.Verify(d => d.Complete(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task BreezEventProcessor_HandlesConfirmationFailure()
    {
        // Arrange
        var processor = new BreezEventProcessor(_loggerMock.Object, _scopeFactoryMock.Object);
        var paymentHash = "failure_hash";
        var paymentSucceededEvent = CreatePaymentSucceededEvent(paymentHash);
        var exception = new Exception("Confirmation failed");

        _deduperMock.Setup(d => d.TryBegin(paymentHash)).Returns(true);
        _paymentServiceMock.Setup(p => p.ConfirmPaymentAsync(paymentHash)).ThrowsAsync(exception);

        // Act
        await processor.StartAsync(CancellationToken.None);
        await processor.EnqueueEvent(paymentSucceededEvent);
        await Task.Delay(100);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        _deduperMock.Verify(d => d.TryBegin(paymentHash), Times.Once);
        _paymentServiceMock.Verify(p => p.ConfirmPaymentAsync(paymentHash), Times.Once);
        _deduperMock.Verify(d => d.Complete(paymentHash), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to confirm payment from SDK event.")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task EnqueueEvent_HandlesFullQueue()
    {
        // Arrange
        var processor = new BreezEventProcessor(_loggerMock.Object, _scopeFactoryMock.Object);
        var paymentHash = "test_hash";
        var paymentSucceededEvent = CreatePaymentSucceededEvent(paymentHash);

        // Fill the queue
        for (int i = 0; i < 100; i++)
        {
            await processor.EnqueueEvent(CreatePaymentSucceededEvent($"hash_{i}"));
        }

        // Act
        var enqueueTask = processor.EnqueueEvent(paymentSucceededEvent);
        await Task.Delay(50); // Give it time to block

        // Assert
        Assert.IsFalse(enqueueTask.IsCompleted); // Should be waiting
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("BreezSDK: Event queue is full.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Cleanup
        await processor.StartAsync(CancellationToken.None);
        await Task.WhenAll(enqueueTask, Task.Delay(200)); // Allow queue to drain a bit
        await processor.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public void Dispose_DisposesCancellationTokenSource()
    {
        // Arrange
        var processor = new BreezEventProcessor(_loggerMock.Object, _scopeFactoryMock.Object);
        var ctsField = typeof(BreezEventProcessor).GetField("_cts", BindingFlags.NonPublic | BindingFlags.Instance);
        var cts = (CancellationTokenSource)ctsField.GetValue(processor);

        // Act
        processor.Dispose();

        // Assert
        Assert.IsTrue(cts.IsCancellationRequested);
    }

    [TestMethod]
    public async Task StopAsync_WithoutStart_DoesNothing()
    {
        // Arrange
        var processor = new BreezEventProcessor(_loggerMock.Object, _scopeFactoryMock.Object);

        // Act
        await processor.StopAsync(CancellationToken.None);

        // Assert
        // No exception thrown
    }
}
