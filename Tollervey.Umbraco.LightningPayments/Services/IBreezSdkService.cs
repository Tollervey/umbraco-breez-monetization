namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public interface IBreezSdkService : IAsyncDisposable
    {
        Task<string> CreateInvoiceAsync(ulong amountSat, string description, CancellationToken ct = default);
        Task<string> CreateBolt12OfferAsync(ulong amountSat, string description, CancellationToken ct = default);
        Task<bool> IsConnectedAsync(CancellationToken ct = default);
    }
}
