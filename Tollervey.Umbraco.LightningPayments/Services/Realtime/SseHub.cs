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
 if (!_clientsBySession.TryGetValue(sessionId, out var bucket) || bucket.Count ==0) return;
 var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
 var frame = $"event: {@event}\n" + $"data: {json}\n\n";
 foreach (var kvp in bucket)
 {
 _ = kvp.Value.Outbound.Writer.WriteAsync(frame);
 }
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
