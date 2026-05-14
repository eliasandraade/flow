using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Results.Queries.GetResult;

public class GetResultQueryHandler : IRequestHandler<GetResultQuery, ResultDto>
{
    private readonly IApplicationDbContext _context;

    public GetResultQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ResultDto> Handle(GetResultQuery request, CancellationToken cancellationToken)
    {
        var r = await _context.Results
            .FirstOrDefaultAsync(x => x.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Result", request.ProjectId);

        return new ResultDto(
            r.Id, r.ProjectId,
            r.EstimatedRevenue, r.EstimatedSavings, r.EstimatedCost, r.EstimatedROI, r.EstimatedRecordedAt,
            r.ActualRevenue, r.ActualSavings, r.ActualCost, r.ActualROI, r.ActualRecordedAt,
            r.PaybackPeriodMonths, r.Notes,
            r.RecordedBy, r.CreatedAt, r.UpdatedAt);
    }
}
