using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Application.Common.Persistence;
using Flow.Domain.Enums;
using MediatR;

namespace Flow.Application.Ideas.Queries.GetIdeaById;

public class GetIdeaByIdQueryHandler : IRequestHandler<GetIdeaByIdQuery, IdeaDetailDto>
{
    private readonly IIdeaRepository _ideas;
    private readonly IGuidelineRepository _guidelines;
    private readonly ICurrentUserService _currentUser;

    public GetIdeaByIdQueryHandler(
        IIdeaRepository ideas,
        IGuidelineRepository guidelines,
        ICurrentUserService currentUser)
    {
        _ideas = ideas;
        _guidelines = guidelines;
        _currentUser = currentUser;
    }

    public async Task<IdeaDetailDto> Handle(
        GetIdeaByIdQuery request, CancellationToken cancellationToken)
    {
        var idea = await _ideas.GetByIdAsync(request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        var isOwner = idea.SubmittedBy == _currentUser.UserId;

        // Resource-level authorization: role alone is not enough, an Operator may only open
        // their own idea.
        if (_currentUser.IsInRole(UserRole.Operator) && !isOwner)
            throw new ForbiddenException("You can only view your own ideas.");

        string? guidelineTitle = null;
        if (idea.LinkedGuidelineId is { } guidelineId)
        {
            var guideline = await _guidelines.GetByIdAsync(guidelineId, cancellationToken);
            guidelineTitle = guideline?.Title;
        }

        return IdeaDetailDto.From(idea, guidelineTitle, isOwner);
    }
}
