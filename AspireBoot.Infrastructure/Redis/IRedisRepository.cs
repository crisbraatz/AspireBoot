namespace AspireBoot.Infrastructure.Redis;

public interface IRedisRepository
{
    Task<string?> GetValueFromKeyAsync(string prefix, string key, CancellationToken cancellationToken = default);

    Task SetKeyAsync(
        string prefix,
        string key,
        string value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    Task RemoveKeyAsync(string prefix, string key, CancellationToken cancellationToken = default);
}
