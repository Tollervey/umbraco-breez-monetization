using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Breez.Sdk.Liquid;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
 /// <summary>
 /// High-level facade exposing the SDK "send", limits, and history features
 /// through async methods that do not leak the raw SDK handle to consumers.
 ///
 /// Use this from your Umbraco UI layer to implement the Breez UX guidelines:
 /// - Unified Send: Parse ? PrepareSend ? confirm ? Send
 /// - Limits/Fee display before confirmation
 /// - History and details with separate fees/amount and clear states.
 /// </summary>
 public interface IBreezPaymentsFacade
 {
 Task<InputType?> ParseAsync(string input, CancellationToken ct = default);
 Task<LightningPaymentLimitsResponse?> FetchLightningLimitsAsync(CancellationToken ct = default);
 Task<OnchainPaymentLimitsResponse?> FetchOnchainLimitsAsync(CancellationToken ct = default);
 Task<PrepareSendResponse?> PrepareSendAsync(PrepareSendRequest request, CancellationToken ct = default);
 Task<SendPaymentResponse?> SendPaymentAsync(SendPaymentRequest request, CancellationToken ct = default);
 Task<List<Payment>> ListPaymentsAsync(ListPaymentsRequest request, CancellationToken ct = default);
 Task<Payment?> GetPaymentAsync(GetPaymentRequest request, CancellationToken ct = default);
 Task<RecommendedFees?> RecommendedFeesAsync(CancellationToken ct = default);
 Task<FetchPaymentProposedFeesResponse?> FetchPaymentProposedFeesAsync(FetchPaymentProposedFeesRequest request, CancellationToken ct = default);
 Task AcceptPaymentProposedFeesAsync(AcceptPaymentProposedFeesRequest request, CancellationToken ct = default);
 }

 internal sealed class BreezPaymentsFacade : IBreezPaymentsFacade
 {
 private readonly IBreezSdkHandleProvider _handleProvider;
 private readonly IBreezSdkWrapper _wrapper;

 public BreezPaymentsFacade(IBreezSdkHandleProvider handleProvider, IBreezSdkWrapper wrapper)
 {
 _handleProvider = handleProvider;
 _wrapper = wrapper;
 }

 public async Task<InputType?> ParseAsync(string input, CancellationToken ct = default)
 {
 var sdk = await _handleProvider.GetSdkAsync(ct);
 if (sdk == null) return null;
 return await _wrapper.ParseAsync(sdk, input, ct);
 }

 public async Task<LightningPaymentLimitsResponse?> FetchLightningLimitsAsync(CancellationToken ct = default)
 {
 var sdk = await _handleProvider.GetSdkAsync(ct);
 if (sdk == null) return null;
 return await _wrapper.FetchLightningLimitsAsync(sdk, ct);
 }

 public async Task<OnchainPaymentLimitsResponse?> FetchOnchainLimitsAsync(CancellationToken ct = default)
 {
 var sdk = await _handleProvider.GetSdkAsync(ct);
 if (sdk == null) return null;
 return await _wrapper.FetchOnchainLimitsAsync(sdk, ct);
 }

 public async Task<PrepareSendResponse?> PrepareSendAsync(PrepareSendRequest request, CancellationToken ct = default)
 {
 var sdk = await _handleProvider.GetSdkAsync(ct);
 if (sdk == null) return null;
 return await _wrapper.PrepareSendPaymentAsync(sdk, request, ct);
 }

 public async Task<SendPaymentResponse?> SendPaymentAsync(SendPaymentRequest request, CancellationToken ct = default)
 {
 var sdk = await _handleProvider.GetSdkAsync(ct);
 if (sdk == null) return null;
 return await _wrapper.SendPaymentAsync(sdk, request, ct);
 }

 public async Task<List<Payment>> ListPaymentsAsync(ListPaymentsRequest request, CancellationToken ct = default)
 {
 var sdk = await _handleProvider.GetSdkAsync(ct);
 if (sdk == null) return new List<Payment>();
 return await _wrapper.ListPaymentsAsync(sdk, request, ct);
 }

 public async Task<Payment?> GetPaymentAsync(GetPaymentRequest request, CancellationToken ct = default)
 {
 var sdk = await _handleProvider.GetSdkAsync(ct);
 if (sdk == null) return null;
 return await _wrapper.GetPaymentAsync(sdk, request, ct);
 }

 public async Task<RecommendedFees?> RecommendedFeesAsync(CancellationToken ct = default)
 {
 var sdk = await _handleProvider.GetSdkAsync(ct);
 if (sdk == null) return null;
 return await _wrapper.RecommendedFeesAsync(sdk, ct);
 }

 public async Task<FetchPaymentProposedFeesResponse?> FetchPaymentProposedFeesAsync(FetchPaymentProposedFeesRequest request, CancellationToken ct = default)
 {
 var sdk = await _handleProvider.GetSdkAsync(ct);
 if (sdk == null) return null;
 return await _wrapper.FetchPaymentProposedFeesAsync(sdk, request, ct);
 }

 public async Task AcceptPaymentProposedFeesAsync(AcceptPaymentProposedFeesRequest request, CancellationToken ct = default)
 {
 var sdk = await _handleProvider.GetSdkAsync(ct);
 if (sdk == null) return; // no-op if not connected
 await _wrapper.AcceptPaymentProposedFeesAsync(sdk, request, ct);
 }
 }
}
