using AspireBoot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireBoot.Infrastructure.Postgres.Mappings;

public abstract class BaseEntityMapping<T> where T : BaseEntity
{
    protected EntityTypeBuilder<T> EntityTypeBuilder { get; }
    protected abstract string TableName { get; }

    protected BaseEntityMapping(EntityTypeBuilder<T> entityTypeBuilder)
    {
        EntityTypeBuilder = entityTypeBuilder;
        Map();
    }

    protected abstract void MapProperties();
    protected abstract void MapIndexes();
    protected abstract void MapForeignKeys();

    private void Map()
    {
        MapTableName();
        MapProperties();
        MapBaseProperties();
        MapPrimaryKey();
        MapIndexes();
        MapForeignKeys();
    }

    private void MapTableName() => EntityTypeBuilder.ToTable(TableName);

    private void MapBaseProperties()
    {
        EntityTypeBuilder.Property(x => x.Id).HasColumnName("id").IsRequired();
        EntityTypeBuilder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        EntityTypeBuilder.Property(x => x.CreatedBy).HasColumnName("created_by").IsRequired();
        EntityTypeBuilder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        EntityTypeBuilder.Property(x => x.UpdatedBy).HasColumnName("updated_by").IsRequired();
        EntityTypeBuilder.Property(x => x.Active).HasColumnName("active").IsRequired();
    }

    private void MapPrimaryKey() => EntityTypeBuilder.HasKey(x => x.Id);
}
