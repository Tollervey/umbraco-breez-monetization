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
        Task<InputType> ParseAsync(BindingLiquidSdk sdk, string input, CancellationToken ct = default);
        Task<LightningPaymentLimitsResponse> FetchLightningLimitsAsync(BindingLiquidSdk sdk, CancellationToken ct = default);
        Task<OnchainPaymentLimitsResponse> FetchOnchainLimitsAsync(BindingLiquidSdk sdk, CancellationToken ct = default);
        Task<PrepareSendResponse> PrepareSendPaymentAsync(BindingLiquidSdk sdk, PrepareSendRequest request, CancellationToken ct = default);
        Task<SendPaymentResponse> SendPaymentAsync(BindingLiquidSdk sdk, SendPaymentRequest request, CancellationToken ct = default);
        Task<List<Payment>> ListPaymentsAsync(BindingLiquidSdk sdk, ListPaymentsRequest request, CancellationToken ct = default);
        Task<Payment?> GetPaymentAsync(BindingLiquidSdk sdk, GetPaymentRequest request, CancellationToken ct = default);
        Task<List<RefundableSwap>> ListRefundablesAsync(BindingLiquidSdk sdk, CancellationToken ct = default);
        Task<RecommendedFees> RecommendedFeesAsync(BindingLiquidSdk sdk, CancellationToken ct = default);
        Task<RefundResponse> RefundAsync(BindingLiquidSdk sdk, RefundRequest request, CancellationToken ct = default);
        Task RescanOnchainSwapsAsync(BindingLiquidSdk sdk, CancellationToken ct = default);
        Task<FetchPaymentProposedFeesResponse> FetchPaymentProposedFeesAsync(BindingLiquidSdk sdk, FetchPaymentProposedFeesRequest request, CancellationToken ct = default);
        Task AcceptPaymentProposedFeesAsync(BindingLiquidSdk sdk, AcceptPaymentProposedFeesRequest request, CancellationToken ct = default);
    }
}