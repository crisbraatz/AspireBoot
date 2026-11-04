using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspireBoot.Infrastructure.Identity.Users.Audit;

public class UserAuditConfiguration : IEntityTypeConfiguration<UserAudit>
{
    public void Configure(EntityTypeBuilder<UserAudit> builder)
    {
        builder.ToTable("user_audits");
        builder.HasKey(audit => audit.MessageId);
        builder.Property(audit => audit.MessageId).HasColumnName("message_id").ValueGeneratedNever();
        builder.Property(audit => audit.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(audit => audit.Action).HasColumnName("action").HasMaxLength(64).IsRequired();
        builder.Property(audit => audit.UpdatedFields)
            .HasColumnName("updated_fields").HasColumnType("text[]").IsRequired();
        builder.Property(audit => audit.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(audit => audit.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
        builder.HasIndex(audit => new { audit.UserId, audit.CreatedAtUtc })
            .HasDatabaseName("ix_user_audits_user_id_created_at_utc");
    }
}
