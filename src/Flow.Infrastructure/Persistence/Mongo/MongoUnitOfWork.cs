using Flow.Application.Common.Persistence;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Flow.Infrastructure.Persistence.Mongo;

/// <summary>
/// Runs a use case inside a MongoDB multi-document transaction.
///
/// Under EF Core the atomicity of "aggregate + audit log + snapshot" came free from a
/// single SaveChanges. With explicit document writes that guarantee has to be created
/// deliberately, and this is where it is created. Requires a replica set.
/// </summary>
public sealed class MongoUnitOfWork : IUnitOfWork
{
    private readonly FlowMongoContext _context;
    private readonly MongoSessionAccessor _sessionAccessor;
    private readonly ILogger<MongoUnitOfWork> _logger;

    public MongoUnitOfWork(
        FlowMongoContext context,
        MongoSessionAccessor sessionAccessor,
        ILogger<MongoUnitOfWork> logger)
    {
        _context = context;
        _sessionAccessor = sessionAccessor;
        _logger = logger;
    }

    public Task ExecuteAsync(
        Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
        ExecuteAsync<object?>(async ct =>
        {
            await operation(ct);
            return null;
        }, cancellationToken);

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        // Nested calls join the transaction that is already open rather than starting a
        // second one, so composing handlers stays safe.
        if (_sessionAccessor.InTransaction)
            return await operation(cancellationToken);

        using var session = await _context.Client.StartSessionAsync(
            cancellationToken: cancellationToken);

        using var scope = _sessionAccessor.Use(session);

        session.StartTransaction();

        try
        {
            var result = await operation(cancellationToken);
            await session.CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            // If any part of the unit fails — including the audit write — nothing is
            // persisted. A state transition without its audit entry is worse than a
            // failed request.
            try
            {
                await session.AbortTransactionAsync(CancellationToken.None);
            }
            catch (Exception abortEx)
            {
                _logger.LogError(abortEx, "Failed to abort a MongoDB transaction after an error.");
            }

            _logger.LogWarning(ex, "Transaction rolled back: {Message}", ex.Message);
            throw;
        }
    }
}
