using System.Collections.Concurrent;
using Tollervey.Umbraco.LightningPayments.UI.Models;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    /// <summary>
    /// In-memory implementation of <see cref="IPaymentStateService"/> for development and testing.
    /// </summary>
    public class InMemoryPaymentStateService : IPaymentStateService
    {
        private readonly ConcurrentDictionary<string, PaymentState> _paymentStatesByHash = new();
        private readonly ConcurrentDictionary<string, string> _paymentHashBySession = new();

        /// <inheritdoc />
        public Task AddPendingPaymentAsync(string paymentHash, int contentId, string userSessionId)
        {
            try
            {
                var state = new PaymentState
                {
                    PaymentHash = paymentHash,
                    ContentId = contentId,
                    UserSessionId = userSessionId,
                    Status = PaymentStatus.Pending,
                    AmountSat =0UL,
                    Kind = PaymentKind.Paywall
                };
                _paymentStatesByHash.TryAdd(paymentHash, state);
                // Only map session->content for paywall items (contentId>0) to de-dupe; allow multiple tips (contentId==0)
                if (contentId >0)
                {
                    _paymentHashBySession.AddOrUpdate($"{userSessionId}:{contentId}", paymentHash, (_, __) => paymentHash);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to add pending payment.", ex);
            }
        }

        /// <inheritdoc />
        public Task SetPaymentMetadataAsync(string paymentHash, ulong amountSat, PaymentKind kind)
        {
            if (_paymentStatesByHash.TryGetValue(paymentHash, out var state))
            {
                state.AmountSat = amountSat;
                state.Kind = kind;
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<PaymentConfirmationResult> ConfirmPaymentAsync(string paymentHash)
        {
            try
            {
                if (_paymentStatesByHash.TryGetValue(paymentHash, out var state))
                {
                    if (state.Status == PaymentStatus.Paid) return Task.FromResult(PaymentConfirmationResult.AlreadyConfirmed);
                    if (state.Status == PaymentStatus.Pending)
                    {
                        state.Status = PaymentStatus.Paid;
                        return Task.FromResult(PaymentConfirmationResult.Confirmed);
                    }
                    return Task.FromResult(PaymentConfirmationResult.NotFound);
                }
                return Task.FromResult(PaymentConfirmationResult.NotFound);
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to confirm payment.", ex);
            }
        }

        /// <inheritdoc />
        public Task<PaymentState?> GetPaymentStateAsync(string userSessionId, int contentId)
        {
            try
            {
                if (contentId <=0)
                {
                    // tips are not linked to content; return null for this lookup
                    return Task.FromResult<PaymentState?>(null);
                }
                if (_paymentHashBySession.TryGetValue($"{userSessionId}:{contentId}", out var paymentHash) && _paymentStatesByHash.TryGetValue(paymentHash, out var state))
                {
                    return Task.FromResult<PaymentState?>(state);
                }
                return Task.FromResult<PaymentState?>(null);
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to get payment state.", ex);
            }
        }

        /// <inheritdoc />
        public Task<IEnumerable<PaymentState>> GetAllPaymentsAsync()
        {
            return Task.FromResult(_paymentStatesByHash.Values.AsEnumerable());
        }

        /// <inheritdoc />
        public Task<bool> MarkAsFailedAsync(string paymentHash)
        {
            try
            {
                if (_paymentStatesByHash.TryGetValue(paymentHash, out var state))
                {
                    state.Status = PaymentStatus.Failed;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to mark payment as failed.", ex);
            }
        }

        /// <inheritdoc />
        public Task<bool> MarkAsExpiredAsync(string paymentHash)
        {
            try
            {
                if (_paymentStatesByHash.TryGetValue(paymentHash, out var state))
                {
                    state.Status = PaymentStatus.Expired;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to mark payment as expired.", ex);
            }
        }

        /// <inheritdoc />
        public Task<bool> MarkAsRefundPendingAsync(string paymentHash)
        {
            try
            {
                if (_paymentStatesByHash.TryGetValue(paymentHash, out var state))
                {
                    state.Status = PaymentStatus.RefundPending;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to mark payment as refund pending.", ex);
            }
        }

        /// <inheritdoc />
        public Task<bool> MarkAsRefundedAsync(string paymentHash)
        {
            try
            {
                if (_paymentStatesByHash.TryGetValue(paymentHash, out var state))
                {
                    state.Status = PaymentStatus.Refunded;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to mark payment as refunded.", ex);
            }
        }

        /// <inheritdoc />
        public Task<PaymentState?> GetByPaymentHashAsync(string paymentHash)
        {
            _paymentStatesByHash.TryGetValue(paymentHash, out var state);
            return Task.FromResult<PaymentState?>(state);
        }
    }
}
