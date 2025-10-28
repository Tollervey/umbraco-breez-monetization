using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tollervey.Umbraco.LightningPayments.UI.Services.Realtime
{
 /// <summary>
 /// Minimal in-memory Server-Sent Events hub keyed by session id.
 /// Allows broadcasting payment updates to all clients in a session.
 /// </summary>
 public sealed class SseHub
 {
 /// <summary>
 /// Represents a connected SSE client with an outbound message channel.
 /// </summary>
 public sealed class SseClient
 {
 /// <summary>
 /// Gets the unique client identifier.
 /// </summary>
 public Guid Id { get; } = Guid.NewGuid();
 /// <summary>
 /// Gets the outbound channel for SSE frames.
 /// </summary>
 public Channel<string> Outbound { get; } = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

 /// <summary>
 /// Try to gracefully complete the outbound writer.
 /// </summary>
 public void Complete() => Outbound.Writer.TryComplete();
 }

 private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, SseClient>> _clientsBySession = new();

 /// <summary>
 /// Adds a client to the given session's bucket and returns the client handle.
 /// </summary>
 public SseClient AddClient(string sessionId)
 {
 var client = new SseClient();
 var bucket = _clientsBySession.GetOrAdd(sessionId, _ => new ConcurrentDictionary<Guid, SseClient>());
 bucket[client.Id] = client;
 return client;
 }

 /// <summary>
 /// Removes a client from a session, cleaning up empty buckets.
 /// </summary>
 public void RemoveClient(string sessionId, Guid clientId)
 {
 if (_clientsBySession.TryGetValue(sessionId, out var bucket))
 {
 if (bucket.TryRemove(clientId, out var client))
 {
 try { client?.Complete(); } catch { /* ignore */ }
 }
 if (bucket.IsEmpty)
 {
 _clientsBySession.TryRemove(sessionId, out _);
 }
 }
 }

 /// <summary>
 /// Broadcasts an event and payload to all clients in the specified session.
 /// Use "*" to broadcast to all sessions.
 /// </summary>
 public void Broadcast(string sessionId, string @event, object payload)
 {
 if (sessionId == "*") { BroadcastAll(@event, payload); return; }
 if (!_clientsBySession.TryGetValue(sessionId, out var bucket) || bucket.Count ==0) return;
 var frame = BuildFrame(@event, payload);
 foreach (var kvp in bucket)
 {
 var client = kvp.Value;
 // TryWrite avoids queuing to closed writers. If it fails, remove the client.
 if (!client.Outbound.Writer.TryWrite(frame))
 {
 RemoveClient(sessionId, client.Id);
 }
 }
 }

 /// <summary>
 /// Broadcasts an event and payload to all connected sessions.
 /// </summary>
 public void BroadcastAll(string @event, object payload)
 {
 var frame = BuildFrame(@event, payload);
 foreach (var session in _clientsBySession)
 {
 var sessionId = session.Key;
 var bucket = session.Value;
 foreach (var kvp in bucket)
 {
 var client = kvp.Value;
 if (!client.Outbound.Writer.TryWrite(frame))
 {
 RemoveClient(sessionId, client.Id);
 }
 }
 }
 }

 /// <summary>
 /// Send a lightweight SSE comment heartbeat to keep connections alive.
 /// </summary>
 public void SendHeartbeat(string sessionId)
 {
 if (!_clientsBySession.TryGetValue(sessionId, out var bucket) || bucket.Count ==0) return;
 const string heartbeat = ":\n\n"; // SSE comment to keep connection alive
 foreach (var kvp in bucket)
 {
 var client = kvp.Value;
 if (!client.Outbound.Writer.TryWrite(heartbeat))
 {
 RemoveClient(sessionId, client.Id);
 }
 }
 }

 /// <summary>
 /// Send heartbeat to all sessions.
 /// </summary>
 public void SendHeartbeatAll()
 {
 const string heartbeat = ":\n\n";
 foreach (var session in _clientsBySession)
 {
 var sessionId = session.Key;
 var bucket = session.Value;
 foreach (var kvp in bucket)
 {
 var client = kvp.Value;
 if (!client.Outbound.Writer.TryWrite(heartbeat))
 {
 RemoveClient(sessionId, client.Id);
 }
 }
 }
 }

 private static string BuildFrame(string @event, object payload)
 {
 var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
 return $"event: {@event}\n" + $"data: {json}\n\n";
 }

 /// <summary>
 /// Writes a continuous SSE stream for the given channel reader to the HTTP response.
 /// </summary>
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
