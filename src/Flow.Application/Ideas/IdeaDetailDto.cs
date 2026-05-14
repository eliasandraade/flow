namespace Flow.Application.Ideas;

public record IdeaDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Problem,
    string Status,
    string Priority,
    Guid SubmittedBy,
    string? ManagerComment,
    Guid? LinkedGuidelineId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
