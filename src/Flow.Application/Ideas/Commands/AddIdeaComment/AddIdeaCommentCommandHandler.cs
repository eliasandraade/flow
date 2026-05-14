using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.AddIdeaComment;

public class AddIdeaCommentCommandHandler : IRequestHandler<AddIdeaCommentCommand, IdeaCommentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddIdeaCommentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IdeaCommentDto> Handle(AddIdeaCommentCommand request, CancellationToken cancellationToken)
    {
        var ideaExists = await _context.Ideas
            .AnyAsync(i => i.Id == request.IdeaId, cancellationToken);
        if (!ideaExists)
            throw new NotFoundException("Idea", request.IdeaId);

        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");

        var comment = IdeaComment.Create(request.IdeaId, actorId, request.Body);
        _context.IdeaComments.Add(comment);

        var auditLog = AuditLog.Create(
            entityType: nameof(Idea),
            entityId: request.IdeaId,
            action: "CommentAdded",
            actorId: actorId,
            actorName: _currentUser.UserName ?? string.Empty,
            newValue: comment.Id.ToString());

        await _context.SaveChangesWithAuditAsync(new[] { auditLog }, cancellationToken);

        return new IdeaCommentDto(comment.Id, comment.AuthorId, comment.Body, comment.CreatedAt);
    }
}
