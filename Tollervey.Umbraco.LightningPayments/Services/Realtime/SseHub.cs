using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;

namespace Tollervey.Umbraco.LightningPayments.UI.Services.Realtime
{
 public sealed class SseHub
 {
 public sealed class SseClient
 {
 public Guid Id { get; } = Guid.NewGuid();
 public Channel<string> Outbound { get; } = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
 }

 private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, SseClient>> _clientsBySession = new();

 public SseClient AddClient(string sessionId)
 {
 var client = new SseClient();
 var bucket = _clientsBySession.GetOrAdd(sessionId, _ => new ConcurrentDictionary<Guid, SseClient>());
 bucket[client.Id] = client;
 return client;
 }

 public void RemoveClient(string sessionId, Guid clientId)
 {
 if (_clientsBySession.TryGetValue(sessionId, out var bucket))
 {
 bucket.TryRemove(clientId, out _);
 if (bucket.IsEmpty)
 {
 _clientsBySession.TryRemove(sessionId, out _);
 }
 }
 }

 public void Broadcast(string sessionId, string @event, object payload)
 {
 if (sessionId == "*") { BroadcastAll(@event, payload); return; }
 if (!_clientsBySession.TryGetValue(sessionId, out var bucket) || bucket.Count ==0) return;
 var frame = BuildFrame(@event, payload);
 foreach (var kvp in bucket) { _ = kvp.Value.Outbound.Writer.WriteAsync(frame); }
 }

 public void BroadcastAll(string @event, object payload)
 {
 var frame = BuildFrame(@event, payload);
 foreach (var bucket in _clientsBySession.Values)
 {
 foreach (var kvp in bucket) { _ = kvp.Value.Outbound.Writer.WriteAsync(frame); }
 }
 }

 private static string BuildFrame(string @event, object payload)
 {
 var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
 return $"event: {@event}\n" + $"data: {json}\n\n";
 }

 public static async Task WriteStreamAsync(Microsoft.AspNetCore.Http.HttpResponse response, ChannelReader<string> reader, CancellationToken ct)
 {
 await foreach (var message in reader.ReadAllAsync(ct))
 {
 await response.WriteAsync(message, ct);
 await response.Body.FlushAsync(ct);
 }
 }
 }
}
