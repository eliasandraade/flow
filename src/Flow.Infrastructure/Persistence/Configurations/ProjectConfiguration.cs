using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(p => p.OwnerId).IsRequired();
        builder.Property(p => p.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.Priority).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ActualCost).HasColumnType("decimal(18,2)");
        builder.Property(p => p.BlockedReason).HasMaxLength(1000);
        builder.Property(p => p.CancelledReason).HasMaxLength(1000);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.OwnerId);
    }
}
