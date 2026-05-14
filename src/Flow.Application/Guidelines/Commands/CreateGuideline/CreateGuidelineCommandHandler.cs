using Flow.Application.Common.Interfaces;
using Flow.Application.Guidelines;
using Flow.Domain.Entities;
using MediatR;

namespace Flow.Application.Guidelines.Commands.CreateGuideline;

public class CreateGuidelineCommandHandler : IRequestHandler<CreateGuidelineCommand, GuidelineDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateGuidelineCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<GuidelineDto> Handle(CreateGuidelineCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");
        var guideline = StrategicGuideline.Create(request.Title, request.Description, actorId);

        _context.StrategicGuidelines.Add(guideline);
        await _context.SaveChangesAsync(cancellationToken);

        return new GuidelineDto(
            guideline.Id, guideline.Title, guideline.Description,
            guideline.CreatedBy, guideline.CreatedAt, guideline.UpdatedAt);
    }
}
