using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class PointLedgerEntryConfiguration : IEntityTypeConfiguration<PointLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PointLedgerEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Points).IsRequired();
        builder.Property(e => e.Reason).IsRequired().HasMaxLength(500);
        builder.Property(e => e.ReferenceType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ReferenceId).IsRequired();
        builder.Property(e => e.AwardedAt).IsRequired();

        builder.HasIndex(e => e.UserId);
    }
}
