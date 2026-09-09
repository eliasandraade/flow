using Flow.Domain.Common;
using Flow.Domain.Exceptions;

namespace Flow.Domain.Entities;

public class StrategicGuideline : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private set; }

    private StrategicGuideline() { }

    public static StrategicGuideline Create(string title, string description, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Guideline title is required.");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Guideline description is required.");
        if (createdBy == Guid.Empty)
            throw new DomainException("CreatedBy must be a valid user ID.");

        var now = DateTimeOffset.UtcNow;
        return new StrategicGuideline
        {
            Title = title,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Guideline title is required.");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Guideline description is required.");

        Title = title;
        Description = description;
        SetUpdated();
    }
}
