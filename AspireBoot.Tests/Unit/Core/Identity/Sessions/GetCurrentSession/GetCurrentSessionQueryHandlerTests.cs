using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions.GetCurrentSession;
using AspireBoot.Core.Identity.Users;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Sessions.GetCurrentSession;

public class GetCurrentSessionQueryHandlerTests
{
    [Fact]
    public async Task ShouldReturnTheExistingProfile()
    {
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdentityUserIdAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));

        Result<GetCurrentSessionResult> result = await new GetCurrentSessionQueryHandler(userRepository).HandleAsync(new GetCurrentSessionQuery("account", "user@example.com", false), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value!.UserId);
    }

    [Fact]
    public async Task ShouldReturnNotFoundFailureWhenTheProfileIsUnknown()
    {
        Result<GetCurrentSessionResult> result = await new GetCurrentSessionQueryHandler(Substitute.For<IUserRepository>()).HandleAsync(new GetCurrentSessionQuery("account", "user@example.com", false), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.NotFound, Assert.Single(result.Failures!).FailureKind);
    }
}
