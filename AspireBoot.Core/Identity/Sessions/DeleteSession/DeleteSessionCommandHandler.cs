using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Sessions.DeleteSession;

public class DeleteSessionCommandHandler(ISessionsTicketStore sessionsTicketStore)
    : ICommandHandler<DeleteSessionCommand>
{
    public async Task<Result> HandleAsync(DeleteSessionCommand command, CancellationToken cancellationToken)
    {
        if ((await sessionsTicketStore
                .ListAsync(command.IdentityUserId, cancellationToken: cancellationToken).ConfigureAwait(false))
            .All(issuedSession => issuedSession.Id != command.SessionId))
            return Result.Failure(
            [
                Failure.NotFound(
                    "identity.sessions.not-found",
                    "Session not found.")
            ]);

        await sessionsTicketStore.RemoveAsync(command.SessionId, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
