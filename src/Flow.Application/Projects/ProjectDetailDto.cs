namespace Flow.Application.Projects;

public record ProjectDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    Guid OwnerId,
    Guid? SourceIdeaId,
    decimal? EstimatedCost,
    decimal? ActualCost,
    DateTimeOffset? StartDate,
    DateTimeOffset? Deadline,
    DateTimeOffset? CompletedAt,
    string? BlockedReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
