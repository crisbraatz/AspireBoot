using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Integrations.ObjectStorage;

public interface IObjectStorage
{
    Task<Result<string>> CreateUrlAsync(
        string objectKey, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken);
    Task<Result<bool>> ExistsAsync(string objectKey, CancellationToken cancellationToken);

    Task<Result<ListStoredObjectResult>> ListAsync(
        string prefix, string? continuationToken = null, CancellationToken cancellationToken = default);

    Task<Result> PutAsync(
        string objectKey, ReadOnlyMemory<byte> content, string contentType, CancellationToken cancellationToken);
}
