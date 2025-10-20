using System;

namespace Tollervey.LightningPayments.Breez.Services
{
    public interface IPaymentEventDeduper
    {
        bool TryBegin(string key);
        void Complete(string key);
    }
}