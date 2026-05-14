using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Idea> Ideas { get; }
    DbSet<IdeaComment> IdeaComments { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectSnapshot> ProjectSnapshots { get; }
    DbSet<StrategicGuideline> StrategicGuidelines { get; }
    DbSet<PointLedgerEntry> PointLedgerEntries { get; }
    DbSet<Result> Results { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Atomically appends auditEntries and saves all pending changes in one transaction.
    // All domain operations that produce audit events must call this instead of SaveChangesAsync.
    Task<int> SaveChangesWithAuditAsync(
        IEnumerable<AuditLog> auditEntries,
        CancellationToken cancellationToken = default);
}
