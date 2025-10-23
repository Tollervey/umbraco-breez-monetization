namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public interface IPaymentEventDeduper
    {
        bool TryBegin(string key);
        void Complete(string key);
    }
}