using Flow.Application.Common.Persistence;
using Flow.Domain.Entities;
using MongoDB.Driver;

namespace Flow.Infrastructure.Persistence.Mongo.Repositories;

public sealed class MongoRefreshTokenRepository
    : MongoRepositoryBase<RefreshToken>, IRefreshTokenRepository
{
    public MongoRefreshTokenRepository(FlowMongoContext context, MongoSessionAccessor sessions)
        : base(context.RefreshTokens, sessions) { }

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default) =>
        InsertAsync(token, cancellationToken);

    public Task<RefreshToken?> GetByHashAsync(
        string tokenHash, CancellationToken cancellationToken = default) =>
        Find(Filter.Eq(x => x.TokenHash, tokenHash)).FirstOrDefaultAsync(cancellationToken)!;

    public async Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await Find(Filter.And(
                Filter.Eq(x => x.UserId, userId),
                Filter.Eq(x => x.RevokedAt, null),
                Filter.Gt(x => x.ExpiresAt, DateTimeOffset.UtcNow)))
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default) =>
        ReplaceAsync(Filter.Eq(x => x.Id, token.Id), token, upsert: false, cancellationToken);

    /// <summary>
    /// Used when a revoked token is replayed: the safest response is to invalidate every
    /// live session for that user rather than guess which one was stolen.
    /// </summary>
    public Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        UpdateManyAsync(
            Filter.And(Filter.Eq(x => x.UserId, userId), Filter.Eq(x => x.RevokedAt, null)),
            Update.Set(x => x.RevokedAt, DateTimeOffset.UtcNow),
            cancellationToken);
}
