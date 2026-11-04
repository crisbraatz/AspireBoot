using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.ObjectStorage;

namespace AspireBoot.Core.Identity.Users.CleanupAvatars;

public class CleanupAvatarsCommandHandler(
    ICleanupAvatarRepository cleanupAvatarRepository,
    IObjectStorage objectStorage)
    : ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult>
{
    public async Task<Result<CleanupAvatarsResult>> HandleAsync(
        CleanupAvatarsCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.OlderThanUtc > DateTimeOffset.UtcNow.AddDays(-1))
            return Result<CleanupAvatarsResult>.Failure(
            [
                Failure.Validation(
                    "identity.users.avatar.cleanup-age",
                    "Avatar cleanup requires a grace period of at least 24 hours.",
                    "olderThanUtc")
            ]);

        int deleted = 0;
        string? continuationToken = null;

        do
        {
            Result<ListStoredObjectResult> listResult =
                await objectStorage.ListAsync("users/", continuationToken, cancellationToken).ConfigureAwait(false);

            if (!listResult.IsSuccess)
                return Result<CleanupAvatarsResult>.Failure(listResult.Failures!);

            foreach (ListStoredObjectItemResult item in listResult.Value!.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item.LastModifiedAtUtc >= command.OlderThanUtc || !IsAvatarKey(item.Key))
                    continue;

                if (await cleanupAvatarRepository.IsReferencedAsync(item.Key, cancellationToken).ConfigureAwait(false))
                    continue;

                Result deleteResult =
                    await objectStorage.DeleteAsync(item.Key, cancellationToken).ConfigureAwait(false);

                if (!deleteResult.IsSuccess)
                    return Result<CleanupAvatarsResult>.Failure(deleteResult.Failures!);

                deleted++;
            }

            if (listResult.Value.ContinuationToken is not null &&
                listResult.Value.ContinuationToken == continuationToken)
                throw new InvalidOperationException("Object storage returned a repeated continuation token.");

            continuationToken = listResult.Value.ContinuationToken;
        } while (continuationToken is not null);

        return Result<CleanupAvatarsResult>.Success(new CleanupAvatarsResult(deleted));
    }

    private static bool IsAvatarKey(string key)
    {
        string[] parts = key.Split('/');

        if (parts is not ["users", _, _, _] || !Guid.TryParseExact(parts[1], "D", out _) || parts[2] is not "avatars")
            return false;

        string[] file = parts[3].Split('.');

        return file.Length is 2 && Guid.TryParseExact(file[0], "N", out _) && file[1] is "jpg" or "png" or "webp";
    }
}
