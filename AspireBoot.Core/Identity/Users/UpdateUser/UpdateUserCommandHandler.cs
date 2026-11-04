using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.Audit;
using AspireBoot.Core.Integrations.Caching;
using AspireBoot.Core.Integrations.ObjectStorage;

namespace AspireBoot.Core.Identity.Users.UpdateUser;

public class UpdateUserCommandHandler(
    ICache cache,
    IObjectStorage objectStorage,
    IUserRepository userRepository)
    : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepository
            .FindByIdentityUserIdAsync(command.IdentityUserId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Result.Failure(
            [
                Failure.NotFound(
                    "identity.users.not-found",
                    "User not found.")
            ]);

        List<string> updatedFields = [];

        if (command.NewName is not null && user.ChangeName(IdentityConstants.NormalizeName(command.NewName)))
            updatedFields.Add(UserAuditFields.Name);

        string? oldAvatarKey = user.AvatarKey;
        bool changedAvatarKey = false;

        if (command.Avatar is { } avatar)
        {
            string avatarContentType = command.AvatarContentType!;
            string avatarObjectKey =
                $"users/{user.Id}/avatars/{Guid.CreateVersion7():N}{AvatarPolicy.GetExtension(avatarContentType)}";
            changedAvatarKey = user.ChangeAvatarKey(avatarObjectKey);

            if (changedAvatarKey)
            {
                updatedFields.Add(UserAuditFields.AvatarKey);

                Result result = await objectStorage
                    .PutAsync(avatarObjectKey, avatar, avatarContentType, cancellationToken).ConfigureAwait(false);

                if (!result.IsSuccess)
                    return result;
            }
        }

        if (updatedFields.Count is 0)
            return Result.Success();

        updatedFields.Add(UserAuditFields.UpdatedAtUtc);

        await userRepository.UpdateAsync(user.Id, updatedFields, cancellationToken).ConfigureAwait(false);
        await cache
            .RemoveAsync($"{IdentityConstants.UsersCachePrefix}{user.Id:N}", cancellationToken).ConfigureAwait(false);

        if (oldAvatarKey is not null && changedAvatarKey)
            await objectStorage.DeleteAsync(oldAvatarKey, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
