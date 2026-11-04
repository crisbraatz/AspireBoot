using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.ListUsers;
using AspireBoot.Core.Integrations.ObjectStorage;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.ListUsers;

public class ListUsersQueryHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessWhenListingTheRequestedPage()
    {
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.ListAsync(10, 10, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<User>>([user]));
        userRepository.CountAsync(false, Arg.Any<CancellationToken>()).Returns(Task.FromResult(11));
        userRepository.GetAdministratorIdentityUserIdsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>([]));

        Result<ListUsersResult> result = await new ListUsersQueryHandler(Substitute.For<IObjectStorage>(), userRepository).HandleAsync(new ListUsersQuery(2, 10, false), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, Assert.Single(result.Value!.Items).Id);
        Assert.Equal(11, result.Value.Count);
        Assert.Equal(2, result.Value.Page);
    }

    [Fact]
    public async Task ShouldReturnSuccessWhenListingUsersWithSignedAvatarUrls()
    {
        User user = new("account", "user@example.com", "User Name");
        user.ChangeAvatarKey("avatar");
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.ListAsync(0, 10, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<User>>([user]));
        userRepository.GetAdministratorIdentityUserIdsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>([]));
        IObjectStorage objectStorage = Substitute.For<IObjectStorage>();
        objectStorage.CreateUrlAsync("avatar", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string>.Failure([Failure.ServiceUnavailable("storage.unavailable", "Unavailable.")])));
        ListUsersQueryHandler queryHandler = new(objectStorage, userRepository);
        objectStorage.CreateUrlAsync("avatar", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string>.Success("https://storage.tests/avatar.jpg")));

        Result<ListUsersResult> result = await queryHandler.HandleAsync(new ListUsersQuery(1, 10, false), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        ListUserItemResult itemResult = Assert.Single(result.Value!.Items);
        Assert.Equal(user.Id, itemResult.Id);
        Assert.Equal(new Uri("https://storage.tests/avatar.jpg"), itemResult.AvatarUrl);
    }

    [Fact]
    public async Task ShouldReturnAnEmptyDirectory()
    {
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.ListAsync(0, 10, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<User>>([]));
        userRepository.GetAdministratorIdentityUserIdsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>([]));

        Result<ListUsersResult> result = await new ListUsersQueryHandler(Substitute.For<IObjectStorage>(), userRepository).HandleAsync(new ListUsersQuery(1, 10, false), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.Count);
    }

    [Fact]
    public async Task ShouldReturnFailureWhenPreservingAvatarStorageFailures()
    {
        User user = new("account", "user@example.com", "User Name");
        user.ChangeAvatarKey("avatar");
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.ListAsync(0, 10, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<User>>([user]));
        userRepository.GetAdministratorIdentityUserIdsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>([]));
        IObjectStorage objectStorage = Substitute.For<IObjectStorage>();
        objectStorage.CreateUrlAsync("avatar", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result<string>.Failure([Failure.ServiceUnavailable("storage.unavailable", "Unavailable.")])));

        Result<ListUsersResult> result = await new ListUsersQueryHandler(objectStorage, userRepository).HandleAsync(new ListUsersQuery(1, 10, false), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("storage.unavailable", Assert.Single(result.Failures!).Code);
    }
}
