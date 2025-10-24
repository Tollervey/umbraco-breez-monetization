namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public interface IBreezSdkService : IAsyncDisposable
    {
        Task<string> CreateInvoiceAsync(ulong amountSat, string description, CancellationToken ct = default);
        Task<string> CreateBolt12OfferAsync(ulong amountSat, string description, CancellationToken ct = default);
        Task<bool> IsConnectedAsync(CancellationToken ct = default);
        Task<string?> TryExtractPaymentHashAsync(string invoice, CancellationToken ct = default);
        Task<Breez.Sdk.Liquid.Payment?> GetPaymentByHashAsync(string paymentHash, CancellationToken ct = default);
        Task<long> GetReceiveFeeQuoteAsync(ulong amountSat, bool bolt12 = false, CancellationToken ct = default);
        Task<Breez.Sdk.Liquid.RecommendedFees?> GetRecommendedFeesAsync(CancellationToken ct = default);
    }
}
