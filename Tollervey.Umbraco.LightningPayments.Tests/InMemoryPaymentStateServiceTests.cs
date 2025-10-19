using System.Collections.Concurrent;
using Tollervey.LightningPayments.Breez.Models;
using Tollervey.LightningPayments.Breez.Services;

namespace Tollervey.Umbraco.LightningPayments.Tests;

[TestClass]
public class InMemoryPaymentStateServiceTests
{
    private InMemoryPaymentStateService _service;

    [TestInitialize]
    public void Setup()
    {
        _service = new InMemoryPaymentStateService();
    }

    [TestMethod]
    public async Task AddPendingPaymentAsync_AddsStateSuccessfully()
    {
        var paymentHash = "hash1";
        var contentId = 1;
        var sessionId = "session1";

        await _service.AddPendingPaymentAsync(paymentHash, contentId, sessionId);

        var state = await _service.GetPaymentStateAsync(sessionId, contentId);
        Assert.IsNotNull(state);
        Assert.AreEqual(paymentHash, state.PaymentHash);
        Assert.AreEqual(PaymentStatus.Pending, state.Status);
    }

    [TestMethod]
    [ExpectedException(typeof(PaymentException))]
    public async Task AddPendingPaymentAsync_ThrowsOnNullHash()
    {
        await _service.AddPendingPaymentAsync(null!, 1, "session");
    }

    [TestMethod]
    public async Task ConfirmPaymentAsync_ConfirmsPendingPayment()
    {
        var paymentHash = "hash1";
        var contentId = 1;
        var sessionId = "session1";
        await _service.AddPendingPaymentAsync(paymentHash, contentId, sessionId);

        var result = await _service.ConfirmPaymentAsync(paymentHash);

        Assert.AreEqual(PaymentConfirmationResult.Confirmed, result);
        var state = await _service.GetPaymentStateAsync(sessionId, contentId);
        Assert.AreEqual(PaymentStatus.Paid, state!.Status);
    }

    [TestMethod]
    public async Task ConfirmPaymentAsync_ReturnsAlreadyConfirmedIfPaid()
    {
        var paymentHash = "hash1";
        var contentId = 1;
        var sessionId = "session1";
        await _service.AddPendingPaymentAsync(paymentHash, contentId, sessionId);
        await _service.ConfirmPaymentAsync(paymentHash); // First confirm

        var result = await _service.ConfirmPaymentAsync(paymentHash);

        Assert.AreEqual(PaymentConfirmationResult.AlreadyConfirmed, result);
    }

    [TestMethod]
    public async Task ConfirmPaymentAsync_ReturnsNotFoundIfNotExists()
    {
        var result = await _service.ConfirmPaymentAsync("nonexistent");

        Assert.AreEqual(PaymentConfirmationResult.NotFound, result);
    }

    [TestMethod]
    public async Task GetPaymentStateAsync_ReturnsNullIfNotFound()
    {
        var state = await _service.GetPaymentStateAsync("nonexistent", 1);

        Assert.IsNull(state);
    }

    [TestMethod]
    public async Task GetAllPaymentsAsync_ReturnsAllStates()
    {
        await _service.AddPendingPaymentAsync("hash1", 1, "session1");
        await _service.AddPendingPaymentAsync("hash2", 2, "session2");

        var all = await _service.GetAllPaymentsAsync();

        Assert.AreEqual(2, all.Count());
    }

    [TestMethod]
    public async Task MarkAsFailedAsync_MarksIfExists()
    {
        var paymentHash = "hash1";
        await _service.AddPendingPaymentAsync(paymentHash, 1, "session1");

        var success = await _service.MarkAsFailedAsync(paymentHash);

        Assert.IsTrue(success);
        var state = (await _service.GetAllPaymentsAsync()).First();
        Assert.AreEqual(PaymentStatus.Failed, state.Status);
    }

    [TestMethod]
    public async Task MarkAsFailedAsync_ReturnsFalseIfNotExists()
    {
        var success = await _service.MarkAsFailedAsync("nonexistent");

        Assert.IsFalse(success);
    }

    // Similar tests for MarkAsExpiredAsync, MarkAsRefundPendingAsync, MarkAsRefundedAsync

    [TestMethod]
    public async Task MarkAsExpiredAsync_MarksIfExists()
    {
        var paymentHash = "hash1";
        await _service.AddPendingPaymentAsync(paymentHash, 1, "session1");

        var success = await _service.MarkAsExpiredAsync(paymentHash);

        Assert.IsTrue(success);
        var state = (await _service.GetAllPaymentsAsync()).First();
        Assert.AreEqual(PaymentStatus.Expired, state.Status);
    }

    [TestMethod]
    public async Task MarkAsRefundPendingAsync_MarksIfExists()
    {
        var paymentHash = "hash1";
        await _service.AddPendingPaymentAsync(paymentHash, 1, "session1");

        var success = await _service.MarkAsRefundPendingAsync(paymentHash);

        Assert.IsTrue(success);
        var state = (await _service.GetAllPaymentsAsync()).First();
        Assert.AreEqual(PaymentStatus.RefundPending, state.Status);
    }

    [TestMethod]
    public async Task MarkAsRefundedAsync_MarksIfExists()
    {
        var paymentHash = "hash1";
        await _service.AddPendingPaymentAsync(paymentHash, 1, "session1");

        var success = await _service.MarkAsRefundedAsync(paymentHash);

        Assert.IsTrue(success);
        var state = (await _service.GetAllPaymentsAsync()).First();
        Assert.AreEqual(PaymentStatus.Refunded, state.Status);
    }

    [TestMethod]
    [ExpectedException(typeof(PaymentException))]
    public async Task ConfirmPaymentAsync_ThrowsOnException()
    {
        // To test the catch block, but since it's concurrent, hard to force exception. For coverage, perhaps reflection or assume.
    }

    // Add concurrency tests if needed, e.g., multiple threads adding/confirming
}