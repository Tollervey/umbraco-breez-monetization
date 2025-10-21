using System.Threading.Tasks;
using Breez.Sdk.Liquid;

namespace Tollervey.LightningPayments.Breez.Services
{
    public interface IBreezEventProcessor
    {
        Task EnqueueEvent(SdkEvent.PaymentSucceeded e);
    }
}
