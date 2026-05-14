using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class StrategicGuidelineConfiguration : IEntityTypeConfiguration<StrategicGuideline>
{
    public void Configure(EntityTypeBuilder<StrategicGuideline> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Title).IsRequired().HasMaxLength(200);
        builder.Property(g => g.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(g => g.CreatedBy).IsRequired();
        builder.Property(g => g.CreatedAt).IsRequired();
        builder.Property(g => g.UpdatedAt).IsRequired();
    }
}
