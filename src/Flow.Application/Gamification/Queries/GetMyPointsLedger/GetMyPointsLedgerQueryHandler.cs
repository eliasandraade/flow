using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Gamification.Queries.GetMyPointsLedger;

public class GetMyPointsLedgerQueryHandler
    : IRequestHandler<GetMyPointsLedgerQuery, IReadOnlyList<PointsLedgerEntryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyPointsLedgerQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PointsLedgerEntryDto>> Handle(
        GetMyPointsLedgerQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");

        var entries = await _context.PointLedgerEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.AwardedAt)
            .Select(e => new { e.Id, e.Points, e.Reason, e.ReferenceType, e.ReferenceId, e.AwardedAt })
            .ToListAsync(cancellationToken);

        return entries
            .Select(e => new PointsLedgerEntryDto(
                e.Id, e.Points, e.Reason, e.ReferenceType, e.ReferenceId, e.AwardedAt))
            .ToList();
    }
}
