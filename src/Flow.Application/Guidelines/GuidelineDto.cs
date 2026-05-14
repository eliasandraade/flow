namespace Flow.Application.Guidelines;

public record GuidelineDto(
    Guid Id,
    string Title,
    string Description,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
