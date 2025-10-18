namespace Tollervey.LightningPayments.Core.Models
{
    /// <summary>
    /// Represents the status of a payment.
    /// </summary>
    public enum PaymentStatus { Pending, Paid, Failed, Expired, RefundPending, Refunded }

    /// <summary>
    /// Represents the state of a payment for content access.
    /// </summary>
    public class PaymentState
    {
        /// <summary>
        /// The unique hash of the payment.
        /// </summary>
        public string PaymentHash { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the content item.
        /// </summary>
        public int ContentId { get; set; }

        /// <summary>
        /// The user's session ID.
        /// </summary>
        public string UserSessionId { get; set; } = string.Empty;

        /// <summary>
        /// The current status of the payment.
        /// </summary>
        public PaymentStatus Status { get; set; }
    }
}