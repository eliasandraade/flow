using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.CreateIdea;

public class CreateIdeaCommandHandler : IRequestHandler<CreateIdeaCommand, IdeaSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateIdeaCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IdeaSummaryDto> Handle(CreateIdeaCommand request, CancellationToken cancellationToken)
    {
        if (request.LinkedGuidelineId.HasValue)
        {
            var exists = await _context.StrategicGuidelines
                .AnyAsync(g => g.Id == request.LinkedGuidelineId.Value, cancellationToken);
            if (!exists)
                throw new NotFoundException("StrategicGuideline", request.LinkedGuidelineId.Value);
        }

        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");
        var idea = Idea.Create(
            request.Title, request.Description, request.Problem,
            actorId, request.LinkedGuidelineId);

        _context.Ideas.Add(idea);
        await _context.SaveChangesAsync(cancellationToken);

        return new IdeaSummaryDto(
            idea.Id, idea.Title, idea.Problem,
            idea.Status.ToString(), idea.Priority.ToString(),
            idea.SubmittedBy, idea.LinkedGuidelineId, idea.CreatedAt);
    }
}
