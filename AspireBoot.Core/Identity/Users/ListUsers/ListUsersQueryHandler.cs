using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.ObjectStorage;

namespace AspireBoot.Core.Identity.Users.ListUsers;

public class ListUsersQueryHandler(IObjectStorage objectStorage, IUserRepository userRepository)
    : IQueryHandler<ListUsersQuery, ListUsersResult>
{
    public async Task<Result<ListUsersResult>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<User> users = await userRepository
            .ListAsync((query.Page - 1) * query.Size, query.Size, query.IsAdministrator, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<string> administratorIds = await userRepository
            .GetAdministratorIdentityUserIdsAsync(
                users.Select(user => user.IdentityUserId).ToArray(), cancellationToken)
            .ConfigureAwait(false);

        List<ListUserItemResult> items = [];

        foreach (User user in users)
        {
            Uri? avatarUrl = null;

            if (user.AvatarKey is not null)
            {
                Result<string> result =
                    await objectStorage.CreateUrlAsync(user.AvatarKey, null, cancellationToken).ConfigureAwait(false);

                if (!result.IsSuccess)
                    return Result<ListUsersResult>.Failure(result.Failures!);

                avatarUrl = new Uri(result.Value!, UriKind.Absolute);
            }

            items.Add(new ListUserItemResult(
                user.Id, user.Name, user.IsActive, administratorIds.Contains(user.IdentityUserId), avatarUrl));
        }

        return Result<ListUsersResult>.Success(new ListUsersResult(
            items,
            query.Page,
            query.Size,
            await userRepository.CountAsync(query.IsAdministrator, cancellationToken).ConfigureAwait(false)));
    }
}
