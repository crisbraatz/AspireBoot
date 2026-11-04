using System.Diagnostics;
using System.Net;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.ObjectStorage;
using AspireBoot.Infrastructure.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspireBoot.Infrastructure.Integrations.Rustfs;

public class RustfsObjectStorage(
    IAmazonS3 amazonS3,
    [FromKeyedServices("Rustfs")] IAmazonS3 rustfsAmazonS3,
    RustfsOptions rustfsOptions,
    ILogger<RustfsObjectStorage> logger)
    : IObjectStorage, IDisposable
{
    private bool _bucketExists;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public async Task<Result<string>> CreateUrlAsync(
        string objectKey, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
    {
        Result result = ValidateObjectKey(objectKey);

        if (!result.IsSuccess)
            return Result<string>.Failure(result.Failures!);

        timeToLive ??= TimeSpan.FromDays(rustfsOptions.UrlExpiresInDays);

        if (timeToLive.Value.TotalSeconds < 1 || timeToLive.Value.TotalDays > 7)
            return Result<string>.Failure(
            [
                Failure.Validation(
                    "object_storage.validation",
                    "Time to live must be between 1 second and 7 days.",
                    "timeToLive")
            ]);

        using DependencyTrace dependencyTrace = new(DependencySources.ObjectStorage, "rustfs", "create_url");

        dependencyTrace.Activity?.SetTag("object_storage.bucket", rustfsOptions.BucketName);
        dependencyTrace.Activity?.SetTag("object_storage.url_expires_in_days", timeToLive.Value.TotalDays);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string url = await rustfsAmazonS3
                .GetPreSignedURLAsync(new GetPreSignedUrlRequest
                {
                    BucketName = rustfsOptions.BucketName,
                    Expires = DateTime.UtcNow.AddSeconds((int)timeToLive.Value.TotalSeconds),
                    Key = objectKey,
                    Protocol = new Uri(rustfsOptions.PublicEndpoint).Scheme is "https"
                        ? Protocol.HTTPS
                        : Protocol.HTTP,
                    Verb = HttpVerb.GET
                })
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            dependencyTrace.Activity?.SetStatus(ActivityStatusCode.Ok);

            return Result<string>.Success(url);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (Exception exception) when
            (exception is
                 AmazonClientException or
                 AmazonServiceException or
                 HttpRequestException or
                 IOException or
                 OperationCanceledException)
        {
            return DependencyFailure<string>(dependencyTrace, exception, "create_url");
        }
    }

    public async Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        Result result = ValidateObjectKey(objectKey);

        if (!result.IsSuccess)
            return result;

        using DependencyTrace dependencyTrace = new(DependencySources.ObjectStorage, "rustfs", "delete");

        dependencyTrace.Activity?.SetTag("object_storage.bucket", rustfsOptions.BucketName);

        try
        {
            await amazonS3
                .DeleteObjectAsync(
                    new DeleteObjectRequest { BucketName = rustfsOptions.BucketName, Key = objectKey },
                    cancellationToken)
                .ConfigureAwait(false);

            dependencyTrace.Activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (Exception exception) when
            (exception is
                 AmazonClientException or
                 AmazonServiceException or
                 HttpRequestException or
                 IOException or
                 OperationCanceledException)
        {
            return DependencyFailure(dependencyTrace, exception, "delete");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task<Result<bool>> ExistsAsync(string objectKey, CancellationToken cancellationToken)
    {
        Result result = ValidateObjectKey(objectKey);

        if (!result.IsSuccess)
            return Result<bool>.Failure(result.Failures!);

        using DependencyTrace dependencyTrace = new(DependencySources.ObjectStorage, "rustfs", "exists");

        dependencyTrace.Activity?.SetTag("object_storage.bucket", rustfsOptions.BucketName);

        try
        {
            bool exists;

            try
            {
                await amazonS3
                    .GetObjectMetadataAsync(
                        new GetObjectMetadataRequest { BucketName = rustfsOptions.BucketName, Key = objectKey },
                        cancellationToken)
                    .ConfigureAwait(false);

                exists = true;
            }
            catch (AmazonS3Exception exception) when
                (exception.StatusCode is HttpStatusCode.NotFound && exception.ErrorCode is not "NoSuchBucket")
            {
                await amazonS3
                    .HeadBucketAsync(new HeadBucketRequest { BucketName = rustfsOptions.BucketName }, cancellationToken)
                    .ConfigureAwait(false);

                exists = false;
            }

            dependencyTrace.Activity?.SetTag("object_storage.object.exists", exists);
            dependencyTrace.Activity?.SetStatus(ActivityStatusCode.Ok);

            return Result<bool>.Success(exists);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (Exception exception) when
            (exception is
                 AmazonClientException or
                 AmazonServiceException or
                 HttpRequestException or
                 IOException or
                 OperationCanceledException)
        {
            return DependencyFailure<bool>(dependencyTrace, exception, "exists");
        }
    }

    public async Task<Result<ListStoredObjectResult>> ListAsync(
        string prefix, string? continuationToken = null, CancellationToken cancellationToken = default)
    {
        Result validation = ValidateObjectKey(prefix.TrimEnd('/'));

        if (!validation.IsSuccess)
            return Result<ListStoredObjectResult>.Failure(validation.Failures!);

        using DependencyTrace dependencyTrace = new(DependencySources.ObjectStorage, "rustfs", "list");

        dependencyTrace.Activity?.SetTag("object_storage.bucket", rustfsOptions.BucketName);

        try
        {
            ListObjectsV2Response response = await amazonS3
                .ListObjectsV2Async(
                    new ListObjectsV2Request
                    {
                        BucketName = rustfsOptions.BucketName,
                        ContinuationToken = continuationToken,
                        MaxKeys = 1000,
                        Prefix = prefix
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            List<ListStoredObjectItemResult> items = [];

            foreach (S3Object item in response.S3Objects ?? [])
                if (item.LastModified is { } lastModified)
                    items.Add(new ListStoredObjectItemResult(
                        item.Key, new DateTimeOffset(DateTime.SpecifyKind(lastModified, DateTimeKind.Utc))));

            string? next = response.IsTruncated is true ? response.NextContinuationToken : null;

            if (response.IsTruncated is true && string.IsNullOrEmpty(next))
                throw new InvalidOperationException("Object storage did not return a continuation token.");

            dependencyTrace.Activity?.SetStatus(ActivityStatusCode.Ok);

            return Result<ListStoredObjectResult>.Success(new ListStoredObjectResult(items, next));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (Exception exception) when
            (exception is
                 AmazonClientException or
                 AmazonServiceException or
                 HttpRequestException or
                 IOException or
                 OperationCanceledException)
        {
            return DependencyFailure<ListStoredObjectResult>(dependencyTrace, exception, "list");
        }
    }

    public async Task<Result> PutAsync(
        string objectKey, ReadOnlyMemory<byte> content, string contentType, CancellationToken cancellationToken)
    {
        Result result = ValidateObjectKey(objectKey);

        if (!result.IsSuccess)
            return result;

        using DependencyTrace dependencyTrace = new(DependencySources.ObjectStorage, "rustfs", "put");

        dependencyTrace.Activity?.SetTag("object_storage.bucket", rustfsOptions.BucketName);
        dependencyTrace.Activity?.SetTag("object_storage.object.content_type", contentType);
        dependencyTrace.Activity?.SetTag("object_storage.object.size_bytes", content.Length);

        try
        {
            if (rustfsOptions.CreateBucketIfMissing)
                await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);

            using MemoryStream memoryStream = new(content.ToArray(), false);

            await amazonS3
                .PutObjectAsync(
                    new PutObjectRequest
                    {
                        BucketName = rustfsOptions.BucketName,
                        ContentType = contentType,
                        InputStream = memoryStream,
                        Key = objectKey,
                        UseChunkEncoding = false
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            dependencyTrace.Activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            dependencyTrace.Cancel();

            throw;
        }
        catch (Exception exception) when
            (exception is
                 AmazonClientException or
                 AmazonServiceException or
                 HttpRequestException or
                 IOException or
                 OperationCanceledException)
        {
            return DependencyFailure(dependencyTrace, exception, "put");
        }
    }

    protected virtual void Dispose(bool dispose)
    {
        if (dispose)
            _semaphoreSlim.Dispose();
    }

    private Result DependencyFailure(DependencyTrace dependencyTrace, Exception exception, string method)
    {
        dependencyTrace.Fail(exception.GetType().Name, "RustFS integration failed.");
        DependencyLogs.LogDependencyFailure(
            logger,
            DependencyKind.ObjectStorage,
            method,
            exception.GetType().Name,
            "RustFS integration failed.",
            exception);

        return Result.Failure(
        [
            Failure.ServiceUnavailable(
                "object_storage.unavailable",
                "Object storage is temporarily unavailable.")
        ]);
    }

    private Result<T> DependencyFailure<T>(DependencyTrace dependencyTrace, Exception exception, string method)
    {
        dependencyTrace.Fail(exception.GetType().Name, "RustFS integration failed.");
        DependencyLogs.LogDependencyFailure(
            logger,
            DependencyKind.ObjectStorage,
            method,
            exception.GetType().Name,
            "RustFS integration failed.",
            exception);

        return Result<T>.Failure(
        [
            Failure.ServiceUnavailable(
                "object_storage.unavailable",
                "Object storage is temporarily unavailable.")
        ]);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_bucketExists)
            return;

        await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_bucketExists)
                return;

            try
            {
                await amazonS3
                    .HeadBucketAsync(new HeadBucketRequest { BucketName = rustfsOptions.BucketName }, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode is HttpStatusCode.NotFound)
            {
                try
                {
                    await amazonS3
                        .PutBucketAsync(
                            new PutBucketRequest { BucketName = rustfsOptions.BucketName, UseClientRegion = true },
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (AmazonS3Exception amazonS3Exception) when
                    (amazonS3Exception.ErrorCode is "BucketAlreadyOwnedByYou")
                {
                    // BucketAlreadyOwnedByYou
                }
            }

            _bucketExists = true;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private static Result ValidateObjectKey(string? objectKey = null) =>
        !string.IsNullOrWhiteSpace(objectKey) &&
        Encoding.UTF8.GetByteCount(objectKey) <= 1024 &&
        !objectKey.StartsWith('/') &&
        !objectKey.Contains('\\', StringComparison.Ordinal) &&
        objectKey.Split('/').All(segment => !string.IsNullOrWhiteSpace(segment) && segment is not ("." or ".."))
            ? Result.Success()
            : Result.Failure(
            [
                Failure.Validation(
                    "object_storage.validation",
                    "Object key must be a safe relative key.",
                    "objectKey")
            ]);
}
