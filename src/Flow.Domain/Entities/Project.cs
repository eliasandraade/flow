using Flow.Domain.Common;
using Flow.Domain.Enums;
using Flow.Domain.Exceptions;

namespace Flow.Domain.Entities;

public class Project : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? SourceIdeaId { get; private set; }
    public Guid OwnerId { get; private set; }
    public ProjectStatus Status { get; private set; }
    public ProjectPriority Priority { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public decimal? ActualCost { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? Deadline { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? BlockedReason { get; private set; }

    private Project() { }

    public static Project Create(
        string title,
        string description,
        Guid ownerId,
        ProjectPriority priority,
        Guid? sourceIdeaId = null,
        decimal? estimatedCost = null,
        DateTimeOffset? deadline = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Project title is required.");
        if (string.IsNullOrWhiteSpace(description)) throw new DomainException("Project description is required.");
        if (ownerId == Guid.Empty) throw new DomainException("Project must have a valid owner.");

        var now = DateTimeOffset.UtcNow;
        return new Project
        {
            Title = title,
            Description = description,
            OwnerId = ownerId,
            Status = ProjectStatus.Planned,
            Priority = priority,
            SourceIdeaId = sourceIdeaId,
            EstimatedCost = estimatedCost,
            Deadline = deadline,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string title,
        string description,
        ProjectPriority priority,
        Guid ownerId,
        decimal? estimatedCost,
        decimal? actualCost,
        DateTimeOffset? deadline)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Project title is required.");
        if (string.IsNullOrWhiteSpace(description)) throw new DomainException("Project description is required.");
        if (ownerId == Guid.Empty) throw new DomainException("Project must have a valid owner.");

        Title = title;
        Description = description;
        Priority = priority;
        OwnerId = ownerId;
        EstimatedCost = estimatedCost;
        ActualCost = actualCost;
        Deadline = deadline;
        SetUpdated();
    }

    public void Start()
    {
        if (Status != ProjectStatus.Planned)
            throw new DomainException("Only Planned projects can be started.");

        Status = ProjectStatus.InProgress;
        StartDate = DateTimeOffset.UtcNow;
        SetUpdated();
    }

    public void Complete()
    {
        if (Status != ProjectStatus.InProgress)
            throw new DomainException("Only InProgress projects can be completed.");

        Status = ProjectStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        SetUpdated();
    }

    public void Cancel(string reason)
    {
        if (Status != ProjectStatus.InProgress && Status != ProjectStatus.Blocked)
            throw new DomainException("Only InProgress or Blocked projects can be cancelled.");

        Status = ProjectStatus.Cancelled;
        SetUpdated();
    }

    public void Block(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A reason is required when blocking a project.");
        if (Status != ProjectStatus.Planned && Status != ProjectStatus.InProgress)
            throw new DomainException("Only Planned or InProgress projects can be blocked.");

        Status = ProjectStatus.Blocked;
        BlockedReason = reason;
        SetUpdated();
    }

    public void Unblock()
    {
        if (Status != ProjectStatus.Blocked)
            throw new DomainException("Only Blocked projects can be unblocked.");

        Status = ProjectStatus.InProgress;
        BlockedReason = null;
        SetUpdated();
    }
}
