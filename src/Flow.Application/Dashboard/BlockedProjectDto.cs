namespace Flow.Application.Dashboard;

public record BlockedProjectDto(
    Guid Id,
    string Title,
    Guid OwnerId,
    string BlockedReason,
    int DaysBlocked);
