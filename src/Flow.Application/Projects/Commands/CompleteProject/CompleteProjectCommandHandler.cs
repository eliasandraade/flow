using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Commands.CompleteProject;

public class CompleteProjectCommandHandler : IRequestHandler<CompleteProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CompleteProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(CompleteProjectCommand request, CancellationToken cancellationToken)
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
        project.Complete();

        var snapshot = ProjectSnapshot.Create(project, owner.Name, "Completed", actorId);
        _context.ProjectSnapshots.Add(snapshot);

        var audit = AuditLog.Create(
            entityType: "Project",
            entityId: project.Id,
            action: "Completed",
            actorId: actorId,
            actorName: _currentUser.UserName ?? string.Empty,
            oldValue: oldStatus,
            newValue: project.Status.ToString());

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
