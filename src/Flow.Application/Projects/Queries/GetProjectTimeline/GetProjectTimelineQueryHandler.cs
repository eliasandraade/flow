using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Queries.GetProjectTimeline;

public class GetProjectTimelineQueryHandler : IRequestHandler<GetProjectTimelineQuery, IReadOnlyList<TimelineEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectTimelineQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<TimelineEntryDto>> Handle(
        GetProjectTimelineQuery request, CancellationToken cancellationToken)
    {
        var rows = await _context.AuditLogs
            .Where(a => a.EntityType == "Project" && a.EntityId == request.ProjectId)
            .OrderBy(a => a.Timestamp)
            .Select(a => new TimelineEntryDto(
                a.Action, a.ActorId, a.ActorName,
                a.OldValue, a.NewValue, a.Reason, a.Timestamp))
            .ToListAsync(cancellationToken);

        return rows;
    }
}
