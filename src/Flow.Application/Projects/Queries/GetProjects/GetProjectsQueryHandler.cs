using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Queries.GetProjects;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, IReadOnlyList<ProjectSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProjectSummaryDto>> Handle(
        GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Projects.AsQueryable();
        if (request.OwnerId.HasValue)
            query = query.Where(p => p.OwnerId == request.OwnerId.Value);

        var rows = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id, p.Title, p.Status, p.Priority,
                p.OwnerId, p.SourceIdeaId, p.Deadline, p.BlockedReason, p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(p => new ProjectSummaryDto(
                p.Id, p.Title, p.Status.ToString(), p.Priority.ToString(),
                p.OwnerId, p.SourceIdeaId, p.Deadline, p.BlockedReason, p.CreatedAt))
            .ToList();
    }
}
