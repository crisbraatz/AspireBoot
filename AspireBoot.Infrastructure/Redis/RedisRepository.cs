using Microsoft.Extensions.Caching.Distributed;

namespace AspireBoot.Infrastructure.Redis;

public class RedisRepository(IDistributedCache distributedCache) : IRedisRepository
{
    public async Task<string?> GetValueFromKeyAsync(string prefix, string key, CancellationToken token = default) =>
        await distributedCache.GetStringAsync($"{prefix}:{key}", token);

    public async Task SetKeyAsync(string prefix, string key, string value, CancellationToken token = default) =>
        await distributedCache.SetStringAsync(
            $"{prefix}:{key}",
            value,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            },
            token);

    public async Task RemoveKeyAsync(string prefix, string key, CancellationToken token = default) =>
        await distributedCache.RemoveAsync($"{prefix}:{key}", token);
}