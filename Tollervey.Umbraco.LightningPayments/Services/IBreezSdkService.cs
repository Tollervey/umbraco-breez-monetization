namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    /// <summary>
    /// High-level Breez SDK service used by controllers/components. Encapsulates invoice/offer creation,
    /// parsing, fee quoting, and status queries.
    /// </summary>
    public interface IBreezSdkService : IAsyncDisposable
    {
        /// <summary>
        /// Creates a BOLT11 invoice for the given amount and description.
        /// </summary>
        Task<string> CreateInvoiceAsync(ulong amountSat, string description, CancellationToken ct = default);

        /// <summary>
        /// Creates a BOLT12 offer for the given amount and description (Liquid only).
        /// </summary>
        Task<string> CreateBolt12OfferAsync(ulong amountSat, string description, CancellationToken ct = default);

        /// <summary>
        /// Indicates whether the SDK is connected.
        /// </summary>
        Task<bool> IsConnectedAsync(CancellationToken ct = default);

        /// <summary>
        /// Attempts to parse a BOLT11 invoice and extract its payment hash.
        /// </summary>
        Task<string?> TryExtractPaymentHashAsync(string invoice, CancellationToken ct = default);

        /// <summary>
        /// Attempts to extract the expiry of a BOLT11 invoice.
        /// </summary>
        Task<DateTimeOffset?> TryExtractInvoiceExpiryAsync(string invoice, CancellationToken ct = default);

        /// <summary>
        /// Retrieves a payment by its payment hash.
        /// </summary>
        Task<Breez.Sdk.Liquid.Payment?> GetPaymentByHashAsync(string paymentHash, CancellationToken ct = default);

        /// <summary>
        /// Gets a fee quote for receiving the specified amount.
        /// </summary>
        Task<long> GetReceiveFeeQuoteAsync(ulong amountSat, bool bolt12 = false, CancellationToken ct = default);

        /// <summary>
        /// Retrieves recommended on-chain fees.
        /// </summary>
        Task<Breez.Sdk.Liquid.RecommendedFees?> GetRecommendedFeesAsync(CancellationToken ct = default);
    }
}
