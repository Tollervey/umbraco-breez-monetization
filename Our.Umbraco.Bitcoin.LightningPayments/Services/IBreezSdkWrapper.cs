using Breez.Sdk.Liquid;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
    /// <summary>
    /// Abstraction over Breez SDK Liquid bindings to facilitate testing and hide direct dependency from higher-level services.
    /// </summary>
    public interface IBreezSdkWrapper
    {
        /// <summary>
        /// Returns the default SDK configuration for the selected network and API key.
        /// </summary>
        Config DefaultConfig(LiquidNetwork network, string apiKey);

        /// <summary>
        /// Establishes a connection to the Breez SDK.
        /// </summary>
        Task<BindingLiquidSdk> ConnectAsync(ConnectRequest request, CancellationToken ct = default);

        /// <summary>
        /// Sets the Breez SDK logger.
        /// </summary>
        void SetLogger(Logger logger);

        /// <summary>
        /// Prepare a receive payment operation.
        /// </summary>
        Task<PrepareReceiveResponse> PrepareReceivePaymentAsync(BindingLiquidSdk sdk, PrepareReceiveRequest request, CancellationToken ct = default);

        /// <summary>
        /// Finalize a previously prepared receive payment operation.
        /// </summary>
        Task<ReceivePaymentResponse> ReceivePaymentAsync(BindingLiquidSdk sdk, ReceivePaymentRequest request, CancellationToken ct = default);

        /// <summary>
        /// Registers a webhook callback URL.
        /// </summary>
        Task RegisterWebhookAsync(BindingLiquidSdk sdk, string webhookUrl, CancellationToken ct = default);

        /// <summary>
        /// Disconnects the SDK.
        /// </summary>
        Task DisconnectAsync(BindingLiquidSdk sdk, CancellationToken ct = default);

        /// <summary>
        /// Adds an event listener to the SDK instance.
        /// </summary>
        void AddEventListener(BindingLiquidSdk sdk, EventListener listener);

        /// <summary>
        /// Removes a previously added event listener (may be a no-op depending on SDK support).
        /// </summary>
        void RemoveEventListener(BindingLiquidSdk sdk, EventListener listener);

        /// <summary>
        /// Parses user input into a typed payment target.
        /// </summary>
        Task<InputType> ParseAsync(BindingLiquidSdk sdk, string input, CancellationToken ct = default);

        /// <summary>
        /// Fetches Lightning payment limits.
        /// </summary>
        Task<LightningPaymentLimitsResponse> FetchLightningLimitsAsync(BindingLiquidSdk sdk, CancellationToken ct = default);

        /// <summary>
        /// Fetches on-chain payment limits.
        /// </summary>
        Task<OnchainPaymentLimitsResponse> FetchOnchainLimitsAsync(BindingLiquidSdk sdk, CancellationToken ct = default);

        /// <summary>
        /// Prepare a send payment operation.
        /// </summary>
        Task<PrepareSendResponse> PrepareSendPaymentAsync(BindingLiquidSdk sdk, PrepareSendRequest request, CancellationToken ct = default);

        /// <summary>
        /// Execute a previously prepared send payment.
        /// </summary>
        Task<SendPaymentResponse> SendPaymentAsync(BindingLiquidSdk sdk, SendPaymentRequest request, CancellationToken ct = default);

        /// <summary>
        /// List payments with optional filtering.
        /// </summary>
        Task<List<Payment>> ListPaymentsAsync(BindingLiquidSdk sdk, ListPaymentsRequest request, CancellationToken ct = default);

        /// <summary>
        /// Gets a payment by hash or id.
        /// </summary>
        Task<Payment?> GetPaymentAsync(BindingLiquidSdk sdk, GetPaymentRequest request, CancellationToken ct = default);

        /// <summary>
        /// Lists refundable swaps.
        /// </summary>
        Task<List<RefundableSwap>> ListRefundablesAsync(BindingLiquidSdk sdk, CancellationToken ct = default);

        /// <summary>
        /// Gets recommended on-chain fees.
        /// </summary>
        Task<RecommendedFees> RecommendedFeesAsync(BindingLiquidSdk sdk, CancellationToken ct = default);

        /// <summary>
        /// Performs a refund for a refundable swap.
        /// </summary>
        Task<RefundResponse> RefundAsync(BindingLiquidSdk sdk, RefundRequest request, CancellationToken ct = default);

        /// <summary>
        /// Rescans on-chain swaps.
        /// </summary>
        Task RescanOnchainSwapsAsync(BindingLiquidSdk sdk, CancellationToken ct = default);

        /// <summary>
        /// Fetches proposed fees for pending amountless payments.
        /// </summary>
        Task<FetchPaymentProposedFeesResponse> FetchPaymentProposedFeesAsync(BindingLiquidSdk sdk, FetchPaymentProposedFeesRequest request, CancellationToken ct = default);

        /// <summary>
        /// Accepts proposed fees for pending amountless payments.
        /// </summary>
        Task AcceptPaymentProposedFeesAsync(BindingLiquidSdk sdk, AcceptPaymentProposedFeesRequest request, CancellationToken ct = default);
    }
}
