using Flow.Domain.Entities;

namespace Flow.Application.Common.Persistence;

public interface IOutboxRepository
{
    /// <summary>
    /// Returns false when the dedupe key already exists, which is how a reprocessed event
    /// is prevented from producing a duplicate push.
    /// </summary>
    Task<bool> TryAddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes exclusive ownership of up to <paramref name="limit"/> due messages, moving
    /// each one to Processing in the same operation that selects it.
    ///
    /// Reading the due messages and then marking them is not equivalent, and that gap is
    /// the whole problem: two replicas polling at the same moment both see the same Pending
    /// document before either writes, and the recipient gets the notification twice. The
    /// selection and the claim therefore have to be one operation, decided by the database.
    ///
    /// A claim carries a lease. A worker that is killed between claiming and delivering
    /// would otherwise strand the message in Processing forever, so once the lease lapses
    /// another worker may take it over — which covers a crashed process, a restarted pod
    /// and a deploy in the middle of a batch.
    /// </summary>
    /// <param name="owner">Identifies the claiming worker. Diagnostic only.</param>
    Task<IReadOnlyList<OutboxMessage>> ClaimDueAsync(
        string owner,
        DateTimeOffset now,
        TimeSpan lease,
        int limit,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(Domain.Enums.OutboxStatus status, CancellationToken cancellationToken = default);
}
