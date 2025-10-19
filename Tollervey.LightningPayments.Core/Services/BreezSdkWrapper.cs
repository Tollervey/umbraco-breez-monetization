using Breez.Sdk.Liquid;

namespace Tollervey.LightningPayments.Breez.Services
{
    public class BreezSdkWrapper : IBreezSdkWrapper
    {
        public Config DefaultConfig(LiquidNetwork network, string apiKey) => BreezSdkLiquidMethods.DefaultConfig(network, apiKey);

        public BindingLiquidSdk Connect(ConnectRequest request) => BreezSdkLiquidMethods.Connect(request);

        public void SetLogger(Logger logger) => BreezSdkLiquidMethods.SetLogger(logger);

        public PrepareReceiveResponse PrepareReceivePayment(BindingLiquidSdk sdk, PrepareReceiveRequest request) => sdk.PrepareReceivePayment(request);

        public ReceivePaymentResponse ReceivePayment(BindingLiquidSdk sdk, ReceivePaymentRequest request) => sdk.ReceivePayment(request);

        public void RegisterWebhook(BindingLiquidSdk sdk, string webhookUrl) => sdk.RegisterWebhook(webhookUrl);

        public void Disconnect(BindingLiquidSdk sdk) => sdk.Disconnect();

        public void AddEventListener(BindingLiquidSdk sdk, EventListener listener) => sdk.AddEventListener(listener);
    }
}