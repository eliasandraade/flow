using Flow.Domain.Entities;
using Flow.Domain.Enums;

namespace Flow.Application.Common.Persistence;

public interface IIdeaRepository
{
    Task<Idea?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Idea>> QueryAsync(IdeaFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Idea>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(IdeaStatus status, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Idea idea, CancellationToken cancellationToken = default);
    Task UpdateAsync(Idea idea, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
