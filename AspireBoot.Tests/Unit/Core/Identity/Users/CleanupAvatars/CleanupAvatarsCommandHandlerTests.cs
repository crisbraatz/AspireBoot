using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.CleanupAvatars;
using AspireBoot.Core.Integrations.ObjectStorage;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.CleanupAvatars;

public class CleanupAvatarsCommandHandlerTests
{
    [Theory]
    [InlineData(".jpg")]
    [InlineData(".png")]
    [InlineData(".webp")]
    public async Task ShouldReturnSuccessWhenDeletingOnlyOldUnreferencedAvatars(string extension)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string orphan = CreateAvatarKey(extension);
        string referenced = CreateAvatarKey();
        string recent = CreateAvatarKey();
        ICleanupAvatarRepository cleanupAvatarRepository = Substitute.For<ICleanupAvatarRepository>();
        cleanupAvatarRepository.IsReferencedAsync(referenced, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        IObjectStorage objectStorage = MockObjectStorage([new ListStoredObjectItemResult(orphan, now.AddDays(-3)), new ListStoredObjectItemResult(referenced, now.AddDays(-3)), new ListStoredObjectItemResult(recent, now)]);

        Result<CleanupAvatarsResult> result = await new CleanupAvatarsCommandHandler(cleanupAvatarRepository, objectStorage).HandleAsync(new CleanupAvatarsCommand(now.AddDays(-2), 48), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.DeletedCount);
        await objectStorage.Received(1).DeleteAsync(orphan, Arg.Any<CancellationToken>());
        await objectStorage.DidNotReceive().DeleteAsync(referenced, Arg.Any<CancellationToken>());
        await objectStorage.DidNotReceive().DeleteAsync(recent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenContinuingThroughEveryListingPage()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string first = CreateAvatarKey();
        IObjectStorage objectStorage = MockObjectStorage([new ListStoredObjectItemResult(first, now.AddDays(-3))], "next");
        string second = CreateAvatarKey();
        objectStorage.ListAsync("users/", "next", Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<ListStoredObjectResult>.Success(new ListStoredObjectResult([new ListStoredObjectItemResult(second, now.AddDays(-3))]))));

        Result<CleanupAvatarsResult> result = await new CleanupAvatarsCommandHandler(Substitute.For<ICleanupAvatarRepository>(), objectStorage).HandleAsync(new CleanupAvatarsCommand(now.AddDays(-2), 48), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.DeletedCount);
        await objectStorage.Received(1).DeleteAsync(first, Arg.Any<CancellationToken>());
        await objectStorage.Received(1).DeleteAsync(second, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenPreservingAnAvatarAtTheCutoffBoundary()
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddDays(-2);
        string key = CreateAvatarKey();
        IObjectStorage objectStorage = MockObjectStorage([new ListStoredObjectItemResult(key, cutoff)]);
        ICleanupAvatarRepository cleanupAvatarRepository = Substitute.For<ICleanupAvatarRepository>();

        Result<CleanupAvatarsResult> result = await new CleanupAvatarsCommandHandler(cleanupAvatarRepository, objectStorage).HandleAsync(new CleanupAvatarsCommand(cutoff, 48), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.DeletedCount);
        await cleanupAvatarRepository.DidNotReceive().IsReferencedAsync(key, Arg.Any<CancellationToken>());
        await objectStorage.DidNotReceive().DeleteAsync(key, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("document.txt")]
    [InlineData("other/11111111-1111-1111-1111-111111111111/avatars/22222222222222222222222222222222.jpg")]
    [InlineData("users/not-a-guid/avatars/22222222222222222222222222222222.jpg")]
    [InlineData("users/11111111-1111-1111-1111-111111111111/files/22222222222222222222222222222222.jpg")]
    [InlineData("users/11111111-1111-1111-1111-111111111111/avatars/not-a-guid.jpg")]
    [InlineData("users/11111111-1111-1111-1111-111111111111/avatars/22222222222222222222222222222222")]
    [InlineData("users/11111111-1111-1111-1111-111111111111/avatars/22222222222222222222222222222222.jpg.extra")]
    [InlineData("users/11111111-1111-1111-1111-111111111111/avatars/22222222222222222222222222222222.gif")]
    public async Task ShouldReturnSuccessWhenSkippingObjectsOutsideTheAvatarKeyContract(string key)
    {
        IObjectStorage objectStorage = MockObjectStorage([new ListStoredObjectItemResult(key, DateTimeOffset.UtcNow.AddDays(-3))]);
        ICleanupAvatarRepository cleanupAvatarRepository = Substitute.For<ICleanupAvatarRepository>();

        Result<CleanupAvatarsResult> result = await new CleanupAvatarsCommandHandler(cleanupAvatarRepository, objectStorage).HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddDays(-2), 48), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.DeletedCount);
        await cleanupAvatarRepository.DidNotReceive().IsReferencedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await objectStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenCompletingAnEmptyListing()
    {
        IObjectStorage objectStorage = MockObjectStorage([]);

        Result<CleanupAvatarsResult> result = await new CleanupAvatarsCommandHandler(Substitute.For<ICleanupAvatarRepository>(), objectStorage).HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddDays(-2), 48), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.DeletedCount);
        await objectStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnFailureWhenACutoffWithoutTheMinimumGracePeriodIsProvided()
    {
        IObjectStorage objectStorage = MockObjectStorage([]);

        Result<CleanupAvatarsResult> result = await new CleanupAvatarsCommandHandler(Substitute.For<ICleanupAvatarRepository>(), objectStorage).HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow, 48), TestContext.Current.CancellationToken);

        Assert.Equal("identity.users.avatar.cleanup-age", Assert.Single(result.Failures!).Code);
        await objectStorage.DidNotReceive().ListAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnFailureWhenPreservingListingFailures()
    {
        IObjectStorage objectStorage = MockObjectStorage([]);
        Failure failure = Failure.ServiceUnavailable("storage.unavailable", "Unavailable.");
        objectStorage.ListAsync("users/", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<ListStoredObjectResult>.Failure([failure])));

        Result<CleanupAvatarsResult> result = await new CleanupAvatarsCommandHandler(Substitute.For<ICleanupAvatarRepository>(), objectStorage).HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddDays(-2), 48), TestContext.Current.CancellationToken);

        Assert.Equal(failure, Assert.Single(result.Failures!));
        await objectStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnFailureWhenPreservingADeletionFailureWhenStoppingBeforeTheNextObject()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string first = CreateAvatarKey();
        string second = CreateAvatarKey();
        IObjectStorage objectStorage = MockObjectStorage([new ListStoredObjectItemResult(first, now.AddDays(-3)), new ListStoredObjectItemResult(second, now.AddDays(-3))]);
        Failure failure = Failure.ServiceUnavailable("storage.delete-unavailable", "Unavailable.");
        objectStorage.DeleteAsync(first, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([failure])));

        Result<CleanupAvatarsResult> result = await new CleanupAvatarsCommandHandler(Substitute.For<ICleanupAvatarRepository>(), objectStorage).HandleAsync(new CleanupAvatarsCommand(now.AddDays(-2), 48), TestContext.Current.CancellationToken);

        Assert.Equal(failure, Assert.Single(result.Failures!));
        await objectStorage.DidNotReceive().DeleteAsync(second, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenARepeatedContinuationTokenIsProvided()
    {
        IObjectStorage objectStorage = MockObjectStorage([], "next");
        objectStorage.ListAsync("users/", "next", Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<ListStoredObjectResult>.Success(new ListStoredObjectResult([], "next"))));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new CleanupAvatarsCommandHandler(Substitute.For<ICleanupAvatarRepository>(), objectStorage).HandleAsync(new CleanupAvatarsCommand(DateTimeOffset.UtcNow.AddDays(-2), 48), TestContext.Current.CancellationToken));

        Assert.Equal("Object storage returned a repeated continuation token.", exception.Message);
        await objectStorage.Received(1).ListAsync("users/", "next", Arg.Any<CancellationToken>());
    }

    private static string CreateAvatarKey(string extension = ".jpg") => $"users/{Guid.NewGuid()}/avatars/{Guid.NewGuid():N}{extension}";

    private static IObjectStorage MockObjectStorage(IReadOnlyList<ListStoredObjectItemResult> items, string? continuationToken = null)
    {
        IObjectStorage objectStorage = Substitute.For<IObjectStorage>();
        objectStorage.ListAsync("users/", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<ListStoredObjectResult>.Success(new ListStoredObjectResult(items, continuationToken))));
        objectStorage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        return objectStorage;
    }
}
