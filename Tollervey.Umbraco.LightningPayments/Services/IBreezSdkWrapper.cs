using Breez.Sdk.Liquid;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public interface IBreezSdkWrapper
    {
        Config DefaultConfig(LiquidNetwork network, string apiKey);
        Task<BindingLiquidSdk> ConnectAsync(ConnectRequest request, CancellationToken ct = default);
        void SetLogger(Logger logger);
        Task<PrepareReceiveResponse> PrepareReceivePaymentAsync(BindingLiquidSdk sdk, PrepareReceiveRequest request, CancellationToken ct = default);
        Task<ReceivePaymentResponse> ReceivePaymentAsync(BindingLiquidSdk sdk, ReceivePaymentRequest request, CancellationToken ct = default);
        Task RegisterWebhookAsync(BindingLiquidSdk sdk, string webhookUrl, CancellationToken ct = default);
        Task DisconnectAsync(BindingLiquidSdk sdk, CancellationToken ct = default);
        void AddEventListener(BindingLiquidSdk sdk, EventListener listener);
        void RemoveEventListener(BindingLiquidSdk sdk, EventListener listener);
    }
}