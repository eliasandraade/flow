namespace Flow.Domain.Entities;

public class StrategicGuideline
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private StrategicGuideline() { }

    public static StrategicGuideline Create(string title, string description, Guid createdBy)
    {
        var now = DateTimeOffset.UtcNow;
        return new StrategicGuideline
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string description)
    {
        Title = title;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
