using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Results.Commands.RecordResult;

public class RecordResultCommandHandler : IRequestHandler<RecordResultCommand, ResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RecordResultCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ResultDto> Handle(RecordResultCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");
        var actorName = _currentUser.UserName ?? "Unknown";

        _ = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var result = await _context.Results
            .FirstOrDefaultAsync(r => r.ProjectId == request.ProjectId, cancellationToken);

        if (result is null)
        {
            result = Result.Create(request.ProjectId, actorId);
            _context.Results.Add(result);
        }

        bool hasEstimated = request.EstimatedRevenue.HasValue
            || request.EstimatedSavings.HasValue
            || request.EstimatedCost.HasValue;

        if (hasEstimated)
            result.SetEstimated(request.EstimatedRevenue, request.EstimatedSavings, request.EstimatedCost);

        bool hasActual = request.ActualRevenue.HasValue
            || request.ActualSavings.HasValue
            || request.ActualCost.HasValue;

        if (hasActual)
            result.SetActual(request.ActualRevenue, request.ActualSavings, request.ActualCost);

        if (request.PaybackPeriodMonths.HasValue || request.Notes is not null)
            result.SetNotes(request.PaybackPeriodMonths, request.Notes);

        var audit = AuditLog.Create(
            "Project", request.ProjectId, "ResultRecorded", actorId, actorName);

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);

        return ToDto(result);
    }

    private static ResultDto ToDto(Result r) => new(
        r.Id, r.ProjectId,
        r.EstimatedRevenue, r.EstimatedSavings, r.EstimatedCost, r.EstimatedROI, r.EstimatedRecordedAt,
        r.ActualRevenue, r.ActualSavings, r.ActualCost, r.ActualROI, r.ActualRecordedAt,
        r.PaybackPeriodMonths, r.Notes,
        r.RecordedBy, r.CreatedAt, r.UpdatedAt);
}
