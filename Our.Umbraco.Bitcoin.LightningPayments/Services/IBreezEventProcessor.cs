using Breez.Sdk.Liquid;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
    /// <summary>
    /// Processes Breez SDK events, queuing and consuming them to update application state and notify clients.
    /// </summary>
    public interface IBreezEventProcessor
    {
        /// <summary>
        /// Enqueue a strongly-typed payment succeeded event.
        /// </summary>
        Task EnqueueEvent(SdkEvent.PaymentSucceeded e);

        /// <summary>
        /// Enqueue a generic SDK event.
        /// </summary>
        Task Enqueue(SdkEvent e);
    }
}

