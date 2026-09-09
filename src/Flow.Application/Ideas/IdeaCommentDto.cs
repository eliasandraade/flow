namespace Flow.Application.Ideas;

public record IdeaCommentDto(
    Guid Id,
    Guid AuthorId,
    string Body,
    DateTimeOffset CreatedAt);
