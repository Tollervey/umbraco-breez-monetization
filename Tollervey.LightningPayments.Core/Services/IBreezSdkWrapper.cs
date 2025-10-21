using Breez.Sdk.Liquid;

namespace Tollervey.LightningPayments.Breez.Services
{
    public interface IBreezSdkWrapper
    {
        Config DefaultConfig(LiquidNetwork network, string apiKey);
        BindingLiquidSdk Connect(ConnectRequest request);
        void SetLogger(Logger logger);
        PrepareReceiveResponse PrepareReceivePayment(BindingLiquidSdk sdk, PrepareReceiveRequest request);
        ReceivePaymentResponse ReceivePayment(BindingLiquidSdk sdk, ReceivePaymentRequest request);
        void RegisterWebhook(BindingLiquidSdk sdk, string webhookUrl);
        void Disconnect(BindingLiquidSdk sdk);
        void AddEventListener(BindingLiquidSdk sdk, EventListener listener);
        void RemoveEventListener(BindingLiquidSdk sdk, EventListener listener);
    }
}