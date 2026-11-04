using AspireBoot.Core.Identity.Users.CleanupAvatars;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Identity.Users.CleanupAvatars;

public class CleanupAvatarRepository(AspireBootDbContext aspireBootDbContext) : ICleanupAvatarRepository
{
    public Task<bool> IsReferencedAsync(string avatarKey, CancellationToken cancellationToken) =>
        aspireBootDbContext.Users.AsNoTracking().AnyAsync(user => user.AvatarKey == avatarKey, cancellationToken);
}
