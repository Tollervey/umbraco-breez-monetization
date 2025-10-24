using Breez.Sdk.Liquid;
using System.Collections.Generic;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public class BreezSdkWrapper : IBreezSdkWrapper
    {
#region Connection & Configuration

        // Configures default SDK settings for the selected network/api key.
        public Config DefaultConfig(LiquidNetwork network, string apiKey) => BreezSdkLiquidMethods.DefaultConfig(network, apiKey);

        // Establishes a connection to the Breez SDK. Underlying call is blocking.
        public Task<BindingLiquidSdk> ConnectAsync(ConnectRequest request, CancellationToken ct = default)
        => Task.Run(() => BreezSdkLiquidMethods.Connect(request), ct);

        // Sets the SDK logger.
        public void SetLogger(Logger logger) => BreezSdkLiquidMethods.SetLogger(logger);

        // Registers a webhook URL (used for offline flows). Underlying call is blocking.
        public Task RegisterWebhookAsync(BindingLiquidSdk sdk, string webhookUrl, CancellationToken ct = default)
        => Task.Run(() => sdk.RegisterWebhook(webhookUrl), ct);

        // Disconnects from the Breez SDK. Underlying call is blocking.
        public Task DisconnectAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
        => Task.Run(() => sdk.Disconnect(), ct);

#endregion

#region Parsing

        // Parses inputs (bolt11, bolt12, lnurl, bitcoin/liquid addresses, etc.). Underlying call is blocking.
        public Task<InputType> ParseAsync(BindingLiquidSdk sdk, string input, CancellationToken ct = default)
        => Task.Run(() => sdk.Parse(input), ct);

#endregion

#region Limits

        // Fetch current Lightning send/receive limits. Underlying call is blocking.
        public Task<LightningPaymentLimitsResponse> FetchLightningLimitsAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
        => Task.Run(() => sdk.FetchLightningLimits(), ct);

        // Fetch current on-chain (Bitcoin) send/receive limits. Underlying call is blocking.
        public Task<OnchainPaymentLimitsResponse> FetchOnchainLimitsAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
        => Task.Run(() => sdk.FetchOnchainLimits(), ct);

#endregion

#region Receiving payments

        // Prepare a receive payment (Bolt11, Bolt12, Bitcoin/Liquid). Underlying call is blocking.
        public Task<PrepareReceiveResponse> PrepareReceivePaymentAsync(BindingLiquidSdk sdk, PrepareReceiveRequest request, CancellationToken ct = default)
        => Task.Run(() => sdk.PrepareReceivePayment(request), ct);

        // Receive a prepared payment (returns invoice/offer/URI). Underlying call is blocking.
        public Task<ReceivePaymentResponse> ReceivePaymentAsync(BindingLiquidSdk sdk, ReceivePaymentRequest request, CancellationToken ct = default)
        => Task.Run(() => sdk.ReceivePayment(request), ct);

#endregion

#region Sending payments

        // Prepare a send payment (Bolt11/Bolt12/Liquid/BIP21). Underlying call is blocking.
        public Task<PrepareSendResponse> PrepareSendPaymentAsync(BindingLiquidSdk sdk, PrepareSendRequest request, CancellationToken ct = default)
        => Task.Run(() => sdk.PrepareSendPayment(request), ct);

        // Send a prepared payment. Underlying call is blocking.
        public Task<SendPaymentResponse> SendPaymentAsync(BindingLiquidSdk sdk, SendPaymentRequest request, CancellationToken ct = default)
        => Task.Run(() => sdk.SendPayment(request), ct);

#endregion

#region Payments history & queries

        // List payments with optional filters/paging. Underlying call is blocking.
        public Task<List<Payment>> ListPaymentsAsync(BindingLiquidSdk sdk, ListPaymentsRequest request, CancellationToken ct = default)
        => Task.Run(() => sdk.ListPayments(request), ct);

        // Get a single payment by hash or swap id. Underlying call is blocking.
        public Task<Payment?> GetPaymentAsync(BindingLiquidSdk sdk, GetPaymentRequest request, CancellationToken ct = default)
        => Task.Run(() => sdk.GetPayment(request), ct);

#endregion

#region Refunds

        // List refundable swaps (Bitcoin flow). Underlying call is blocking.
        public Task<List<RefundableSwap>> ListRefundablesAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
        => Task.Run(() => sdk.ListRefundables(), ct);

        // Get recommended on-chain fees (Bitcoin mempool estimates). Underlying call is blocking.
        public Task<RecommendedFees> RecommendedFeesAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
        => Task.Run(() => sdk.RecommendedFees(), ct);

        // Execute a refund for a refundable swap. Underlying call is blocking.
        public Task<RefundResponse> RefundAsync(BindingLiquidSdk sdk, RefundRequest request, CancellationToken ct = default)
        => Task.Run(() => sdk.Refund(request), ct);

        // Fetch currently proposed fees for amountless Bitcoin payments awaiting acceptance. Underlying call is blocking.
        public Task<FetchPaymentProposedFeesResponse> FetchPaymentProposedFeesAsync(BindingLiquidSdk sdk, FetchPaymentProposedFeesRequest request, CancellationToken ct = default)
        => Task.Run(() => sdk.FetchPaymentProposedFees(request), ct);

        // Accept proposed fees for amountless Bitcoin payments. Underlying call is blocking.
        public Task AcceptPaymentProposedFeesAsync(BindingLiquidSdk sdk, AcceptPaymentProposedFeesRequest request, CancellationToken ct = default)
        => Task.Run(() => sdk.AcceptPaymentProposedFees(request), ct);

#endregion

#region Maintenance

        // Rescan on-chain swap addresses for missed deposits. Underlying call is blocking.
        public Task RescanOnchainSwapsAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
        => Task.Run(() => sdk.RescanOnchainSwaps(), ct);

#endregion

#region Events

        // Register an event listener for SDK events.
        public void AddEventListener(BindingLiquidSdk sdk, EventListener listener) => sdk.AddEventListener(listener);

        // Placeholder: the SDK may not support listener removal yet.
        public void RemoveEventListener(BindingLiquidSdk sdk, EventListener listener)
        {
            // sdk.RemoveEventListener(listener);
        }

#endregion
    }
}