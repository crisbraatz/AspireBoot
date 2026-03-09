using AspireBoot.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireBoot.Infrastructure.Postgres.Mappings.Users;

public class UserMapping(EntityTypeBuilder<User> entityTypeBuilder) : BaseEntityMapping<User>(entityTypeBuilder)
{
    protected override string TableName => "users";

    protected override void MapProperties()
    {
        EntityTypeBuilder.Property(x => x.Email).HasColumnName("email").IsRequired();
        EntityTypeBuilder.Property(x => x.Password).HasColumnName("password").IsRequired();
    }

    protected override void MapIndexes() => EntityTypeBuilder.HasIndex(x => new { x.Email, x.Active });

    protected override void MapForeignKeys()
    {
    }
}
