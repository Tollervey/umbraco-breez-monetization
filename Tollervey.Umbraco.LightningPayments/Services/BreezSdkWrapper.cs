using Breez.Sdk.Liquid;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public class BreezSdkWrapper : IBreezSdkWrapper
    {
        public Config DefaultConfig(LiquidNetwork network, string apiKey) => BreezSdkLiquidMethods.DefaultConfig(network, apiKey);

        // The underlying SDK method is blocking. Task.Run is used to avoid blocking the calling thread.
        public Task<BindingLiquidSdk> ConnectAsync(ConnectRequest request, CancellationToken ct = default) => Task.Run(() => BreezSdkLiquidMethods.Connect(request), ct);

        public void SetLogger(Logger logger) => BreezSdkLiquidMethods.SetLogger(logger);

        // The underlying SDK method is blocking. Task.Run is used to avoid blocking the calling thread.
        public Task<PrepareReceiveResponse> PrepareReceivePaymentAsync(BindingLiquidSdk sdk, PrepareReceiveRequest request, CancellationToken ct = default) => Task.Run(() => sdk.PrepareReceivePayment(request), ct);

        // The underlying SDK method is blocking. Task.Run is used to avoid blocking the calling thread.
        public Task<ReceivePaymentResponse> ReceivePaymentAsync(BindingLiquidSdk sdk, ReceivePaymentRequest request, CancellationToken ct = default) => Task.Run(() => sdk.ReceivePayment(request), ct);

        // The underlying SDK method is blocking. Task.Run is used to avoid blocking the calling thread.
        public Task RegisterWebhookAsync(BindingLiquidSdk sdk, string webhookUrl, CancellationToken ct = default) => Task.Run(() => sdk.RegisterWebhook(webhookUrl), ct);

        // The underlying SDK method is blocking. Task.Run is used to avoid blocking the calling thread.
        public Task DisconnectAsync(BindingLiquidSdk sdk, CancellationToken ct = default) => Task.Run(() => sdk.Disconnect(), ct);

        public void AddEventListener(BindingLiquidSdk sdk, EventListener listener) => sdk.AddEventListener(listener);

        public void RemoveEventListener(BindingLiquidSdk sdk, EventListener listener)
        {
            // Note: The Breez SDK does not currently support removing event listeners.
            // This is a placeholder for when/if it becomes available.
            // sdk.RemoveEventListener(listener);
        }

        // New: parse inputs using Breez SDK
        public Task<InputType> ParseAsync(BindingLiquidSdk sdk, string input, CancellationToken ct = default)
        => Task.Run(() => sdk.Parse(input), ct);
    }
}