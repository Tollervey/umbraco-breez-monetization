using Our.Umbraco.Bitcoin.LightningPayments.Models;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
    /// <summary>
    /// Provides methods to manage and verify payment states.
    /// </summary>
    public interface IPaymentStateService
    {
        /// <summary>
        /// Adds a pending payment asynchronously.
        /// </summary>
        /// <param name="paymentHash">The unique hash of the payment.</param>
        /// <param name="contentId">The ID of the content being paid for.</param>
        /// <param name="userSessionId">The user's session ID.</param>
        Task AddPendingPaymentAsync(string paymentHash, int contentId, string userSessionId);

        /// <summary>
        /// Confirms a payment asynchronously.
        /// </summary>
        /// <param name="paymentHash">The unique hash of the payment.</param>
        /// <returns>A <see cref="PaymentConfirmationResult"/> describing the outcome.</returns>
        Task<PaymentConfirmationResult> ConfirmPaymentAsync(string paymentHash);

        /// <summary>
        /// Retrieves the payment state for a user and content asynchronously.
        /// </summary>
        /// <param name="userSessionId">The user's session ID.</param>
        /// <param name="contentId">The ID of the content.</param>
        /// <returns>The payment state or null if not found.</returns>
        Task<PaymentState?> GetPaymentStateAsync(string userSessionId, int contentId);

        /// <summary>
        /// Retrieves all payment states for admin view.
        /// </summary>
        Task<IEnumerable<PaymentState>> GetAllPaymentsAsync();

        /// <summary>
        /// Marks a payment as failed asynchronously.
        /// </summary>
        /// <param name="paymentHash">The unique hash of the payment.</param>
        /// <returns>True if updated, false otherwise.</returns>
        Task<bool> MarkAsFailedAsync(string paymentHash);

        /// <summary>
        /// Marks a payment as expired asynchronously.
        /// </summary>
        /// <param name="paymentHash">The unique hash of the payment.</param>
        /// <returns>True if updated, false otherwise.</returns>
        Task<bool> MarkAsExpiredAsync(string paymentHash);

        /// <summary>
        /// Marks a payment as refund pending asynchronously.
        /// </summary>
        /// <param name="paymentHash">The unique hash of the payment.</param>
        /// <returns>True if updated, false otherwise.</returns>
        Task<bool> MarkAsRefundPendingAsync(string paymentHash);

        /// <summary>
        /// Marks a payment as refunded asynchronously.
        /// </summary>
        /// <param name="paymentHash">The unique hash of the payment.</param>
        /// <returns>True if updated, false otherwise.</returns>
        Task<bool> MarkAsRefundedAsync(string paymentHash);

        /// <summary>
        /// Sets the metadata for a payment, such as amount and kind.
        /// </summary>
        /// <param name="paymentHash">The unique hash of the payment.</param>
        /// <param name="amountSat">The amount in satoshis.</param>
        /// <param name="kind">The kind of payment.</param>
        Task SetPaymentMetadataAsync(string paymentHash, ulong amountSat, PaymentKind kind);

        /// <summary>
        /// Gets a payment state by its payment hash.
        /// </summary>
        Task<PaymentState?> GetByPaymentHashAsync(string paymentHash);

        /// <summary>
        /// Attempts to get an IdempotencyMapping by key.
        /// </summary>
        Task<IdempotencyMapping?> TryGetMappingByKeyAsync(string idempotencyKey);

        /// <summary>
        /// Attempts to atomically create a new IdempotencyMapping if key does not exist. Returns existing mapping if present.
        /// </summary>
        Task<(IdempotencyMapping mapping, bool created)> TryCreateMappingAsync(string idempotencyKey, string paymentHash, string invoice);

        /// <summary>
        /// Checks if the payment state service is healthy and operational.
        /// </summary>
        /// <returns>True if the service is healthy, false otherwise.</returns>
        Task<bool> IsServiceHealthyAsync();
    }

    /// <summary>
    /// Result of attempting to confirm a payment.
    /// </summary>
    public enum PaymentConfirmationResult
    {
        /// <summary>
        /// The payment moved from Pending to Paid.
        /// </summary>
        Confirmed,
        /// <summary>
        /// The payment was already confirmed earlier.
        /// </summary>
        AlreadyConfirmed,
        /// <summary>
        /// No confirmable record was found.
        /// </summary>
        NotFound
    }
}

