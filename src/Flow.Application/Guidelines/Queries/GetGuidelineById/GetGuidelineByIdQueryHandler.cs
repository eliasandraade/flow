using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Application.Guidelines;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Guidelines.Queries.GetGuidelineById;

public class GetGuidelineByIdQueryHandler : IRequestHandler<GetGuidelineByIdQuery, GuidelineDto>
{
    private readonly IApplicationDbContext _context;

    public GetGuidelineByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<GuidelineDto> Handle(
        GetGuidelineByIdQuery request, CancellationToken cancellationToken)
    {
        var guideline = await _context.StrategicGuidelines
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Guideline", request.Id);

        return new GuidelineDto(
            guideline.Id, guideline.Title, guideline.Description,
            guideline.CreatedBy, guideline.CreatedAt, guideline.UpdatedAt);
    }
}
