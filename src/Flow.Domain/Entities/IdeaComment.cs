namespace Flow.Domain.Entities;

public class IdeaComment
{
    public Guid Id { get; private set; }
    public Guid IdeaId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private IdeaComment() { }

    public static IdeaComment Create(Guid ideaId, Guid authorId, string body)
    {
        return new IdeaComment
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            AuthorId = authorId,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
