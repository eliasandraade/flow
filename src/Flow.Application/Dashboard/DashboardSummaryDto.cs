namespace Flow.Application.Dashboard;

public record DashboardSummaryDto(
    int TotalIdeas,
    int ApprovedIdeas,
    int RejectedIdeas,
    int PendingIdeas,
    double ConversionRate,
    int ActiveProjects,
    int BlockedProjects,
    int CompletedProjects,
    decimal TotalRoi,
    double AverageCompletionDays,
    double BottleneckIndex,
    IReadOnlyList<BlockedProjectDto> BlockedProjectList);
