using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users;

namespace AspireBoot.Core.Identity.Sessions.GetCurrentSession;

public class GetCurrentSessionQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetCurrentSessionQuery, GetCurrentSessionResult>
{
    public async Task<Result<GetCurrentSessionResult>> HandleAsync(
        GetCurrentSessionQuery query, CancellationToken cancellationToken)
    {
        User? user = await userRepository
            .FindByIdentityUserIdAsync(query.IdentityUserId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Result<GetCurrentSessionResult>.Failure(
            [
                Failure.NotFound(
                    "identity.users.not-found",
                    "User not found.")
            ]);

        return Result<GetCurrentSessionResult>.Success(
            new GetCurrentSessionResult(query.IdentityUserId, user.Id, query.Email, query.IsAdministrator));
    }
}
