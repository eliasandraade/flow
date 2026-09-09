namespace Flow.Application.Projects;

public record ProjectSummaryDto(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    Guid OwnerId,
    Guid? SourceIdeaId,
    DateTimeOffset? Deadline,
    string? BlockedReason,
    DateTimeOffset CreatedAt);
