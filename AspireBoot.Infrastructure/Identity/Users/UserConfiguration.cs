using AspireBoot.Core.Identity.Users;
using AspireBoot.Infrastructure.Integrations.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireBoot.Infrastructure.Identity.Users;

public class UserConfiguration : EntityConfiguration<User>
{
    public const string IdentityIndexName = "ux_users_identity_user_id";

    protected override void ConfigureEntity(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.Property(user => user.IdentityUserId).HasColumnName("identity_user_id").HasMaxLength(255).IsRequired();
        builder.Property(user => user.Email)
            .HasColumnName("email").HasColumnType("citext").HasMaxLength(254).IsRequired();
        builder.Property(user => user.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(user => user.AvatarKey).HasColumnName("avatar_key").HasMaxLength(512);
        builder.Property(user => user.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(user => user.IdentityUserId).HasDatabaseName(IdentityIndexName).IsUnique();
        builder.HasIndex(user => new { user.Name, user.Id }).HasDatabaseName("ix_users_name_id");
        builder.HasIndex(user => user.AvatarKey)
            .HasDatabaseName("ix_users_avatar_key").HasFilter("avatar_key IS NOT NULL");
    }
}
