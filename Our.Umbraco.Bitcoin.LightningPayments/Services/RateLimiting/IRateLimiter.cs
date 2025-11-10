using System;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services.RateLimiting
{
 public interface IRateLimiter
 {
 /// <summary>
 /// Attempts to consume a single permit from the given bucket within the provided window.
 /// Returns false if the limit has been exceeded; retryAfter will contain the time until reset.
 /// </summary>
 bool TryConsume(string bucketKey, int limit, TimeSpan window, out TimeSpan retryAfter);
 }
}

