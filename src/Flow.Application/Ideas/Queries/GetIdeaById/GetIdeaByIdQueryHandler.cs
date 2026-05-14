using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Queries.GetIdeaById;

public class GetIdeaByIdQueryHandler : IRequestHandler<GetIdeaByIdQuery, IdeaDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetIdeaByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IdeaDetailDto> Handle(GetIdeaByIdQuery request, CancellationToken cancellationToken)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        return new IdeaDetailDto(
            idea.Id, idea.Title, idea.Description, idea.Problem,
            idea.Status.ToString(), idea.Priority.ToString(),
            idea.SubmittedBy, idea.ManagerComment, idea.LinkedGuidelineId,
            idea.CreatedAt, idea.UpdatedAt);
    }
}
