using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Flow.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Idea> Ideas => Set<Idea>();
    public DbSet<IdeaComment> IdeaComments => Set<IdeaComment>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectSnapshot> ProjectSnapshots => Set<ProjectSnapshot>();
    public DbSet<StrategicGuideline> StrategicGuidelines => Set<StrategicGuideline>();
    public DbSet<PointLedgerEntry> PointLedgerEntries => Set<PointLedgerEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public async Task<int> SaveChangesWithAuditAsync(
        IEnumerable<AuditLog> auditEntries,
        CancellationToken cancellationToken = default)
    {
        AuditLogs.AddRange(auditEntries);
        return await base.SaveChangesAsync(cancellationToken);
    }
}
