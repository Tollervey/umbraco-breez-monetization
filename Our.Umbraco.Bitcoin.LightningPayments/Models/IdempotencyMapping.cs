using System;

namespace Our.Umbraco.Bitcoin.LightningPayments.Models
{
 /// <summary>
 /// Maps a client-provided idempotency key to a created invoice/paymentHash and stores status.
 /// Key is provided by callers (e.g., client UUID) to allow safe retries.
 /// </summary>
 public class IdempotencyMapping
 {
 /// <summary>
 /// The idempotency key supplied by the client. Primary key.
 /// </summary>
 public string IdempotencyKey { get; set; } = string.Empty;

 /// <summary>
 /// The payment hash (unique) created for the invoice.
 /// </summary>
 public string PaymentHash { get; set; } = string.Empty;

 /// <summary>
 /// The raw invoice or offer string returned by the SDK.
 /// </summary>
 public string Invoice { get; set; } = string.Empty;

 /// <summary>
 /// Creation time in UTC.
 /// </summary>
 public DateTime CreatedAt { get; set; }

 /// <summary>
 /// Snapshot of payment status at the time of persistence.
 /// </summary>
 public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
 }
}

