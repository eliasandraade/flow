using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.ActorName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.OldValue).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValue).HasColumnType("nvarchar(max)");
        builder.Property(a => a.Reason).HasMaxLength(1000);

        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.Timestamp);
    }
}
