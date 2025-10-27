using Microsoft.EntityFrameworkCore;
using Tollervey.Umbraco.LightningPayments.UI.Models;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    /// <summary>
    /// Persistent implementation of IPaymentStateService using Entity Framework Core and SQLite.
    /// </summary>
    public class PersistentPaymentStateService : IPaymentStateService
    {
        private readonly PaymentDbContext _context;

        public PersistentPaymentStateService(PaymentDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task AddPendingPaymentAsync(string paymentHash, int contentId, string userSessionId)
        {
            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Only de-dupe for paywall content (contentId >0). For tips (contentId ==0), allow multiple pendings per session.
                    if (contentId > 0)
                    {
                        var existing = await _context.PaymentStates
                            .FirstOrDefaultAsync(p => p.UserSessionId == userSessionId && p.ContentId == contentId && p.Status == PaymentStatus.Pending);

                        if (existing != null)
                        {
                            _context.PaymentStates.Remove(existing);
                        }
                    }

                    var state = new PaymentState
                    {
                        PaymentHash = paymentHash,
                        ContentId = contentId,
                        UserSessionId = userSessionId,
                        Status = PaymentStatus.Pending,
                        AmountSat = 0UL,
                        Kind = PaymentKind.Paywall
                    };

                    _context.PaymentStates.Add(state);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to add pending payment.", ex);
            }
        }

        // helper to set amount/kind when known (e.g., tips)
        public async Task SetPaymentMetadataAsync(string paymentHash, ulong amountSat, PaymentKind kind)
        {
            var state = await _context.PaymentStates.FirstOrDefaultAsync(p => p.PaymentHash == paymentHash);
            if (state != null)
            {
                state.AmountSat = amountSat;
                state.Kind = kind;
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc />
        public async Task<PaymentConfirmationResult> ConfirmPaymentAsync(string paymentHash)
        {
            try
            {
                var state = await _context.PaymentStates.FirstOrDefaultAsync(p => p.PaymentHash == paymentHash);
                if (state == null)
                {
                    return PaymentConfirmationResult.NotFound;
                }
                if (state.Status == PaymentStatus.Paid)
                {
                    return PaymentConfirmationResult.AlreadyConfirmed;
                }
                if (state.Status == PaymentStatus.Pending)
                {
                    state.Status = PaymentStatus.Paid;
                    await _context.SaveChangesAsync();
                    return PaymentConfirmationResult.Confirmed;
                }
                // For other statuses, do not confirm
                return PaymentConfirmationResult.NotFound;
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to confirm payment.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<PaymentState?> GetPaymentStateAsync(string userSessionId, int contentId)
        {
            try
            {
                return await _context.PaymentStates.FirstOrDefaultAsync(p => p.UserSessionId == userSessionId && p.ContentId == contentId);
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to get payment state.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<PaymentState>> GetAllPaymentsAsync()
        {
            try
            {
                return await _context.PaymentStates.ToListAsync();
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to get all payments.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<bool> MarkAsFailedAsync(string paymentHash)
        {
            try
            {
                var state = await _context.PaymentStates.FirstOrDefaultAsync(p => p.PaymentHash == paymentHash);
                if (state != null)
                {
                    state.Status = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to mark payment as failed.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<bool> MarkAsExpiredAsync(string paymentHash)
        {
            try
            {
                var state = await _context.PaymentStates.FirstOrDefaultAsync(p => p.PaymentHash == paymentHash);
                if (state != null)
                {
                    state.Status = PaymentStatus.Expired;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to mark payment as expired.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<bool> MarkAsRefundPendingAsync(string paymentHash)
        {
            try
            {
                var state = await _context.PaymentStates.FirstOrDefaultAsync(p => p.PaymentHash == paymentHash);
                if (state != null)
                {
                    state.Status = PaymentStatus.RefundPending;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to mark payment as refund pending.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<bool> MarkAsRefundedAsync(string paymentHash)
        {
            try
            {
                var state = await _context.PaymentStates.FirstOrDefaultAsync(p => p.PaymentHash == paymentHash);
                if (state != null)
                {
                    state.Status = PaymentStatus.Refunded;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to mark payment as refunded.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<PaymentState?> GetByPaymentHashAsync(string paymentHash)
        {
            try
            {
                return await _context.PaymentStates.FirstOrDefaultAsync(p => p.PaymentHash == paymentHash);
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to get payment by hash.", ex);
            }
        }
    }
}
