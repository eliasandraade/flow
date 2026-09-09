using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Gamification.Queries.GetUserPoints;

public class GetUserPointsQueryHandler : IRequestHandler<GetUserPointsQuery, PointsSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserPointsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PointsSummaryDto> Handle(
        GetUserPointsQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        return new PointsSummaryDto(user.Id, user.Name, user.Points);
    }
}
