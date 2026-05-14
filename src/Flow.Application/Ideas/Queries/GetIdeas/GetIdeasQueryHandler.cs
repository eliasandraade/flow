using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Queries.GetIdeas;

public class GetIdeasQueryHandler : IRequestHandler<GetIdeasQuery, IReadOnlyList<IdeaSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetIdeasQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<IdeaSummaryDto>> Handle(
        GetIdeasQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Ideas.AsQueryable();
        if (request.SubmittedById.HasValue)
            query = query.Where(i => i.SubmittedBy == request.SubmittedById.Value);

        var ideas = await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        return ideas
            .Select(i => new IdeaSummaryDto(
                i.Id, i.Title, i.Problem,
                i.Status.ToString(), i.Priority.ToString(),
                i.SubmittedBy, i.LinkedGuidelineId, i.CreatedAt))
            .ToList();
    }
}
