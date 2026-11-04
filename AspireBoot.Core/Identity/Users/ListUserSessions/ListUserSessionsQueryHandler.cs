using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Sessions;

namespace AspireBoot.Core.Identity.Users.ListUserSessions;

public class ListUserSessionsQueryHandler(ISessionsTicketStore sessionsTicketStore, IUserRepository userRepository)
    : IQueryHandler<ListUserSessionsQuery, ListUserSessionsResult>
{
    public async Task<Result<ListUserSessionsResult>> HandleAsync(
        ListUserSessionsQuery query, CancellationToken cancellationToken)
    {
        User? user = await userRepository
            .FindByIdAsync(query.TargetUserId, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (user is null)
            return Result<ListUserSessionsResult>.Failure(
            [
                Failure.NotFound(
                    "identity.users.not-found",
                    "User not found.")
            ]);

        IReadOnlyList<IssuedSession> issuedSessions = await sessionsTicketStore
            .ListAsync(user.IdentityUserId, query.CurrentSessionId, cancellationToken).ConfigureAwait(false);

        return Result<ListUserSessionsResult>.Success(new ListUserSessionsResult(
            issuedSessions
                .Skip((int)Math.Min((long)(query.Page - 1) * query.Size, int.MaxValue))
                .Take(query.Size)
                .Select(issuedSession =>
                    new ListUserSessionItemResult(
                        issuedSession.Id,
                        issuedSession.CreatedAtUtc,
                        issuedSession.ExpiresAtUtc,
                        issuedSession.TrackedDetails,
                        issuedSession.IsCurrent))
                .ToArray(),
            query.Page,
            query.Size,
            issuedSessions.Count));
    }
}
