namespace Flow.Application.Projects;

public record ProjectSnapshotDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Status,
    string Priority,
    Guid OwnerId,
    string OwnerName,
    decimal? EstimatedCost,
    decimal? ActualCost,
    DateTimeOffset? StartDate,
    DateTimeOffset? Deadline,
    DateTimeOffset? CompletedAt,
    string? BlockedReason,
    string TriggerAction,
    DateTimeOffset TakenAt);
