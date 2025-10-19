using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Tollervey.LightningPayments.Breez.Configuration;
using Tollervey.LightningPayments.Breez.Models;
using Tollervey.LightningPayments.Breez.Services;
using Tollervey.Umbraco.LightningPayments.Core.Controllers;
using Tollervey.Umbraco.LightningPayments.Core.Models;

namespace Tollervey.Umbraco.LightningPayments.CoreTests;

[TestClass]
public class BreezWebhookControllerTests
{
    private Mock<IPaymentStateService> _paymentStateServiceMock;
    private Mock<ILogger<BreezWebhookController>> _loggerMock;
    private Mock<IOptions<LightningPaymentsSettings>> _settingsMock;
    private Mock<IEmailService> _emailServiceMock;
    private BreezWebhookController _controller;

    [TestInitialize]
    public void Setup()
    {
        _paymentStateServiceMock = new Mock<IPaymentStateService>();
        _loggerMock = new Mock<ILogger<BreezWebhookController>>();
        _settingsMock = new Mock<IOptions<LightningPaymentsSettings>>();
        _emailServiceMock = new Mock<IEmailService>();

        var settings = new LightningPaymentsSettings
        {
            WebhookSecret = "test-secret",
            AdminEmail = "admin@example.com"
        };
        _settingsMock.Setup(s => s.Value).Returns(settings);

        _controller = new BreezWebhookController(
            _paymentStateServiceMock.Object,
            _loggerMock.Object,
            _settingsMock.Object,
            _emailServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleWebhook_InvalidSignature_ReturnsBadRequest()
    {
        // Arrange
        var payload = new BreezWebhookPayload { Type = "payment_received", Payment = new PaymentDetails { Id = "test-hash" } };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        _controller.Request.Headers["Breez-Signature"] = "invalid-signature";
        _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _controller.HandleWebhook(payload);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task HandleWebhook_ValidSignature_ConfirmsPayment()
    {
        // Arrange
        var payload = new BreezWebhookPayload { Type = "payment_received", Payment = new PaymentDetails { Id = "test-hash" } };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(_settingsMock.Object.Value.WebhookSecret),
            System.Text.Encoding.UTF8.GetBytes(json)));
        _controller.Request.Headers["Breez-Signature"] = signature;
        _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        _paymentStateServiceMock.Setup(p => p.ConfirmPaymentAsync("test-hash"))
            .ReturnsAsync(PaymentConfirmationResult.Confirmed);

        // Act
        var result = await _controller.HandleWebhook(payload);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkResult));
        _paymentStateServiceMock.Verify(p => p.ConfirmPaymentAsync("test-hash"), Times.Once);
    }

    [TestMethod]
    public async Task HandleWebhook_ConfirmationFailed_SendsEmail()
    {
        // Arrange
        var payload = new BreezWebhookPayload { Type = "payment_received", Payment = new PaymentDetails { Id = "test-hash" } };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(_settingsMock.Object.Value.WebhookSecret),
            System.Text.Encoding.UTF8.GetBytes(json)));
        _controller.Request.Headers["Breez-Signature"] = signature;
        _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        _paymentStateServiceMock.Setup(p => p.ConfirmPaymentAsync("test-hash"))
            .ReturnsAsync(PaymentConfirmationResult.NotFound);

        // Act
        var result = await _controller.HandleWebhook(payload);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkResult));
        _emailServiceMock.Verify(e => e.SendEmailAsync(
            _settingsMock.Object.Value.AdminEmail,
            "Payment Confirmation Failed",
            It.Is<string>(s => s.Contains("test-hash"))), Times.Once);
    }

    [TestMethod]
    public async Task HandleWebhook_InvalidPayload_ReturnsBadRequest()
    {
        // Arrange
        var payload = new BreezWebhookPayload { Type = "payment_received" }; // Missing Payment
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(_settingsMock.Object.Value.WebhookSecret),
            System.Text.Encoding.UTF8.GetBytes(json)));
        _controller.Request.Headers["Breez-Signature"] = signature;
        _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _controller.HandleWebhook(payload);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task HandleWebhook_PaymentFailed_MarksAsFailed()
    {
        // Arrange
        var payload = new BreezWebhookPayload { Type = "payment_failed", Payment = new PaymentDetails { Id = "test-hash" } };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(_settingsMock.Object.Value.WebhookSecret),
            System.Text.Encoding.UTF8.GetBytes(json)));
        _controller.Request.Headers["Breez-Signature"] = signature;
        _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        _paymentStateServiceMock.Setup(p => p.MarkAsFailedAsync("test-hash"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.HandleWebhook(payload);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        _paymentStateServiceMock.Verify(p => p.MarkAsFailedAsync("test-hash"), Times.Once);
    }

    [TestMethod]
    public async Task HandleWebhook_InvoiceExpired_MarksAsExpired()
    {
        // Arrange
        var payload = new BreezWebhookPayload { Type = "invoice_expired", Payment = new PaymentDetails { Id = "test-hash" } };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(_settingsMock.Object.Value.WebhookSecret),
            System.Text.Encoding.UTF8.GetBytes(json)));
        _controller.Request.Headers["Breez-Signature"] = signature;
        _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        _paymentStateServiceMock.Setup(p => p.MarkAsExpiredAsync("test-hash"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.HandleWebhook(payload);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        _paymentStateServiceMock.Verify(p => p.MarkAsExpiredAsync("test-hash"), Times.Once);
    }

    [TestMethod]
    public async Task HandleWebhook_RefundInitiated_MarksAsRefundPending()
    {
        // Arrange
        var payload = new BreezWebhookPayload { Type = "refund_initiated", Payment = new PaymentDetails { Id = "test-hash" } };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(_settingsMock.Object.Value.WebhookSecret),
            System.Text.Encoding.UTF8.GetBytes(json)));
        _controller.Request.Headers["Breez-Signature"] = signature;
        _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        _paymentStateServiceMock.Setup(p => p.MarkAsRefundPendingAsync("test-hash"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.HandleWebhook(payload);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        _paymentStateServiceMock.Verify(p => p.MarkAsRefundPendingAsync("test-hash"), Times.Once);
    }

    [TestMethod]
    public async Task HandleWebhook_RefundSucceeded_MarksAsRefunded()
    {
        // Arrange
        var payload = new BreezWebhookPayload { Type = "refund_succeeded", Payment = new PaymentDetails { Id = "test-hash" } };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(_settingsMock.Object.Value.WebhookSecret),
            System.Text.Encoding.UTF8.GetBytes(json)));
        _controller.Request.Headers["Breez-Signature"] = signature;
        _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        _paymentStateServiceMock.Setup(p => p.MarkAsRefundedAsync("test-hash"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.HandleWebhook(payload);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        _paymentStateServiceMock.Verify(p => p.MarkAsRefundedAsync("test-hash"), Times.Once);
    }

    [TestMethod]
    public async Task HandleWebhook_UnknownType_ReturnsBadRequest()
    {
        // Arrange
        var payload = new BreezWebhookPayload { Type = "unknown_type", Payment = new PaymentDetails { Id = "test-hash" } };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(_settingsMock.Object.Value.WebhookSecret),
            System.Text.Encoding.UTF8.GetBytes(json)));
        _controller.Request.Headers["Breez-Signature"] = signature;
        _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _controller.HandleWebhook(payload);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task HandleWebhook_AlreadyConfirmed_ReturnsOk()
    {
        // Arrange
        var payload = new BreezWebhookPayload { Type = "payment_succeeded", Payment = new PaymentDetails { Id = "test-hash" } };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(_settingsMock.Object.Value.WebhookSecret),
            System.Text.Encoding.UTF8.GetBytes(json)));
        _controller.Request.Headers["Breez-Signature"] = signature;
        _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        _paymentStateServiceMock.Setup(p => p.ConfirmPaymentAsync("test-hash"))
            .ReturnsAsync(PaymentConfirmationResult.AlreadyConfirmed);

        // Act
        var result = await _controller.HandleWebhook(payload);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task HandleWebhook_UpdateFailed_ReturnsNotFound()
    {
        // Arrange
        var payload = new BreezWebhookPayload { Type = "payment_failed", Payment = new PaymentDetails { Id = "test-hash" } };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(_settingsMock.Object.Value.WebhookSecret),
            System.Text.Encoding.UTF8.GetBytes(json)));
        _controller.Request.Headers["Breez-Signature"] = signature;
        _controller.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        _paymentStateServiceMock.Setup(p => p.MarkAsFailedAsync("test-hash"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.HandleWebhook(payload);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }

    // Add more tests for other scenarios, like different event types, etc.
}
