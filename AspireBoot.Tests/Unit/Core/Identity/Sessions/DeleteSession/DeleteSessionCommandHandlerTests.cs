using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Identity.Sessions.DeleteSession;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Sessions.DeleteSession;

public class DeleteSessionCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessWhenEndingAnOwnedSession()
    {
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        sessionsTicketStore.ListAsync("account", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<IssuedSession>>([new IssuedSession("session", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1))]));

        Result result = await new DeleteSessionCommandHandler(sessionsTicketStore).HandleAsync(new DeleteSessionCommand("account", "session"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await sessionsTicketStore.Received(1).RemoveAsync("session", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ShouldReturnNotFoundFailureWhenNotEndAnotherAccountsSession()
    {
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        sessionsTicketStore.ListAsync("account", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<IssuedSession>>([]));

        Result result = await new DeleteSessionCommandHandler(sessionsTicketStore).HandleAsync(new DeleteSessionCommand("account", "foreign"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.NotFound, Assert.Single(result.Failures!).FailureKind);
        await sessionsTicketStore.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnNotFoundFailureWhenASessionOutsideTheOwnersSessionListIsProvided()
    {
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();
        sessionsTicketStore.ListAsync("account", null, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<IssuedSession>>([new IssuedSession("session", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1))]));

        Result result = await new DeleteSessionCommandHandler(sessionsTicketStore).HandleAsync(new DeleteSessionCommand("account", "foreign"), TestContext.Current.CancellationToken);

        Assert.Equal(FailureKind.NotFound, Assert.Single(result.Failures!).FailureKind);
        await sessionsTicketStore.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
