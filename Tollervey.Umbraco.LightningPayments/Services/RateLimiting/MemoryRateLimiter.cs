using System.Collections.Concurrent;

namespace Tollervey.Umbraco.LightningPayments.UI.Services.RateLimiting
{
 /// <summary>
 /// Simple in-memory, sliding-window rate limiter. Suitable for single-node deployments.
 /// Key format: arbitrary; recommended to combine IP + session + endpoint alias.
 /// </summary>
 public class MemoryRateLimiter : IRateLimiter
 {
 private readonly ConcurrentDictionary<string, Counter> _buckets = new();

 private sealed class Counter
 {
 public int Count;
 public DateTimeOffset WindowStart;
 }

 public bool TryConsume(string bucketKey, int limit, TimeSpan window, out TimeSpan retryAfter)
 {
 retryAfter = TimeSpan.Zero;
 var now = DateTimeOffset.UtcNow;
 var c = _buckets.GetOrAdd(bucketKey, _ => new Counter { Count =0, WindowStart = now });
 lock (c)
 {
 // Reset window if expired
 if (now - c.WindowStart >= window)
 {
 c.Count =0;
 c.WindowStart = now;
 }

 if (c.Count >= limit)
 {
 retryAfter = (c.WindowStart + window) - now;
 return false;
 }

 c.Count++;
 return true;
 }
 }
 }
}
