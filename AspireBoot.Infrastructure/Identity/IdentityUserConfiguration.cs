using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspireBoot.Infrastructure.Identity;

public static class IdentityUserConfiguration
{
    public static void ConfigureIdentityUser(this ModelBuilder builder)
    {
        builder.Entity<IdentityUser>().Property(identityUser => identityUser.Email).HasMaxLength(254).IsRequired();
        builder.Entity<IdentityUser>()
            .Property(identityUser => identityUser.NormalizedEmail).HasMaxLength(254).IsRequired();
        builder.Entity<IdentityUser>().HasIndex(identityUser => identityUser.NormalizedEmail).IsUnique();
    }
}
