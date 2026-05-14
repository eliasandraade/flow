using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class ProjectSnapshotConfiguration : IEntityTypeConfiguration<ProjectSnapshot>
{
    public void Configure(EntityTypeBuilder<ProjectSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProjectId).IsRequired();
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.Priority).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.OwnerId).IsRequired();
        builder.Property(s => s.OwnerName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(s => s.ActualCost).HasColumnType("decimal(18,2)");
        builder.Property(s => s.TriggerAction).IsRequired().HasMaxLength(100);
        builder.Property(s => s.TriggeredByActorId).IsRequired();
        builder.Property(s => s.TakenAt).IsRequired();

        builder.HasIndex(s => s.ProjectId);
    }
}
