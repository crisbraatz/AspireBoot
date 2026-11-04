using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.GetUser;
using AspireBoot.Core.Integrations.Caching;
using AspireBoot.Core.Integrations.ObjectStorage;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.GetUser;

public class GetUserQueryHandlerTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task ShouldReturnSuccessWhenProjectingEmailOnlyForItsOwnerOrAnAdministrator(bool owner, bool administrator)
    {
        User user = new("account", "private@example.com", "User Name");
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdAsync(user.Id, true, administrator, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        userRepository.GetAdministratorIdentityUserIdsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>([]));
        ICache cache = Substitute.For<ICache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Success(null)));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));

        Result<GetUserResult> result = await new GetUserQueryHandler(cache, Substitute.For<IObjectStorage>(), userRepository).HandleAsync(new GetUserQuery(user.Id, owner ? user.Id : Guid.NewGuid(), administrator), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value!.Id);
        Assert.Equal(owner || administrator ? user.Email : null, result.Value.Email);
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenUsingTheMatchingCachedProfile()
    {
        User user = new("account", "private@example.com", "User Name");
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdAsync(user.Id, true, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        userRepository.GetAdministratorIdentityUserIdsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>([]));
        ICache cache = Substitute.For<ICache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Success(null)));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        GetUserQueryHandler queryHandler = new(cache, Substitute.For<IObjectStorage>(), userRepository);
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Success(System.Text.Json.JsonSerializer.Serialize(new CachedUser(user.Id, user.Email, "Cached Name")))));

        Result<GetUserResult> result = await queryHandler.HandleAsync(new GetUserQuery(user.Id, user.Id, false), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cached Name", result.Value!.Name);
        Assert.Equal(user.Email, result.Value.Email);
        await cache.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnASignedAvatarUrl()
    {
        User user = new("account", "private@example.com", "User Name");
        ICache cache = Substitute.For<ICache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Success("invalid-json")));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        IObjectStorage objectStorage = Substitute.For<IObjectStorage>();
        GetUserQueryHandler queryHandler = new(cache, objectStorage, MockUserRepository(user));
        user.ChangeAvatarKey("avatar");
        objectStorage.CreateUrlAsync("avatar", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string>.Success("https://storage.tests/avatar.jpg")));

        Result<GetUserResult> result = await queryHandler.HandleAsync(new GetUserQuery(user.Id, user.Id, false), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(new Uri("https://storage.tests/avatar.jpg"), result.Value!.AvatarUrl);
    }

    [Fact]
    public async Task ShouldReturnSuccessAndIgnoreACachedNullProfile()
    {
        User user = new("account", "private@example.com", "User Name");
        ICache cache = Substitute.For<ICache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Success("invalid-json")));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        GetUserQueryHandler queryHandler = new(cache, Substitute.For<IObjectStorage>(), MockUserRepository(user));
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Success("null")));

        Result<GetUserResult> result = await queryHandler.HandleAsync(new GetUserQuery(user.Id, user.Id, false), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value!.Id);
        Assert.Equal(user.Name, result.Value.Name);
        await cache.Received(1).SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessAndIgnoreACachedProfileForAnotherUser()
    {
        User user = new("account", "private@example.com", "User Name");
        ICache cache = Substitute.For<ICache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Success("invalid-json")));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        GetUserQueryHandler queryHandler = new(cache, Substitute.For<IObjectStorage>(), MockUserRepository(user));
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Success(System.Text.Json.JsonSerializer.Serialize(new CachedUser(Guid.NewGuid(), "other@example.com", "Other Name")))));

        Result<GetUserResult> result = await queryHandler.HandleAsync(new GetUserQuery(user.Id, user.Id, false), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value!.Id);
        Assert.Equal(user.Name, result.Value.Name);
        await cache.Received(1).SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnNotFoundFailureWhenTheProfileIsUnknownOrInvisible()
    {
        ICache cache = Substitute.For<ICache>();

        Result<GetUserResult> result = await new GetUserQueryHandler(cache, Substitute.For<IObjectStorage>(), Substitute.For<IUserRepository>()).HandleAsync(new GetUserQuery(Guid.NewGuid(), Guid.NewGuid(), false), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.NotFound, Assert.Single(result.Failures!).FailureKind);
        await cache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenCacheIsUnavailable()
    {
        User user = new("account", "private@example.com", "User Name");
        ICache cache = Substitute.For<ICache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Failure([Failure.ServiceUnavailable("cache.unavailable", "Unavailable.")])));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));

        Result<GetUserResult> result = await new GetUserQueryHandler(cache, Substitute.For<IObjectStorage>(), MockUserRepository(user)).HandleAsync(new GetUserQuery(user.Id, user.Id, false), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Name, result.Value!.Name);
        Assert.Equal(user.Email, result.Value.Email);
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenCacheIsInvalid()
    {
        User user = new("account", "private@example.com", "User Name");
        ICache cache = Substitute.For<ICache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Success("invalid-json")));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));

        Result<GetUserResult> result = await new GetUserQueryHandler(cache, Substitute.For<IObjectStorage>(), MockUserRepository(user)).HandleAsync(new GetUserQuery(user.Id, user.Id, false), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Name, result.Value!.Name);
        Assert.Equal(user.Email, result.Value.Email);
    }

    [Fact]
    public async Task ShouldReturnFailureWhenPreservingAvatarStorageFailures()
    {
        User user = new("account", "private@example.com", "User Name");
        ICache cache = Substitute.For<ICache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string?>.Success("invalid-json")));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        IObjectStorage objectStorage = Substitute.For<IObjectStorage>();
        GetUserQueryHandler queryHandler = new(cache, objectStorage, MockUserRepository(user));
        user.ChangeAvatarKey("avatar");
        objectStorage.CreateUrlAsync("avatar", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string>.Failure([Failure.ServiceUnavailable("storage.unavailable", "Unavailable.")])));

        Result<GetUserResult> result = await queryHandler.HandleAsync(new GetUserQuery(user.Id, user.Id, false), TestContext.Current.CancellationToken);

        Assert.Equal("storage.unavailable", Assert.Single(result.Failures!).Code);
    }

    private static IUserRepository MockUserRepository(User user)
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdAsync(user.Id, true, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        userRepository.GetAdministratorIdentityUserIdsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>([]));
        return userRepository;
    }
}
