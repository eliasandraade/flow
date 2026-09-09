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

    // Reserved for infrastructure operations that do not produce domain audit events (e.g. auth token management).
    // Domain state transitions MUST use SaveChangesWithAuditAsync.
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Atomically persists all pending changes and the provided audit entries in one transaction.
    Task<int> SaveChangesWithAuditAsync(
        IEnumerable<AuditLog> auditEntries,
        CancellationToken cancellationToken = default);
}
