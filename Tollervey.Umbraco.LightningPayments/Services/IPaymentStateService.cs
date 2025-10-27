using Tollervey.Umbraco.LightningPayments.UI.Models;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
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
        /// <returns>True if confirmed, false otherwise.</returns>
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
    }

    public enum PaymentConfirmationResult
    {
        Confirmed,
        AlreadyConfirmed,
        NotFound
    }
}
