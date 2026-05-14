using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.UpdateIdea;

public class UpdateIdeaCommandHandler : IRequestHandler<UpdateIdeaCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateIdeaCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateIdeaCommand request, CancellationToken cancellationToken)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");
        if (idea.SubmittedBy != actorId)
            throw new ForbiddenException("You can only edit your own ideas.");

        idea.Update(request.Title, request.Description, request.Problem, request.LinkedGuidelineId);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
