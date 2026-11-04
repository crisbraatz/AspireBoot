using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.GetCurrentUser;

public class GetCurrentUserQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetCurrentUserQuery, GetCurrentUserResult>
{
    public async Task<Result<GetCurrentUserResult>> HandleAsync(
        GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        User? user = await userRepository
            .FindByIdentityUserIdAsync(query.IdentityUserId, true, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return user is not null
            ? Result<GetCurrentUserResult>.Success(new GetCurrentUserResult(user.Id))
            : Result<GetCurrentUserResult>.Failure(
            [
                Failure.NotFound(
                    "identity.users.not-found",
                    "User not found.")
            ]);
    }
}
