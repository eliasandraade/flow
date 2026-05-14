using Flow.Application.Common.Interfaces;
using Flow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetDashboardSummaryQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        // --- Idea counts ---
        var totalIdeas = await _context.Ideas.CountAsync(cancellationToken);
        var approvedIdeas = await _context.Ideas
            .CountAsync(i => i.Status == IdeaStatus.Approved, cancellationToken);
        var rejectedIdeas = await _context.Ideas
            .CountAsync(i => i.Status == IdeaStatus.Rejected, cancellationToken);
        var pendingIdeas = await _context.Ideas
            .CountAsync(i => i.Status == IdeaStatus.UnderReview, cancellationToken);

        // --- Conversion rate: approved ideas that became projects ---
        var convertedCount = await _context.Projects
            .CountAsync(p => p.SourceIdeaId != null, cancellationToken);
        var conversionRate = approvedIdeas > 0
            ? Math.Round((double)convertedCount / approvedIdeas * 100, 1)
            : 0.0;

        // --- Project counts by status ---
        var activeProjects = await _context.Projects
            .CountAsync(p => p.Status == ProjectStatus.InProgress, cancellationToken);
        var blockedProjects = await _context.Projects
            .CountAsync(p => p.Status == ProjectStatus.Blocked, cancellationToken);
        var completedProjects = await _context.Projects
            .CountAsync(p => p.Status == ProjectStatus.Completed, cancellationToken);

        // --- Total actual ROI across all results ---
        var totalRoi = await _context.Results
            .Where(r => r.ActualROI.HasValue)
            .Select(r => r.ActualROI!.Value)
            .SumAsync(cancellationToken);

        // --- Average completion time (days from StartDate to CompletedAt) ---
        var completedData = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Completed
                && p.StartDate != null
                && p.CompletedAt != null)
            .Select(p => new { p.StartDate, p.CompletedAt })
            .ToListAsync(cancellationToken);

        var averageCompletionDays = completedData.Count > 0
            ? Math.Round(
                completedData.Average(p =>
                    (p.CompletedAt!.Value - p.StartDate!.Value).TotalDays), 1)
            : 0.0;

        // --- Bottleneck index: % of active+blocked that are blocked ---
        var bottleneckIndex = (activeProjects + blockedProjects) > 0
            ? Math.Round((double)blockedProjects / (activeProjects + blockedProjects) * 100, 1)
            : 0.0;

        // --- Blocked project list with days blocked ---
        var blockedList = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Blocked)
            .Select(p => new { p.Id, p.Title, p.OwnerId, p.BlockedReason })
            .ToListAsync(cancellationToken);

        var blockedIds = blockedList.Select(p => p.Id).ToList();

        // Resolve "blocked since" from most recent snapshot with TriggerAction == "Blocked"
        var blockedSinceMap = new Dictionary<Guid, DateTimeOffset>();
        if (blockedIds.Count > 0)
        {
            var sinceList = await _context.ProjectSnapshots
                .Where(s => blockedIds.Contains(s.ProjectId) && s.TriggerAction == "Blocked")
                .GroupBy(s => s.ProjectId)
                .Select(g => new { ProjectId = g.Key, BlockedAt = g.Max(s => s.TakenAt) })
                .ToListAsync(cancellationToken);

            blockedSinceMap = sinceList.ToDictionary(x => x.ProjectId, x => x.BlockedAt);
        }

        var now = DateTimeOffset.UtcNow;
        var blockedProjectList = blockedList
            .Select(p => new BlockedProjectDto(
                p.Id,
                p.Title,
                p.OwnerId,
                p.BlockedReason ?? string.Empty,
                blockedSinceMap.TryGetValue(p.Id, out var blockedAt)
                    ? (int)(now - blockedAt).TotalDays
                    : 0))
            .ToList();

        return new DashboardSummaryDto(
            totalIdeas,
            approvedIdeas,
            rejectedIdeas,
            pendingIdeas,
            conversionRate,
            activeProjects,
            blockedProjects,
            completedProjects,
            totalRoi,
            averageCompletionDays,
            bottleneckIndex,
            blockedProjectList);
    }
}
