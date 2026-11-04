using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.UpdateUser;
using AspireBoot.Core.Integrations.Caching;
using AspireBoot.Core.Integrations.ObjectStorage;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.UpdateUser;

public class UpdateUserCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessAndSaveTheNewAvatarBeforeDeletingTheOldOne()
    {
        User user = new("account", "user@example.com", "User Name");
        user.ChangeAvatarKey("old-avatar");
        IUserRepository userRepository = MockUserRepository(user);
        IObjectStorage objectStorage = MockObjectStorage();

        Result result = await new UpdateUserCommandHandler(MockCache(), objectStorage, userRepository).HandleAsync(new UpdateUserCommand("account", "Updated Name", new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", user.Name);
        Assert.NotEqual("old-avatar", user.AvatarKey);
        Received.InOrder(() =>
        {
            _ = objectStorage.PutAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), "image/jpeg", Arg.Any<CancellationToken>());
            _ = userRepository.UpdateAsync(user.Id, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
            _ = objectStorage.DeleteAsync("old-avatar", Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task ShouldReturnSuccessAndUpdateOnlyTheName()
    {
        User user = new("account", "user@example.com", "User Name");
        user.ChangeAvatarKey("old-avatar");
        IUserRepository userRepository = MockUserRepository(user);
        IObjectStorage objectStorage = MockObjectStorage();

        Result result = await new UpdateUserCommandHandler(MockCache(), objectStorage, userRepository).HandleAsync(new UpdateUserCommand("account", "Updated Name"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", user.Name);
        await userRepository.Received(1).UpdateAsync(user.Id, Arg.Is<IReadOnlyList<string>>(fields => fields.Contains("name") && !fields.Contains("avatarKey")), Arg.Any<CancellationToken>());
        await objectStorage.DidNotReceive().PutAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await objectStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenTheProfileDidNotChange()
    {
        User user = new("account", "user@example.com", "User Name");
        user.ChangeAvatarKey("old-avatar");
        IUserRepository userRepository = MockUserRepository(user);

        Result result = await new UpdateUserCommandHandler(MockCache(), MockObjectStorage(), userRepository).HandleAsync(new UpdateUserCommand("account", "User Name"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await userRepository.DidNotReceive().UpdateAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenStoringTheFirstAvatarWithoutDeletingAnObject()
    {
        User user = new("account", "user@example.com", "User Name");
        IObjectStorage objectStorage = MockObjectStorage();

        Result result = await new UpdateUserCommandHandler(MockCache(), objectStorage, MockUserRepository(user)).HandleAsync(new UpdateUserCommand("account", "Updated Name", new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(user.AvatarKey);
        await objectStorage.Received(1).PutAsync(user.AvatarKey, Arg.Any<ReadOnlyMemory<byte>>(), "image/jpeg", Arg.Any<CancellationToken>());
        await objectStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnNotFoundFailureWhenTheProfileIsUnknown()
    {
        Result result = await new UpdateUserCommandHandler(Substitute.For<ICache>(), Substitute.For<IObjectStorage>(), Substitute.For<IUserRepository>()).HandleAsync(new UpdateUserCommand("missing", "Updated Name"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.NotFound, Assert.Single(result.Failures!).FailureKind);
    }

    [Fact]
    public async Task ShouldReturnFailureWhenUploadFails()
    {
        User user = new("account", "user@example.com", "User Name");
        user.ChangeAvatarKey("old-avatar");
        IUserRepository userRepository = MockUserRepository(user);
        IObjectStorage objectStorage = MockObjectStorage();
        UpdateUserCommandHandler commandHandler = new(MockCache(), objectStorage, userRepository);
        objectStorage.PutAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), "image/jpeg", Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure([Failure.ServiceUnavailable("storage.unavailable", "Unavailable.")])));

        Result result = await commandHandler.HandleAsync(new UpdateUserCommand("account", Avatar: new byte[] { 0xFF, 0xD8, 0xFF }, AvatarContentType: "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        await objectStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await userRepository.DidNotReceive().UpdateAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenKeepingBothObjectsAfterAnAmbiguousDatabaseFailure()
    {
        User user = new("account", "user@example.com", "User Name");
        user.ChangeAvatarKey("old-avatar");
        IUserRepository userRepository = MockUserRepository(user);
        IObjectStorage objectStorage = MockObjectStorage();
        UpdateUserCommandHandler commandHandler = new(MockCache(), objectStorage, userRepository);
        userRepository.UpdateAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("Commit outcome unknown.")));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => commandHandler.HandleAsync(new UpdateUserCommand("account", Avatar: new byte[] { 0xFF, 0xD8, 0xFF }, AvatarContentType: "image/jpeg"), TestContext.Current.CancellationToken));

        Assert.Equal("Commit outcome unknown.", exception.Message);
        await objectStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static ICache MockCache()
    {
        ICache cache = Substitute.For<ICache>();
        cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        return cache;
    }

    private static IObjectStorage MockObjectStorage()
    {
        IObjectStorage objectStorage = Substitute.For<IObjectStorage>();
        objectStorage.PutAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>(), "image/jpeg", Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        objectStorage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        return objectStorage;
    }

    private static IUserRepository MockUserRepository(User user)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        return userRepository;
    }
}
