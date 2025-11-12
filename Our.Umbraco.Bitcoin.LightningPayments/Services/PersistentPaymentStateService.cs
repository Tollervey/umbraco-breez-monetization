using Microsoft.EntityFrameworkCore;
using Our.Umbraco.Bitcoin.LightningPayments.Models;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
    /// <summary>
    /// Persistent implementation of IPaymentStateService using Entity Framework Core and SQLite.
    /// </summary>IsServiceH
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

                    // If an idempotency mapping exists for this paymentHash, update its status as well
                    var mapping = await _context.IdempotencyMappings.FirstOrDefaultAsync(m => m.PaymentHash == paymentHash);
                    if (mapping != null)
                    {
                        mapping.Status = PaymentStatus.Paid;
                        await _context.SaveChangesAsync();
                    }

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

        /// <summary>
        /// Attempts to get an IdempotencyMapping by key.
        /// </summary>
        public async Task<IdempotencyMapping?> TryGetMappingByKeyAsync(string idempotencyKey)
        {
            try
            {
                return await _context.IdempotencyMappings.FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey);
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to lookup idempotency mapping.", ex);
            }
        }

        /// <summary>
        /// Attempts to atomically create a new IdempotencyMapping if key does not exist. Returns existing mapping if present.
        /// </summary>
        public async Task<(IdempotencyMapping mapping, bool created)> TryCreateMappingAsync(string idempotencyKey, string paymentHash, string invoice)
        {
            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existing = await _context.IdempotencyMappings.FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey);
                    if (existing != null)
                    {
                        return (existing, false);
                    }

                    var mapping = new IdempotencyMapping
                    {
                        IdempotencyKey = idempotencyKey,
                        PaymentHash = paymentHash,
                        Invoice = invoice,
                        CreatedAt = DateTime.UtcNow,
                        Status = PaymentStatus.Pending
                    };

                    _context.IdempotencyMappings.Add(mapping);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return (mapping, true);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new PaymentException("Failed to create idempotency mapping.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsServiceHealthyAsync()
        {
            try
            {
                // Check if the database connection is healthy by attempting to connect
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                // If any exception occurs, consider the service unhealthy
                return false;
            }
        }
    }
}

