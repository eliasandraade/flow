using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Commands.UpdateProject;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");

        project.Update(
            request.Title, request.Description, request.Priority, request.OwnerId,
            request.EstimatedCost, request.ActualCost, request.Deadline);

        var audit = AuditLog.Create(
            entityType: "Project",
            entityId: project.Id,
            action: "Updated",
            actorId: actorId,
            actorName: _currentUser.UserName ?? string.Empty);

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
