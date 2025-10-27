using Breez.Sdk.Liquid;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public interface IBreezEventProcessor
    {
        Task EnqueueEvent(SdkEvent.PaymentSucceeded e);
        Task Enqueue(SdkEvent e);
    }
}
