using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Core.Services;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public interface IRuntimeSettingsService
    {
        Task<RuntimeFeatureFlags> GetAsync(CancellationToken ct = default);
        Task SaveAsync(RuntimeFeatureFlags flags, CancellationToken ct = default);
    }

    internal sealed class RuntimeSettingsService : IRuntimeSettingsService
    {
        private const string Key = "LightningPayments:RuntimeFlags";
        private readonly IKeyValueService _kv;
        private readonly IMemoryCache _cache;

        public RuntimeSettingsService(IKeyValueService kv, IMemoryCache cache)
        {
            _kv = kv;
            _cache = cache;
        }

        public Task<RuntimeFeatureFlags> GetAsync(CancellationToken ct = default)
        {
            if (_cache.TryGetValue(Key, out RuntimeFeatureFlags cached) && cached != null)
                return Task.FromResult(cached);

            var json = _kv.GetValue(Key);
            var flags = string.IsNullOrWhiteSpace(json)
                ? new RuntimeFeatureFlags()
                : JsonSerializer.Deserialize<RuntimeFeatureFlags>(json) ?? new RuntimeFeatureFlags();

            _cache.Set(Key, flags, TimeSpan.FromMinutes(1));
            return Task.FromResult(flags);
        }

        public Task SaveAsync(RuntimeFeatureFlags flags, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(flags);
            _kv.SetValue(Key, json);
            _cache.Remove(Key);
            return Task.CompletedTask;
        }
    }
}