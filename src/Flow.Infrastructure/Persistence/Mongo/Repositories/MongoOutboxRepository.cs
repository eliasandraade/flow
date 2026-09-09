using Flow.Application.Common.Persistence;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using MongoDB.Driver;

namespace Flow.Infrastructure.Persistence.Mongo.Repositories;

public sealed class MongoOutboxRepository : MongoRepositoryBase<OutboxMessage>, IOutboxRepository
{
    public MongoOutboxRepository(FlowMongoContext context, MongoSessionAccessor sessions)
        : base(context.NotificationOutbox, sessions) { }

    /// <summary>
    /// Idempotency is enforced by the unique index on dedupeKey rather than by a
    /// read-then-write check, which would race with a concurrent dispatch.
    /// </summary>
    public async Task<bool> TryAddAsync(
        OutboxMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            await InsertAsync(message, cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
        catch (MongoBulkWriteException ex)
            when (ex.WriteErrors.Any(e => e.Category == ServerErrorCategory.DuplicateKey))
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetDueAsync(
        DateTimeOffset now, int limit, CancellationToken cancellationToken = default) =>
        await Find(Filter.And(
                Filter.In(x => x.Status, new[] { OutboxStatus.Pending, OutboxStatus.Failed }),
                Filter.Lte(x => x.NextAttemptAt, now)))
            .Sort(Sort.Ascending(x => x.NextAttemptAt))
            .Limit(limit)
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default) =>
        ReplaceAsync(Filter.Eq(x => x.Id, message.Id), message, upsert: false, cancellationToken);

    public async Task<int> CountByStatusAsync(
        OutboxStatus status, CancellationToken cancellationToken = default) =>
        (int)await CountAsync(Filter.Eq(x => x.Status, status), cancellationToken);
}
