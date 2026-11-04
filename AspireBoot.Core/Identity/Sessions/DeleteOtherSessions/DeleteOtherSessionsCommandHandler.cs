using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Sessions.DeleteOtherSessions;

public class DeleteOtherSessionsCommandHandler(ISessionsTicketStore sessionsTicketStore)
    : ICommandHandler<DeleteOtherSessionsCommand>
{
    public async Task<Result> HandleAsync(DeleteOtherSessionsCommand command, CancellationToken cancellationToken)
    {
        await sessionsTicketStore
            .RemoveOthersAsync(command.IdentityUserId, command.CurrentSessionId, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success();
    }
}
