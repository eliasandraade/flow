using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class IdeaConfiguration : IEntityTypeConfiguration<Idea>
{
    public void Configure(EntityTypeBuilder<Idea> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Title).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(i => i.Problem).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(i => i.SubmittedBy).IsRequired();
        builder.Property(i => i.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(i => i.Priority).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(i => i.ManagerComment).HasMaxLength(2000);
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();

        builder.HasIndex(i => i.SubmittedBy);
        builder.HasIndex(i => i.Status);
    }
}
