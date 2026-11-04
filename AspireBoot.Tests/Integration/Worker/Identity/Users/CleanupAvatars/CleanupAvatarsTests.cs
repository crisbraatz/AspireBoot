using Amazon.S3;
using Amazon.S3.Model;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.CleanupAvatars;
using AspireBoot.Core.Integrations.ObjectStorage;
using AspireBoot.Infrastructure.Integrations.Postgres;
using AspireBoot.Infrastructure.Integrations.Rustfs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.Extensions;

namespace AspireBoot.Tests.Integration.Worker.Identity.Users.CleanupAvatars;

[Trait("Category", "Integration")]
public class CleanupAvatarsTests(TestFixture testFixture) : TestBase(testFixture)
{
    [Fact]
    public async Task ShouldReturnSuccessWhenDeletingOnlyOldUnreferencedAvatars()
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.Host.Services.CreateAsyncScope();
        IObjectStorage actualObjectStorage = asyncServiceScope.ServiceProvider.GetRequiredService<IObjectStorage>();
        User user = new("account", "user@example.com", "User Name");
        string prefix = $"users/{user.Id}/avatars/";
        string orphan = prefix + Guid.NewGuid().ToString("N") + ".jpg";
        string referenced = prefix + Guid.NewGuid().ToString("N") + ".jpg";
        string recent = prefix + Guid.NewGuid().ToString("N") + ".jpg";
        user.ChangeAvatarKey(referenced);
        AspireBootDbContext aspireBootDbContext = asyncServiceScope.ServiceProvider.GetRequiredService<AspireBootDbContext>();
        aspireBootDbContext.Users.Add(user);
        await aspireBootDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        foreach (string key in new[] { orphan, referenced, recent })
            Assert.True((await actualObjectStorage.PutAsync(key, new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg", TestContext.Current.CancellationToken)).IsSuccess);

        Result<CleanupAvatarsResult> result = await CreateCommandHandler(asyncServiceScope.ServiceProvider, MockObjectStorage(actualObjectStorage, [new ListStoredObjectItemResult(orphan, DateTimeOffset.UtcNow.AddDays(-3)), new ListStoredObjectItemResult(referenced, DateTimeOffset.UtcNow.AddDays(-3)), new ListStoredObjectItemResult(recent, DateTimeOffset.UtcNow)])).HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddDays(-2), 48), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.DeletedCount);
        Assert.False((await actualObjectStorage.ExistsAsync(orphan, TestContext.Current.CancellationToken)).Value);
        Assert.True((await actualObjectStorage.ExistsAsync(referenced, TestContext.Current.CancellationToken)).Value);
        Assert.True((await actualObjectStorage.ExistsAsync(recent, TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenPreservingRecentUnreferencedAvatars()
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.Host.Services.CreateAsyncScope();
        IAmazonS3 amazonS3 = asyncServiceScope.ServiceProvider.GetRequiredService<IAmazonS3>();
        string bucket = asyncServiceScope.ServiceProvider.GetRequiredService<RustfsOptions>().BucketName;
        string key = $"users/{Guid.NewGuid()}/avatars/{Guid.NewGuid():N}.jpg";
        await PutAvatarAsync(amazonS3, bucket, key);

        Result<CleanupAvatarsResult> result = await asyncServiceScope.ServiceProvider.GetRequiredService<ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult>>().HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddDays(-2), 48), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains((await amazonS3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, Prefix = key }, TestContext.Current.CancellationToken)).S3Objects, itemS3Object => itemS3Object.Key == key);
    }

    [Theory]
    [InlineData(23)]
    [InlineData(8761)]
    public async Task ShouldReturnFailureWhenAnUnsafeMinimumAgeIsProvided(int minimumAge)
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.Host.Services.CreateAsyncScope();

        Result<CleanupAvatarsResult> result = await asyncServiceScope.ServiceProvider.GetRequiredService<ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult>>().HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddYears(-2), minimumAge), TestContext.Current.CancellationToken);

        Assert.Equal("identity.users.avatar.cleanup-minimum-age", Assert.Single(result.Failures!).Code);
    }

    [Fact]
    public async Task ShouldReturnFailureWhenAnUnsafeCleanupGracePeriodIsProvided()
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.Host.Services.CreateAsyncScope();

        Result<CleanupAvatarsResult> result = await asyncServiceScope.ServiceProvider.GetRequiredService<ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult>>().HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow, 48), TestContext.Current.CancellationToken);

        Assert.Equal("identity.users.avatar.cleanup-age", Assert.Single(result.Failures!).Code);
    }

    [Fact]
    public async Task ShouldReturnFailureWhenRespectTheConfiguredMinimumAgeBeyondOneDay()
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.Host.Services.CreateAsyncScope();

        Result<CleanupAvatarsResult> result = await asyncServiceScope.ServiceProvider.GetRequiredService<ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult>>().HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddHours(-36), 48), TestContext.Current.CancellationToken);

        Assert.Equal("identity.users.avatar.cleanup-age", Assert.Single(result.Failures!).Code);
    }

    [Fact]
    public async Task ShouldReturnFailureWhenPreservingAnObjectListingFailure()
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.Host.Services.CreateAsyncScope();
        IObjectStorage actualObjectStorage = asyncServiceScope.ServiceProvider.GetRequiredService<IObjectStorage>();
        string key = $"users/{Guid.NewGuid()}/avatars/{Guid.NewGuid():N}.jpg";
        Assert.True((await actualObjectStorage.PutAsync(key, new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg", TestContext.Current.CancellationToken)).IsSuccess);
        IObjectStorage objectStorage = MockObjectStorage(actualObjectStorage, [new ListStoredObjectItemResult(key, DateTimeOffset.UtcNow.AddDays(-3))]);
        Failure failure = Failure.ServiceUnavailable("storage.unavailable", "Unavailable.");
        objectStorage.ListAsync("users/", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<ListStoredObjectResult>.Failure([failure])));

        Result<CleanupAvatarsResult> result = await CreateCommandHandler(asyncServiceScope.ServiceProvider, objectStorage).HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddDays(-2), 48), TestContext.Current.CancellationToken);

        Assert.Equal(failure, Assert.Single(result.Failures!));
        Assert.True((await actualObjectStorage.ExistsAsync(key, TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task ShouldReturnFailureWhenPreservingAnObjectDeletionFailure()
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.Host.Services.CreateAsyncScope();
        IObjectStorage actualObjectStorage = asyncServiceScope.ServiceProvider.GetRequiredService<IObjectStorage>();
        string key = $"users/{Guid.NewGuid()}/avatars/{Guid.NewGuid():N}.jpg";
        Assert.True((await actualObjectStorage.PutAsync(key, new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg", TestContext.Current.CancellationToken)).IsSuccess);
        IObjectStorage objectStorage = MockObjectStorage(actualObjectStorage, [new ListStoredObjectItemResult(key, DateTimeOffset.UtcNow.AddDays(-3))]);
        Failure failure = Failure.ServiceUnavailable("storage.unavailable", "Unavailable.");
        objectStorage.Configure().DeleteAsync(key, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([failure])));

        Result<CleanupAvatarsResult> result = await CreateCommandHandler(asyncServiceScope.ServiceProvider, objectStorage).HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddDays(-2), 48), TestContext.Current.CancellationToken);

        Assert.Equal(failure, Assert.Single(result.Failures!));
        Assert.True((await actualObjectStorage.ExistsAsync(key, TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenRepeatedStoragePaginationTokensAreProvided()
    {
        await using AsyncServiceScope asyncServiceScope = TestFixture.Host.Services.CreateAsyncScope();
        IObjectStorage objectStorage = MockObjectStorage(asyncServiceScope.ServiceProvider.GetRequiredService<IObjectStorage>(), []);
        objectStorage.ListAsync("users/", Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<ListStoredObjectResult>.Success(new ListStoredObjectResult([], "repeated"))));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateCommandHandler(asyncServiceScope.ServiceProvider, objectStorage).HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddDays(-2), 48), TestContext.Current.CancellationToken));

        Assert.Equal("Object storage returned a repeated continuation token.", exception.Message);
    }

    private static ValidatorCommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult> CreateCommandHandler(IServiceProvider serviceProvider, IObjectStorage objectStorage) => new(new CleanupAvatarsCommandHandler(serviceProvider.GetRequiredService<ICleanupAvatarRepository>(), objectStorage), serviceProvider.GetServices<ICommandValidator<CleanupAvatarsCommand>>());

    private static IObjectStorage MockObjectStorage(IObjectStorage actualObjectStorage, IReadOnlyList<ListStoredObjectItemResult> objects)
    {
        IObjectStorage objectStorage = Substitute.For<IObjectStorage>();
        objectStorage.ListAsync("users/", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<ListStoredObjectResult>.Success(new ListStoredObjectResult(objects))));
        objectStorage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(callInfo => actualObjectStorage.DeleteAsync(callInfo.Arg<string>(), callInfo.Arg<CancellationToken>()));
        return objectStorage;
    }

    private static async Task PutAvatarAsync(IAmazonS3 amazonS3, string bucket, string key)
    {
        using MemoryStream imageMemoryStream = new([0xFF, 0xD8, 0xFF]);
        await amazonS3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = key, ContentType = "image/jpeg", InputStream = imageMemoryStream }, TestContext.Current.CancellationToken);
    }
}
