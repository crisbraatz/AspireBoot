using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.ListUserSessions;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.ListUserSessions;

public class ListUserSessionsQueryHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessAndPageThroughOwnedSessions()
    {
        User user = new("account", "user@example.com", "User Name");
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByIdAsync(user.Id, false, false, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        sessionsTicketStore.ListAsync(user.IdentityUserId, "current", Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<IssuedSession>>([new IssuedSession("current", now, now.AddHours(1), true), new IssuedSession("other", now, now.AddHours(1))]));

        Result<ListUserSessionsResult> result = await new ListUserSessionsQueryHandler(sessionsTicketStore, userRepository).HandleAsync(new ListUserSessionsQuery(user.Id, user.Id, "current", 2, 1), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("other", Assert.Single(result.Value!.Items).Id);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task ShouldReturnNotFoundFailureWhenTheUserIsUnknown()
    {
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();

        Result<ListUserSessionsResult> result = await new ListUserSessionsQueryHandler(sessionsTicketStore, Substitute.For<IUserRepository>()).HandleAsync(new ListUserSessionsQuery(Guid.NewGuid(), Guid.NewGuid(), "current", 1, 10), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.NotFound, Assert.Single(result.Failures!).FailureKind);
        await sessionsTicketStore.DidNotReceive().ListAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
