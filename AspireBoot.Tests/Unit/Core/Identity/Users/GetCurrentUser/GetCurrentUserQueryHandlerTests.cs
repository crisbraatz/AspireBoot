using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.GetCurrentUser;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.GetCurrentUser;

public class GetCurrentUserQueryHandlerTests
{
    [Fact]
    public async Task ShouldReturnTheExistingProfile()
    {
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));

        Result<GetCurrentUserResult> result = await new GetCurrentUserQueryHandler(userRepository).HandleAsync(new GetCurrentUserQuery("account"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value!.Id);
    }

    [Fact]
    public async Task ShouldReturnNotFoundFailureWhenTheProfileIsUnknown()
    {
        Result<GetCurrentUserResult> result = await new GetCurrentUserQueryHandler(Substitute.For<IUserRepository>()).HandleAsync(new GetCurrentUserQuery("account"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.NotFound, Assert.Single(result.Failures!).FailureKind);
    }
}
