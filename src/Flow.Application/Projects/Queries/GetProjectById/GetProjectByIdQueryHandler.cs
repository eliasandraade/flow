using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Queries.GetProjectById;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetProjectByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ProjectDetailDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var p = await _context.Projects
            .FirstOrDefaultAsync(x => x.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        return new ProjectDetailDto(
            p.Id, p.Title, p.Description,
            p.Status.ToString(), p.Priority.ToString(),
            p.OwnerId, p.SourceIdeaId,
            p.EstimatedCost, p.ActualCost,
            p.StartDate, p.Deadline, p.CompletedAt,
            p.BlockedReason, p.CreatedAt, p.UpdatedAt);
    }
}
