using AspireBoot.Core.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireBoot.Infrastructure.Integrations.Postgres;

public abstract class EntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : Entity
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever().IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnName("updated_at_utc");
        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
