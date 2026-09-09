namespace Flow.Application.Ideas;

public record IdeaSummaryDto(
    Guid Id,
    string Title,
    string Problem,
    string Status,
    string Priority,
    Guid SubmittedBy,
    Guid? LinkedGuidelineId,
    DateTimeOffset CreatedAt);
