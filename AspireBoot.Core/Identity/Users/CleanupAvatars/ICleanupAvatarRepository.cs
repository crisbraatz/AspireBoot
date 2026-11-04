namespace AspireBoot.Core.Identity.Users.CleanupAvatars;

public interface ICleanupAvatarRepository
{
    Task<bool> IsReferencedAsync(string avatarKey, CancellationToken cancellationToken);
}
