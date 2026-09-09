using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.RejectIdea;

public class RejectIdeaCommandHandler : IRequestHandler<RejectIdeaCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RejectIdeaCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RejectIdeaCommand request, CancellationToken cancellationToken)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");

        var oldStatus = idea.Status.ToString();
        idea.Reject(request.ManagerComment);

        var auditLog = AuditLog.Create(
            entityType: nameof(Idea),
            entityId: idea.Id,
            action: "Rejected",
            actorId: actorId,
            actorName: _currentUser.UserName ?? string.Empty,
            oldValue: oldStatus,
            newValue: idea.Status.ToString(),
            reason: request.ManagerComment);

        await _context.SaveChangesWithAuditAsync(new[] { auditLog }, cancellationToken);
    }
}
