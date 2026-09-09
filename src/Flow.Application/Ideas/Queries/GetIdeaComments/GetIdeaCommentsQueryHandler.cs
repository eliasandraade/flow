using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Persistence;
using MediatR;

namespace Flow.Application.Ideas.Queries.GetIdeaComments;

public class GetIdeaCommentsQueryHandler
    : IRequestHandler<GetIdeaCommentsQuery, IReadOnlyList<IdeaCommentDto>>
{
    private readonly IIdeaRepository _ideas;
    private readonly IIdeaCommentRepository _comments;

    public GetIdeaCommentsQueryHandler(IIdeaRepository ideas, IIdeaCommentRepository comments)
    {
        _ideas = ideas;
        _comments = comments;
    }

    public async Task<IReadOnlyList<IdeaCommentDto>> Handle(
        GetIdeaCommentsQuery request, CancellationToken cancellationToken)
    {
        var idea = await _ideas.GetByIdAsync(request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        var comments = await _comments.GetForIdeaAsync(idea.Id, cancellationToken);
        return comments.Select(IdeaCommentDto.From).ToList();
    }
}
