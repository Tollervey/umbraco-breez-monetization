using System.Threading;
using System.Threading.Tasks;
using Breez.Sdk.Liquid;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
 /// <summary>
 /// Provides access to the connected Breez SDK handle for advanced operations
 /// (send, parse, history, limits) without exposing the raw SDK to UI code.
 /// Implemented by the concrete SDK services (online/offline).
 /// </summary>
 public interface IBreezSdkHandleProvider
 {
 /// <summary>
 /// Returns the connected SDK instance or null if not connected/available.
 /// </summary>
 Task<BindingLiquidSdk?> GetSdkAsync(CancellationToken ct = default);
 }
}

