using Microsoft.Extensions.Caching.Distributed;

namespace AspireBoot.Infrastructure.Redis;

public class RedisRepository(IDistributedCache distributedCache) : IRedisRepository
{
    public async Task<string?> GetValueFromKeyAsync(
        string prefix, string key, CancellationToken cancellationToken = default) =>
        await distributedCache.GetStringAsync($"{prefix}:{key}", cancellationToken).ConfigureAwait(false);

    public async Task SetKeyAsync(
        string prefix,
        string key,
        string value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) =>
        await distributedCache
            .SetStringAsync(
                $"{prefix}:{key}",
                value,
                expiration is null
                    ? new DistributedCacheEntryOptions()
                    : new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration },
                cancellationToken)
            .ConfigureAwait(false);

    public async Task RemoveKeyAsync(string prefix, string key, CancellationToken cancellationToken = default) =>
        await distributedCache.RemoveAsync($"{prefix}:{key}", cancellationToken).ConfigureAwait(false);
}
