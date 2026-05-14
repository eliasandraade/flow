using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.ApproveIdea;

public class ApproveIdeaCommandHandler : IRequestHandler<ApproveIdeaCommand>
{
    private const int IdeaApprovalPoints = 50;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ApproveIdeaCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(ApproveIdeaCommand request, CancellationToken cancellationToken)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");

        var submitter = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == idea.SubmittedBy, cancellationToken)
            ?? throw new NotFoundException("User", idea.SubmittedBy);

        var oldStatus = idea.Status.ToString();
        idea.Approve(request.ManagerComment);
        submitter.AddPoints(IdeaApprovalPoints);

        var ledgerEntry = PointLedgerEntry.Create(
            userId: submitter.Id,
            points: IdeaApprovalPoints,
            reason: "Idea approved",
            referenceType: "Idea",
            referenceId: idea.Id);
        _context.PointLedgerEntries.Add(ledgerEntry);

        var auditLog = AuditLog.Create(
            entityType: nameof(Idea),
            entityId: idea.Id,
            action: "Approved",
            actorId: actorId,
            actorName: _currentUser.UserName ?? string.Empty,
            oldValue: oldStatus,
            newValue: idea.Status.ToString(),
            reason: request.ManagerComment);

        await _context.SaveChangesWithAuditAsync(new[] { auditLog }, cancellationToken);
    }
}
