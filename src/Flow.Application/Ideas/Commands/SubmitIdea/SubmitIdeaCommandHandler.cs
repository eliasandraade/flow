using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.SubmitIdea;

public class SubmitIdeaCommandHandler : IRequestHandler<SubmitIdeaCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmitIdeaCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(SubmitIdeaCommand request, CancellationToken cancellationToken)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");
        if (idea.SubmittedBy != actorId)
            throw new ForbiddenException("You can only submit your own ideas.");

        var oldStatus = idea.Status.ToString();
        idea.Submit();

        var auditLog = AuditLog.Create(
            entityType: nameof(Idea),
            entityId: idea.Id,
            action: "Submitted",
            actorId: actorId,
            actorName: _currentUser.UserName ?? string.Empty,
            oldValue: oldStatus,
            newValue: idea.Status.ToString());

        await _context.SaveChangesWithAuditAsync(new[] { auditLog }, cancellationToken);
    }
}
