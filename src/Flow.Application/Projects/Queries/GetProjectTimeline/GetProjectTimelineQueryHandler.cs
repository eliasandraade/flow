using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Persistence;
using Flow.Domain.Entities;
using MediatR;

namespace Flow.Application.Projects.Queries.GetProjectTimeline;

public class GetProjectTimelineQueryHandler
    : IRequestHandler<GetProjectTimelineQuery, IReadOnlyList<TimelineEntryDto>>
{
    private readonly IProjectRepository _projects;
    private readonly IAuditLogRepository _auditLogs;

    public GetProjectTimelineQueryHandler(
        IProjectRepository projects, IAuditLogRepository auditLogs)
    {
        _projects = projects;
        _auditLogs = auditLogs;
    }

    public async Task<IReadOnlyList<TimelineEntryDto>> Handle(
        GetProjectTimelineQuery request, CancellationToken cancellationToken)
    {
        _ = await _projects.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var entries = await _auditLogs.GetForEntityAsync(
            nameof(Project), request.ProjectId, cancellationToken);

        return entries.Select(TimelineEntryDto.From).ToList();
    }
}
