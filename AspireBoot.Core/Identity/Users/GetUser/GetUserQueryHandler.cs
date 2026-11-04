using System.Text.Json;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.Caching;
using AspireBoot.Core.Integrations.ObjectStorage;

namespace AspireBoot.Core.Identity.Users.GetUser;

public class GetUserQueryHandler(ICache cache, IObjectStorage objectStorage, IUserRepository userRepository)
    : IQueryHandler<GetUserQuery, GetUserResult>
{
    public async Task<Result<GetUserResult>> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        User? user = await userRepository
            .FindByIdAsync(query.TargetUserId, true, query.IsAdministrator, cancellationToken).ConfigureAwait(false);

        if (user is null)
            return Result<GetUserResult>.Failure(
            [
                Failure.NotFound(
                    "identity.users.not-found",
                    "User not found.")
            ]);

        CachedUser? cachedUser = null;
        bool includeEmail = query.TargetUserId == query.CurrentUserId || query.IsAdministrator;

        Result<string?> cacheResult = await cache
            .GetAsync($"{IdentityConstants.UsersCachePrefix}{query.TargetUserId:N}", cancellationToken)
            .ConfigureAwait(false);

        if (cacheResult is { IsSuccess: true, Value: not null })
        {
            try
            {
                cachedUser = JsonSerializer.Deserialize<CachedUser>(cacheResult.Value);
            }
            catch (JsonException)
            {
                // JsonException
            }

            if (cachedUser is null || cachedUser.Id != query.TargetUserId)
                cachedUser = null;
        }

        if (cachedUser is null)
        {
            cachedUser = new CachedUser(user.Id, user.Email, user.Name, user.AvatarKey);

            await cache
                .SetAsync(
                    $"{IdentityConstants.UsersCachePrefix}{user.Id:N}",
                    JsonSerializer.Serialize(cachedUser),
                    TimeSpan.FromMinutes(1),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        Uri? avatarUrl = null;

        if (cachedUser.AvatarKey is not null)
        {
            Result<string> objectStorageResult = await objectStorage
                .CreateUrlAsync(cachedUser.AvatarKey, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!objectStorageResult.IsSuccess)
                return Result<GetUserResult>.Failure(objectStorageResult.Failures!);

            avatarUrl = new Uri(objectStorageResult.Value!, UriKind.Absolute);
        }

        IReadOnlyList<string> administratorIds = await userRepository
            .GetAdministratorIdentityUserIdsAsync([user.IdentityUserId], cancellationToken).ConfigureAwait(false);

        return Result<GetUserResult>.Success(new GetUserResult(
            cachedUser.Id,
            cachedUser.Name,
            user.IsActive,
            administratorIds.Contains(user.IdentityUserId),
            includeEmail ? cachedUser.Email : null,
            avatarUrl));
    }
}
