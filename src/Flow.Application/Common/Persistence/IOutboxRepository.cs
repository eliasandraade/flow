using Flow.Domain.Entities;

namespace Flow.Application.Common.Persistence;

public interface IOutboxRepository
{
    /// <summary>
    /// Returns false when the dedupe key already exists, which is how a reprocessed event
    /// is prevented from producing a duplicate push.
    /// </summary>
    Task<bool> TryAddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetDueAsync(
        DateTimeOffset now, int limit, CancellationToken cancellationToken = default);

    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(Domain.Enums.OutboxStatus status, CancellationToken cancellationToken = default);
}
