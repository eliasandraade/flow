using Flow.Application.Common.Interfaces;
using Flow.Application.Guidelines;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Guidelines.Queries.GetGuidelines;

public class GetGuidelinesQueryHandler : IRequestHandler<GetGuidelinesQuery, IReadOnlyList<GuidelineDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGuidelinesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<GuidelineDto>> Handle(
        GetGuidelinesQuery request, CancellationToken cancellationToken)
    {
        var guidelines = await _context.StrategicGuidelines
            .OrderBy(g => g.Title)
            .ToListAsync(cancellationToken);

        return guidelines
            .Select(g => new GuidelineDto(
                g.Id, g.Title, g.Description, g.CreatedBy, g.CreatedAt, g.UpdatedAt))
            .ToList();
    }
}
