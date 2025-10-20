using System;
using Microsoft.Extensions.Caching.Memory;

namespace Tollervey.LightningPayments.Breez.Services
{
    public class MemoryPaymentEventDeduper : IPaymentEventDeduper
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);
        private readonly object _lock = new object();

        public MemoryPaymentEventDeduper(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool TryBegin(string key)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out _))
                {
                    return false;
                }
                _cache.Set(key, true, _ttl);
                return true;
            }
        }

        public void Complete(string key)
        {
            _cache.Remove(key);
        }
    }
}