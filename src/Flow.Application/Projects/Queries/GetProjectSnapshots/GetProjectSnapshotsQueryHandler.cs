using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Queries.GetProjectSnapshots;

public class GetProjectSnapshotsQueryHandler : IRequestHandler<GetProjectSnapshotsQuery, IReadOnlyList<ProjectSnapshotDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectSnapshotsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProjectSnapshotDto>> Handle(
        GetProjectSnapshotsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _context.ProjectSnapshots
            .Where(s => s.ProjectId == request.ProjectId)
            .OrderBy(s => s.TakenAt)
            .Select(s => new
            {
                s.Id, s.ProjectId, s.Title, s.Status, s.Priority,
                s.OwnerId, s.OwnerName, s.EstimatedCost, s.ActualCost,
                s.StartDate, s.Deadline, s.CompletedAt, s.BlockedReason,
                s.TriggerAction, s.TakenAt
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(s => new ProjectSnapshotDto(
                s.Id, s.ProjectId, s.Title,
                s.Status.ToString(), s.Priority.ToString(),
                s.OwnerId, s.OwnerName,
                s.EstimatedCost, s.ActualCost,
                s.StartDate, s.Deadline, s.CompletedAt,
                s.BlockedReason, s.TriggerAction, s.TakenAt))
            .ToList();
    }
}
