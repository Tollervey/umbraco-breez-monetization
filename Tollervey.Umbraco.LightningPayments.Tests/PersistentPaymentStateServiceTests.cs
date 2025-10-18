using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tollervey.Umbraco.LightningPayments.Models;
using Tollervey.Umbraco.LightningPayments.Services;

namespace MyExtensionsTests
{
    [TestClass]
    public class PersistentPaymentStateServiceTests : IDisposable
    {
        private readonly PaymentDbContext _context;
        private readonly PersistentPaymentStateService _service;

        public PersistentPaymentStateServiceTests()
        {
            var options = new DbContextOptionsBuilder<PaymentDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new PaymentDbContext(options);
            _service = new PersistentPaymentStateService(_context);
        }

        [TestMethod]
        public async Task AddPendingPayment_ShouldAddPaymentState()
        {
            // Arrange
            var paymentHash = "hash123";
            var contentId = 1;
            var sessionId = "session123";

            // Act
            await _service.AddPendingPaymentAsync(paymentHash, contentId, sessionId);

            // Assert
            var state = _context.PaymentStates.FirstOrDefault(p => p.PaymentHash == paymentHash);
            Assert.IsNotNull(state);
            Assert.AreEqual(contentId, state.ContentId);
            Assert.AreEqual(sessionId, state.UserSessionId);
            Assert.AreEqual(PaymentStatus.Pending, state.Status);
        }

        [TestMethod]
        public async Task ConfirmPayment_ShouldUpdateStatusToPaid()
        {
            // Arrange
            var paymentHash = "hash123";
            var contentId = 1;
            var sessionId = "session123";
            await _service.AddPendingPaymentAsync(paymentHash, contentId, sessionId);

            // Act
            var result = await _service.ConfirmPaymentAsync(paymentHash);

            // Assert
            Assert.AreEqual(PaymentConfirmationResult.Confirmed, result);
            var state = _context.PaymentStates.FirstOrDefault(p => p.PaymentHash == paymentHash);
            Assert.IsNotNull(state);
            Assert.AreEqual(PaymentStatus.Paid, state.Status);
        }

        [TestMethod]
        public async Task ConfirmPayment_NonExistentHash_ShouldReturnNotFound()
        {
            // Act
            var result = await _service.ConfirmPaymentAsync("nonexistent");

            // Assert
            Assert.AreEqual(PaymentConfirmationResult.NotFound, result);
        }

        [TestMethod]
        public async Task GetPaymentState_ShouldReturnCorrectState()
        {
            // Arrange
            var paymentHash = "hash123";
            var contentId = 1;
            var sessionId = "session123";
            await _service.AddPendingPaymentAsync(paymentHash, contentId, sessionId);

            // Act
            var state = await _service.GetPaymentStateAsync(sessionId, contentId);

            // Assert
            Assert.IsNotNull(state);
            Assert.AreEqual(paymentHash, state.PaymentHash);
            Assert.AreEqual(PaymentStatus.Pending, state.Status);
        }

        [TestMethod]
        public async Task GetPaymentState_NonExistent_ShouldReturnNull()
        {
            // Act
            var state = await _service.GetPaymentStateAsync("session", 1);

            // Assert
            Assert.IsNull(state);
        }

        [TestMethod]
        public async Task AddPendingPayment_ShouldReplaceExistingPendingPayment()
        {
            // Arrange
            var contentId = 1;
            var sessionId = "session123";
            var firstHash = "hash1";
            var secondHash = "hash2";

            // Add first pending payment
            await _service.AddPendingPaymentAsync(firstHash, contentId, sessionId);

            // Act: Add second pending payment for same session and content
            await _service.AddPendingPaymentAsync(secondHash, contentId, sessionId);

            // Assert
            var states = _context.PaymentStates
                .Where(p => p.UserSessionId == sessionId && p.ContentId == contentId)
                .ToList();

            Assert.AreEqual(1, states.Count);
            var state = states.Single();
            Assert.AreEqual(secondHash, state.PaymentHash);
            Assert.AreEqual(PaymentStatus.Pending, state.Status);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
