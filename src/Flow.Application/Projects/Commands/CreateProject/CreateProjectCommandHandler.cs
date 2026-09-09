using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Commands.CreateProject;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ProjectSummaryDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");

        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.OwnerId, cancellationToken)
            ?? throw new NotFoundException("User", request.OwnerId);

        var project = Project.Create(
            request.Title, request.Description, request.OwnerId, request.Priority,
            sourceIdeaId: null,
            estimatedCost: request.EstimatedCost,
            deadline: request.Deadline);

        _context.Projects.Add(project);

        var snapshot = ProjectSnapshot.Create(project, owner.Name, "Created", actorId);
        _context.ProjectSnapshots.Add(snapshot);

        var audit = AuditLog.Create(
            entityType: "Project",
            entityId: project.Id,
            action: "Created",
            actorId: actorId,
            actorName: _currentUser.UserName ?? string.Empty,
            oldValue: null,
            newValue: project.Status.ToString());

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);

        return new ProjectSummaryDto(
            project.Id, project.Title, project.Status.ToString(), project.Priority.ToString(),
            project.OwnerId, project.SourceIdeaId, project.Deadline, project.BlockedReason,
            project.CreatedAt);
    }
}
