using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.ObjectStorage;
using AspireBoot.Infrastructure.Integrations.Rustfs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AspireBoot.Tests.Integration.Infrastructure.Integrations.Rustfs;

[Trait("Category", "Integration")]
public class RustfsObjectStorageTests(TestFixture testFixture) : TestBase(testFixture)
{
    private static readonly HttpClient s_httpClient = new();

    [Theory]
    [InlineData(1)]
    [InlineData(604800)]
    public async Task CreateUrlAsyncShouldReturnSuccessWhenUsingTheSupportedSignedUrlLifetimeBoundaries(int seconds)
    {
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage();

        Result<string> result = await rustfsObjectStorage.CreateUrlAsync("integration/file.txt", TimeSpan.FromSeconds(seconds), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains("X-Amz-Expires=" + seconds.ToString(System.Globalization.CultureInfo.InvariantCulture), result.Value, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(604801)]
    public async Task CreateUrlAsyncShouldReturnValidationFailureWhenSignedUrlLifetimesOutsideTheSupportedRangeAreProvided(double seconds)
    {
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage();

        Result<string> result = await rustfsObjectStorage.CreateUrlAsync("integration/file.txt", TimeSpan.FromSeconds(seconds), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Failure failure = Assert.Single(result.Failures!);
        Assert.Equal(FailureKind.Validation, failure.FailureKind);
        Assert.Equal("timeToLive", failure.Field);
    }

    [Fact]
    public async Task CreateUrlAsyncShouldReturnASignedUrlRejectedByTheServerWhenCredentialsAreInvalid()
    {
        using AmazonS3Client amazonS3Client = CreateUnauthorizedAmazonS3Client();
        using RustfsObjectStorage rustfsObjectStorage = new(amazonS3Client, amazonS3Client, GetRustfsOptions(), NullLogger<RustfsObjectStorage>.Instance);

        Result<string> result = await rustfsObjectStorage.CreateUrlAsync("integration/file.txt", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        using HttpResponseMessage response = await s_httpClient.GetAsync(new Uri(result.Value!), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("/absolute")]
    [InlineData("directory\\file")]
    [InlineData("a/../b")]
    [InlineData("a/./b")]
    [InlineData("a//b")]
    [InlineData("a/ /b")]
    [InlineData("too-long")]
    [InlineData("unicode-too-long")]
    public async Task CreateUrlAsyncAndDeleteAsyncAndExistsAsyncAndListAsyncAndPutAsyncShouldReturnValidationFailuresWhenTheObjectKeyIsUnsafe(string key)
    {
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage();
        key = key switch { "too-long" => new string('a', 1025), "unicode-too-long" => new string('é', 513), _ => key };

        Result[] results = [await rustfsObjectStorage.CreateUrlAsync(key, cancellationToken: TestContext.Current.CancellationToken), await rustfsObjectStorage.DeleteAsync(key, TestContext.Current.CancellationToken), await rustfsObjectStorage.ExistsAsync(key, TestContext.Current.CancellationToken), await rustfsObjectStorage.ListAsync(key, cancellationToken: TestContext.Current.CancellationToken), await rustfsObjectStorage.PutAsync(key, new byte[] { 1 }, "text/plain", TestContext.Current.CancellationToken)];

        Assert.All(results, result =>
        {
            Assert.False(result.IsSuccess);
            Failure failure = Assert.Single(result.Failures!);
            Assert.Equal(FailureKind.Validation, failure.FailureKind);
            Assert.Equal("objectKey", failure.Field);
        });
    }

    [Fact]
    public async Task DeleteAsyncShouldReturnSuccessWhenDeletingAnObjectAndAllowingDeletingItAgain()
    {
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage();
        string key = $"integration/{Guid.NewGuid():N}/file.txt";
        Assert.True((await rustfsObjectStorage.PutAsync(key, new byte[] { 1 }, "text/plain", TestContext.Current.CancellationToken)).IsSuccess);

        Result result = await rustfsObjectStorage.DeleteAsync(key, TestContext.Current.CancellationToken);
        Result repeatedResult = await rustfsObjectStorage.DeleteAsync(key, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(repeatedResult.IsSuccess);
        Assert.False((await rustfsObjectStorage.ExistsAsync(key, TestContext.Current.CancellationToken)).Value);
    }

    [Theory]
    [InlineData("put")]
    [InlineData("exists")]
    [InlineData("list")]
    [InlineData("delete")]
    public async Task ExecuteAsyncShouldReturnServiceUnavailableFailureWhenTheBucketIsMissing(string operation)
    {
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage("missing-" + Guid.NewGuid().ToString("N"));

        Result result = await ExecuteAsync(rustfsObjectStorage, operation, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Failure failure = Assert.Single(result.Failures!);
        Assert.Equal(FailureKind.ServiceUnavailable, failure.FailureKind);
        Assert.Equal("object_storage.unavailable", failure.Code);
    }

    [Theory]
    [InlineData("put")]
    [InlineData("exists")]
    [InlineData("list")]
    [InlineData("delete")]
    public async Task ExecuteAsyncShouldThrowOperationCanceledExceptionWhenCancellationIsRequestedWhileRustfsIsPaused(string operation)
    {
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage();
        using CancellationTokenSource cancellationTokenSource = new();
        await TestFixture.RustfsContainer.PauseAsync(TestContext.Current.CancellationToken);
        try
        {
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ExecuteAsync(rustfsObjectStorage, operation, cancellationTokenSource.Token));
            Assert.True(cancellationTokenSource.IsCancellationRequested);
        }
        finally
        {
            await TestFixture.RustfsContainer.UnpauseAsync(CancellationToken.None);
        }
    }

    [Theory]
    [InlineData("put")]
    [InlineData("exists")]
    [InlineData("list")]
    [InlineData("delete")]
    public async Task ExecuteAsyncShouldReturnServiceUnavailableFailureWhenCredentialsAreRejected(string operation)
    {
        using AmazonS3Client amazonS3Client = CreateUnauthorizedAmazonS3Client();
        using RustfsObjectStorage rustfsObjectStorage = new(amazonS3Client, amazonS3Client, GetRustfsOptions(), NullLogger<RustfsObjectStorage>.Instance);

        Result result = await ExecuteAsync(rustfsObjectStorage, operation, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Failure failure = Assert.Single(result.Failures!);
        Assert.Equal(FailureKind.ServiceUnavailable, failure.FailureKind);
        Assert.Equal("object_storage.unavailable", failure.Code);
        Assert.Equal("Object storage is temporarily unavailable.", failure.Message);
    }

    [Fact]
    public async Task ListAsyncShouldReturnSuccessWhenListingOnlyTheRequestedPrefixWithUtcTimestamps()
    {
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage();
        string prefix = $"integration/{Guid.NewGuid():N}/";
        string key = prefix + "inside.txt";
        string otherKey = $"integration/{Guid.NewGuid():N}/outside.txt";
        Assert.True((await rustfsObjectStorage.PutAsync(key, new byte[] { 1 }, "text/plain", TestContext.Current.CancellationToken)).IsSuccess);
        Assert.True((await rustfsObjectStorage.PutAsync(otherKey, new byte[] { 2 }, "text/plain", TestContext.Current.CancellationToken)).IsSuccess);

        Result<ListStoredObjectResult> result = await rustfsObjectStorage.ListAsync(prefix, cancellationToken: TestContext.Current.CancellationToken);

        try
        {
            Assert.True(result.IsSuccess);
            ListStoredObjectItemResult itemListStoredObjectItemResult = Assert.Single(result.Value!.Items);
            Assert.Equal(key, itemListStoredObjectItemResult.Key);
            Assert.Equal(TimeSpan.Zero, itemListStoredObjectItemResult.LastModifiedAtUtc.Offset);
            Assert.InRange(itemListStoredObjectItemResult.LastModifiedAtUtc, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow);
            Assert.Null(result.Value.ContinuationToken);
        }
        finally
        {
            await rustfsObjectStorage.DeleteAsync(key, TestContext.Current.CancellationToken);
            await rustfsObjectStorage.DeleteAsync(otherKey, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ListAsyncShouldReturnAllObjectsAcrossMultiplePages()
    {
        string bucketName = "tests-" + Guid.NewGuid().ToString("N");
        IAmazonS3 amazonS3 = GetAmazonS3();
        await amazonS3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName }, TestContext.Current.CancellationToken);
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage(bucketName);
        try
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, 1001), new ParallelOptions { MaxDegreeOfParallelism = 16, CancellationToken = TestContext.Current.CancellationToken }, async (index, cancellationToken) =>
            {
                Result result = await rustfsObjectStorage.PutAsync($"page/{index:D4}.txt", new byte[] { 1 }, "text/plain", cancellationToken);
                Assert.True(result.IsSuccess);
            });

            Result<ListStoredObjectResult> firstPageResult = await rustfsObjectStorage.ListAsync("page/", cancellationToken: TestContext.Current.CancellationToken);

            Result<ListStoredObjectResult> secondPageResult = await rustfsObjectStorage.ListAsync("page/", firstPageResult.Value!.ContinuationToken, TestContext.Current.CancellationToken);
            Assert.True(firstPageResult.IsSuccess);
            Assert.Equal(1000, firstPageResult.Value.Items.Count);
            Assert.NotNull(firstPageResult.Value.ContinuationToken);
            Assert.True(secondPageResult.IsSuccess);
            Assert.Single(secondPageResult.Value!.Items);
            Assert.Null(secondPageResult.Value.ContinuationToken);
            Assert.Equal(1001, firstPageResult.Value.Items.Concat(secondPageResult.Value.Items).Select(itemListStoredObjectItemResult => itemListStoredObjectItemResult.Key).Distinct(StringComparer.Ordinal).Count());
        }
        finally
        {
            foreach (int[] batch in Enumerable.Range(0, 1001).Chunk(1000))
                await amazonS3.DeleteObjectsAsync(new DeleteObjectsRequest { BucketName = bucketName, Objects = [.. batch.Select(index => new KeyVersion { Key = $"page/{index:D4}.txt" })] }, TestContext.Current.CancellationToken);
            await amazonS3.DeleteBucketAsync(bucketName, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ListAsyncAndExistsAsyncShouldReturnAnEmptyPageAndFalseForMissingObjects()
    {
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage();
        string key = $"integration/{Guid.NewGuid():N}/missing";

        Result<ListStoredObjectResult> listedObjectsResult = await rustfsObjectStorage.ListAsync(key, cancellationToken: TestContext.Current.CancellationToken);
        Result<bool> existsResult = await rustfsObjectStorage.ExistsAsync(key, TestContext.Current.CancellationToken);

        Assert.True(listedObjectsResult.IsSuccess);
        Assert.Empty(listedObjectsResult.Value!.Items);
        Assert.Null(listedObjectsResult.Value.ContinuationToken);
        Assert.True(existsResult.IsSuccess);
        Assert.False(existsResult.Value);
    }

    [Fact]
    public async Task PutAsyncShouldReturnSuccessWhenStoringContentAndReadingItThroughASignedUrl()
    {
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage();
        string key = $"integration/{Guid.NewGuid():N}/file.txt";
        byte[] content = [.. "Stored content."u8];

        Result result = await rustfsObjectStorage.PutAsync(key, content, "text/plain", TestContext.Current.CancellationToken);

        try
        {
            Assert.True(result.IsSuccess);
            Assert.True((await rustfsObjectStorage.ExistsAsync(key, TestContext.Current.CancellationToken)).Value);
            Result<string> urlResult = await rustfsObjectStorage.CreateUrlAsync(key, cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(urlResult.IsSuccess);
            using HttpResponseMessage response = await s_httpClient.GetAsync(new Uri(urlResult.Value!), TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType!.MediaType);
            Assert.Equal(content, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            await rustfsObjectStorage.DeleteAsync(key, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task PutAsyncShouldReturnSuccessWhenTheBucketExistsAndAutomaticCreationIsEnabled()
    {
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage(createBucketIfMissing: true);
        string key = $"integration/{Guid.NewGuid():N}/file.txt";

        Result result = await rustfsObjectStorage.PutAsync(key, new byte[] { 1 }, "text/plain", TestContext.Current.CancellationToken);

        try
        {
            Assert.True(result.IsSuccess);
            Assert.True((await rustfsObjectStorage.ExistsAsync(key, TestContext.Current.CancellationToken)).Value);
        }
        finally
        {
            await rustfsObjectStorage.DeleteAsync(key, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task PutAsyncShouldReturnSuccessWhenCreatingTheMissingBucketOnceForConcurrentUploads()
    {
        string bucketName = "tests-" + Guid.NewGuid().ToString("N");
        using RustfsObjectStorage rustfsObjectStorage = CreateObjectStorage(bucketName, true);

        Result[] results = await Task.WhenAll(rustfsObjectStorage.PutAsync("first.txt", new byte[] { 1 }, "text/plain", TestContext.Current.CancellationToken), rustfsObjectStorage.PutAsync("second.txt", new byte[] { 2 }, "text/plain", TestContext.Current.CancellationToken));

        try
        {
            Assert.All(results, result => Assert.True(result.IsSuccess));
            Assert.True((await rustfsObjectStorage.PutAsync("third.txt", new byte[] { 3 }, "text/plain", TestContext.Current.CancellationToken)).IsSuccess);
            Result<ListStoredObjectResult> listedObjectsResult = await rustfsObjectStorage.ListAsync("first", cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("first.txt", Assert.Single(listedObjectsResult.Value!.Items).Key);
        }
        finally
        {
            foreach (string key in new[] { "first.txt", "second.txt", "third.txt" })
                await rustfsObjectStorage.DeleteAsync(key, TestContext.Current.CancellationToken);
            await GetAmazonS3().DeleteBucketAsync(bucketName, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task PutAsyncShouldReturnSuccessWhenSeparateInstancesInitializeTheSameBucketConcurrently()
    {
        string bucketName = "tests-" + Guid.NewGuid().ToString("N");
        using RustfsObjectStorage firstRustfsObjectStorage = CreateObjectStorage(bucketName, true);
        using RustfsObjectStorage secondRustfsObjectStorage = CreateObjectStorage(bucketName, true);

        try
        {
            Result[] results = await Task.WhenAll(firstRustfsObjectStorage.PutAsync("first.txt", new byte[] { 1 }, "text/plain", TestContext.Current.CancellationToken), secondRustfsObjectStorage.PutAsync("second.txt", new byte[] { 2 }, "text/plain", TestContext.Current.CancellationToken));

            Assert.All(results, result => Assert.True(result.IsSuccess));
            Assert.True((await firstRustfsObjectStorage.ExistsAsync("first.txt", TestContext.Current.CancellationToken)).Value);
            Assert.True((await secondRustfsObjectStorage.ExistsAsync("second.txt", TestContext.Current.CancellationToken)).Value);
        }
        finally
        {
            await firstRustfsObjectStorage.DeleteAsync("first.txt", TestContext.Current.CancellationToken);
            await firstRustfsObjectStorage.DeleteAsync("second.txt", TestContext.Current.CancellationToken);
            await GetAmazonS3().DeleteBucketAsync(bucketName, TestContext.Current.CancellationToken);
        }
    }

    private RustfsObjectStorage CreateObjectStorage(string? bucketName = null, bool createBucketIfMissing = false)
    {
        RustfsOptions rustfsOptions = TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<RustfsOptions>();
        return new RustfsObjectStorage(GetAmazonS3(), TestFixture.ApiWebApplicationFactory.Services.GetRequiredKeyedService<IAmazonS3>("Rustfs"), new RustfsOptions { AccessKey = rustfsOptions.AccessKey, BucketName = bucketName ?? rustfsOptions.BucketName, CreateBucketIfMissing = createBucketIfMissing, Endpoint = rustfsOptions.Endpoint, PublicEndpoint = rustfsOptions.PublicEndpoint, ReadinessPath = rustfsOptions.ReadinessPath, Region = rustfsOptions.Region, SecretKey = rustfsOptions.SecretKey, UrlExpiresInDays = rustfsOptions.UrlExpiresInDays }, NullLogger<RustfsObjectStorage>.Instance);
    }

    private AmazonS3Client CreateUnauthorizedAmazonS3Client()
    {
        RustfsOptions rustfsOptions = GetRustfsOptions();
        return new AmazonS3Client(new BasicAWSCredentials("invalid-access-key", "invalid-secret-key"), new AmazonS3Config { AuthenticationRegion = rustfsOptions.Region, ForcePathStyle = true, MaxErrorRetry = 0, RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED, ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED, ServiceURL = rustfsOptions.Endpoint });
    }

    private static async Task<Result> ExecuteAsync(RustfsObjectStorage rustfsObjectStorage, string operation, CancellationToken cancellationToken) => operation switch { "put" => await rustfsObjectStorage.PutAsync("integration/file.txt", new byte[] { 1 }, "text/plain", cancellationToken), "exists" => await rustfsObjectStorage.ExistsAsync("integration/file.txt", cancellationToken), "list" => await rustfsObjectStorage.ListAsync("integration/", cancellationToken: cancellationToken), "delete" => await rustfsObjectStorage.DeleteAsync("integration/file.txt", cancellationToken), _ => throw new ArgumentOutOfRangeException(nameof(operation)) };

    private IAmazonS3 GetAmazonS3() => TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<IAmazonS3>();

    private RustfsOptions GetRustfsOptions() => TestFixture.ApiWebApplicationFactory.Services.GetRequiredService<RustfsOptions>();
}
