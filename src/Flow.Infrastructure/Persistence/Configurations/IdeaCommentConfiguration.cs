using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class IdeaCommentConfiguration : IEntityTypeConfiguration<IdeaComment>
{
    public void Configure(EntityTypeBuilder<IdeaComment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Body).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(c => c.AuthorId).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.IdeaId);
    }
}
