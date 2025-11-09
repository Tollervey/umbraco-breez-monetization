using Breez.Sdk.Liquid;
using System.Collections.Generic;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
 /// <summary>
 /// Thin wrapper over Breez SDK Liquid bindings to keep the higher-level service testable and focused.
 ///
 /// UI/UX pointers for consumers:
 /// - Use <see cref="ParseAsync"/> to implement a unified "Paste/Scan/Upload" entry point for sending payments
 /// (detects BOLT11/BOLT12/LNURL/BIP21/addresses). See Send UX guidelines.
 /// - Use <see cref="FetchLightningLimitsAsync"/> and <see cref="FetchOnchainLimitsAsync"/> to surface limits
 /// and pre-flight validations before confirmation screens.
 /// - Use <see cref="ListPaymentsAsync"/> and <see cref="GetPaymentAsync"/> to populate history/detail views and
 /// display fees separately from amount as recommended by Display UX guidelines.
 /// - Use <see cref="RegisterWebhookAsync"/> with a verified HTTPS endpoint to enable offline receiving flows
 /// required for LNURL-Pay in the Liquid implementation (Receive UX guidelines).
 /// - Consider <see cref="RecommendedFeesAsync"/>, <see cref="FetchPaymentProposedFeesAsync"/>, and
 /// <see cref="AcceptPaymentProposedFeesAsync"/> for flows that need to show user-acceptable fee choices
 /// (progressive disclosure).
 ///
 /// Guidelines references:
 /// - https://sdk-doc-liquid.breez.technology/guide/uxguide_send.html
 /// - https://sdk-doc-liquid.breez.technology/guide/uxguide_receive.html
 /// - https://sdk-doc-liquid.breez.technology/guide/uxguide_display.html
 /// - https://sdk-doc-liquid.breez.technology/guide/uxguide_seed.html
 /// </summary>
 public class BreezSdkWrapper : IBreezSdkWrapper
 {
#region Connection & Configuration

 /// <summary>
 /// Returns the default SDK configuration for the selected network and API key.
 /// Callers typically set a working directory on the returned config before connecting.
 /// </summary>
 public Config DefaultConfig(LiquidNetwork network, string apiKey) => BreezSdkLiquidMethods.DefaultConfig(network, apiKey);

 /// <summary>
 /// Establishes a connection to the Breez SDK.
 /// Wraps a blocking call in a Task; honor the provided cancellation token in higher layers.
 /// </summary>
 public Task<BindingLiquidSdk> ConnectAsync(ConnectRequest request, CancellationToken ct = default)
 => Task.Run(() => BreezSdkLiquidMethods.Connect(request), ct);

 /// <summary>
 /// Sets the Breez SDK logger. Forward logs to your host logger to aid diagnosis and UX telemetry.
 /// </summary>
 public void SetLogger(Logger logger) => BreezSdkLiquidMethods.SetLogger(logger);

 /// <summary>
 /// Registers a webhook URL to support offline receiving flows (LNURL-Pay on Liquid).
 /// Endpoint must be HTTPS and should verify HMAC signatures.
 /// </summary>
 public Task RegisterWebhookAsync(BindingLiquidSdk sdk, string webhookUrl, CancellationToken ct = default)
 => Task.Run(() => sdk.RegisterWebhook(webhookUrl), ct);

 /// <summary>
 /// Disconnects from the Breez SDK.
 /// </summary>
 public Task DisconnectAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
 => Task.Run(() => sdk.Disconnect(), ct);

#endregion

#region Parsing

 /// <summary>
 /// Parses user input into a typed payment target (BOLT11/BOLT12/LNURL/BIP21/addresses).
 /// Use this to power a single unified "Send" entry point.
 /// </summary>
 public Task<InputType> ParseAsync(BindingLiquidSdk sdk, string input, CancellationToken ct = default)
 => Task.Run(() => sdk.Parse(input), ct);

#endregion

#region Limits

 /// <summary>
 /// Fetch current Lightning send/receive limits. Display these in the confirmation UI and validate before sending/receiving.
 /// </summary>
 public Task<LightningPaymentLimitsResponse> FetchLightningLimitsAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
 => Task.Run(() => sdk.FetchLightningLimits(), ct);

 /// <summary>
 /// Fetch current on-chain (Bitcoin) limits. Use when supporting on-chain as an off-/on-ramp.
 /// </summary>
 public Task<OnchainPaymentLimitsResponse> FetchOnchainLimitsAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
 => Task.Run(() => sdk.FetchOnchainLimits(), ct);

#endregion

#region Receiving payments

 /// <summary>
 /// Prepare a receive payment for the given method (BOLT11, BOLT12, Bitcoin/Liquid). Returns fees and preparation data.
 /// </summary>
 public Task<PrepareReceiveResponse> PrepareReceivePaymentAsync(BindingLiquidSdk sdk, PrepareReceiveRequest request, CancellationToken ct = default)
 => Task.Run(() => sdk.PrepareReceivePayment(request), ct);

 /// <summary>
 /// Finalize a prepared receive request and return the destination string (invoice/offer/URI).
 /// </summary>
 public Task<ReceivePaymentResponse> ReceivePaymentAsync(BindingLiquidSdk sdk, ReceivePaymentRequest request, CancellationToken ct = default)
 => Task.Run(() => sdk.ReceivePayment(request), ct);

#endregion

#region Sending payments

 /// <summary>
 /// Prepare a send payment for any supported input. Use this to present fees/limits before asking for confirmation.
 /// </summary>
 public Task<PrepareSendResponse> PrepareSendPaymentAsync(BindingLiquidSdk sdk, PrepareSendRequest request, CancellationToken ct = default)
 => Task.Run(() => sdk.PrepareSendPayment(request), ct);

 /// <summary>
 /// Execute a previously prepared send payment.
 /// </summary>
 public Task<SendPaymentResponse> SendPaymentAsync(BindingLiquidSdk sdk, SendPaymentRequest request, CancellationToken ct = default)
 => Task.Run(() => sdk.SendPayment(request), ct);

#endregion

#region Payments history & queries

 /// <summary>
 /// List payments with optional filters/paging for history UI. Display state and fees separately per UX guidance.
 /// </summary>
 public Task<List<Payment>> ListPaymentsAsync(BindingLiquidSdk sdk, ListPaymentsRequest request, CancellationToken ct = default)
 => Task.Run(() => sdk.ListPayments(request), ct);

 /// <summary>
 /// Fetch a single payment (by hash or swap id). Use for a details page.
 /// </summary>
 public Task<Payment?> GetPaymentAsync(BindingLiquidSdk sdk, GetPaymentRequest request, CancellationToken ct = default)
 => Task.Run(() => sdk.GetPayment(request), ct);

#endregion

#region Refunds

 /// <summary>
 /// List refundable swaps (Bitcoin flow). Consider surfacing in an advanced section.
 /// </summary>
 public Task<List<RefundableSwap>> ListRefundablesAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
 => Task.Run(() => sdk.ListRefundables(), ct);

 /// <summary>
 /// Get recommended on-chain fees. Useful for transparent fee display before confirmations.
 /// </summary>
 public Task<RecommendedFees> RecommendedFeesAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
 => Task.Run(() => sdk.RecommendedFees(), ct);

 /// <summary>
 /// Execute a refund for a refundable swap.
 /// </summary>
 public Task<RefundResponse> RefundAsync(BindingLiquidSdk sdk, RefundRequest request, CancellationToken ct = default)
 => Task.Run(() => sdk.Refund(request), ct);

 /// <summary>
 /// Fetch currently proposed fees for amountless Bitcoin payments awaiting acceptance.
 /// Present these in the UI and allow the user to accept or decline.
 /// </summary>
 public Task<FetchPaymentProposedFeesResponse> FetchPaymentProposedFeesAsync(BindingLiquidSdk sdk, FetchPaymentProposedFeesRequest request, CancellationToken ct = default)
 => Task.Run(() => sdk.FetchPaymentProposedFees(request), ct);

 /// <summary>
 /// Accept proposed fees for amountless Bitcoin payments.
 /// </summary>
 public Task AcceptPaymentProposedFeesAsync(BindingLiquidSdk sdk, AcceptPaymentProposedFeesRequest request, CancellationToken ct = default)
 => Task.Run(() => sdk.AcceptPaymentProposedFees(request), ct);

#endregion

#region Maintenance

 /// <summary>
 /// Rescan on-chain swap addresses for missed deposits.
 /// </summary>
 public Task RescanOnchainSwapsAsync(BindingLiquidSdk sdk, CancellationToken ct = default)
 => Task.Run(() => sdk.RescanOnchainSwaps(), ct);

#endregion

#region Events

 /// <summary>
 /// Register an event listener for SDK events. Use to update UI state reactively (pending/succeeded/failed).
 /// </summary>
 public void AddEventListener(BindingLiquidSdk sdk, EventListener listener) => sdk.AddEventListener(listener);

 /// <summary>
 /// Placeholder for removing a listener (SDK may not support removal yet).
 /// </summary>
 public void RemoveEventListener(BindingLiquidSdk sdk, EventListener listener)
 {
 // sdk.RemoveEventListener(listener);
 }

#endregion
 }
}
