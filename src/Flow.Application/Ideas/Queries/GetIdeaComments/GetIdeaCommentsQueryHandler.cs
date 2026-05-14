using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Queries.GetIdeaComments;

public class GetIdeaCommentsQueryHandler
    : IRequestHandler<GetIdeaCommentsQuery, IReadOnlyList<IdeaCommentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetIdeaCommentsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<IdeaCommentDto>> Handle(
        GetIdeaCommentsQuery request, CancellationToken cancellationToken)
    {
        var ideaExists = await _context.Ideas
            .AnyAsync(i => i.Id == request.IdeaId, cancellationToken);
        if (!ideaExists)
            throw new NotFoundException("Idea", request.IdeaId);

        var comments = await _context.IdeaComments
            .Where(c => c.IdeaId == request.IdeaId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments
            .Select(c => new IdeaCommentDto(c.Id, c.AuthorId, c.Body, c.CreatedAt))
            .ToList();
    }
}
