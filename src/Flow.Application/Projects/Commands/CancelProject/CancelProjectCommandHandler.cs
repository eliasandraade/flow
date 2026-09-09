using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Commands.CancelProject;

public class CancelProjectCommandHandler : IRequestHandler<CancelProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CancelProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(CancelProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");

        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == project.OwnerId, cancellationToken)
            ?? throw new NotFoundException("User", project.OwnerId);

        var oldStatus = project.Status.ToString();
        project.Cancel(request.Reason);

        var snapshot = ProjectSnapshot.Create(project, owner.Name, "Cancelled", actorId);
        _context.ProjectSnapshots.Add(snapshot);

        var audit = AuditLog.Create(
            entityType: "Project",
            entityId: project.Id,
            action: "Cancelled",
            actorId: actorId,
            actorName: _currentUser.UserName ?? string.Empty,
            oldValue: oldStatus,
            newValue: project.Status.ToString(),
            reason: request.Reason);

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
