using Tollervey.Umbraco.LightningPayments.Configuration;

namespace Tollervey.Umbraco.LightningPayments.Services
{
    public interface IBreezSdkService : IAsyncDisposable
    {
        Task<string> CreateInvoiceAsync(ulong amountSat, string description);
        Task<string> CreateBolt12OfferAsync(ulong amountSat, string description);
    }
}
