using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class ResultConfiguration : IEntityTypeConfiguration<Result>
{
    public void Configure(EntityTypeBuilder<Result> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ProjectId).IsRequired();
        builder.HasIndex(r => r.ProjectId).IsUnique(); // one result per project

        builder.HasOne<Project>()
            .WithOne()
            .HasForeignKey<Result>(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.EstimatedRevenue).HasColumnType("decimal(18,2)");
        builder.Property(r => r.EstimatedSavings).HasColumnType("decimal(18,2)");
        builder.Property(r => r.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(r => r.EstimatedROI).HasColumnType("decimal(18,4)");

        builder.Property(r => r.ActualRevenue).HasColumnType("decimal(18,2)");
        builder.Property(r => r.ActualSavings).HasColumnType("decimal(18,2)");
        builder.Property(r => r.ActualCost).HasColumnType("decimal(18,2)");
        builder.Property(r => r.ActualROI).HasColumnType("decimal(18,4)");

        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.RecordedBy).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
    }
}
