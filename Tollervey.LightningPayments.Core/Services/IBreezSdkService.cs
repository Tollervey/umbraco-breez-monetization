namespace Tollervey.LightningPayments.Core.Services
{
    public interface IBreezSdkService : IAsyncDisposable
    {
        Task<string> CreateInvoiceAsync(ulong amountSat, string description);
        Task<string> CreateBolt12OfferAsync(ulong amountSat, string description);
    }
}
