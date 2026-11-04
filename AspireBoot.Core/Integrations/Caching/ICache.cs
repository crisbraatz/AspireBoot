using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Integrations.Caching;

public interface ICache
{
    Task<Result> AddToSetAsync(string key, string value, TimeSpan timeToLive, CancellationToken cancellationToken);
    Task<Result<string?>> GetAndRemoveAsync(string key, CancellationToken cancellationToken);
    Task<Result<string?>> GetAsync(string key, CancellationToken cancellationToken);
    Task<Result<IReadOnlySet<string>>> GetSetAsync(string key, CancellationToken cancellationToken);
    Task<Result> RemoveAsync(string key, CancellationToken cancellationToken);
    Task<Result> RemoveFromSetAsync(string key, string value, CancellationToken cancellationToken);
    Task<Result> SetAsync(string key, string value, TimeSpan timeToLive, CancellationToken cancellationToken);
}
