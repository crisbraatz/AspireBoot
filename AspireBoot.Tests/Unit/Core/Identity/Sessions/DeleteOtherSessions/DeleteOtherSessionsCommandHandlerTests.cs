using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions;
using AspireBoot.Core.Identity.Sessions.DeleteOtherSessions;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Core.Identity.Sessions.DeleteOtherSessions;

public class DeleteOtherSessionsCommandHandlerTests
{
    [Fact]
    public async Task ShouldReturnSuccessWhenEndingOnlyTheOtherSessions()
    {
        ISessionsTicketStore sessionsTicketStore = Substitute.For<ISessionsTicketStore>();

        Result result = await new DeleteOtherSessionsCommandHandler(sessionsTicketStore).HandleAsync(new DeleteOtherSessionsCommand("account", "session"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await sessionsTicketStore.Received(1).RemoveOthersAsync("account", "session", Arg.Any<CancellationToken>());
    }
}
