using Flow.Domain.Enums;

namespace Flow.Domain.Entities;

public class ProjectSnapshot
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ProjectStatus Status { get; private set; }
    public ProjectPriority Priority { get; private set; }
    public Guid OwnerId { get; private set; }
    public string OwnerName { get; private set; } = string.Empty;
    public Guid? SourceIdeaId { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public decimal? ActualCost { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? Deadline { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? BlockedReason { get; private set; }
    public string? CancelledReason { get; private set; }
    public DateTimeOffset TakenAt { get; private set; }
    public string TriggerAction { get; private set; } = string.Empty;
    public Guid TriggeredByActorId { get; private set; }
    public int SchemaVersion { get; private set; }

    private ProjectSnapshot() { }

    public static ProjectSnapshot Create(
        Project project,
        string ownerName,
        string triggerAction,
        Guid triggeredByActorId)
    {
        if (string.IsNullOrWhiteSpace(ownerName))
            throw new ArgumentException("Owner name is required.", nameof(ownerName));
        if (string.IsNullOrWhiteSpace(triggerAction))
            throw new ArgumentException("Trigger action is required.", nameof(triggerAction));

        return new ProjectSnapshot
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = project.Title,
            Description = project.Description,
            Status = project.Status,
            Priority = project.Priority,
            OwnerId = project.OwnerId,
            OwnerName = ownerName,
            SourceIdeaId = project.SourceIdeaId,
            EstimatedCost = project.EstimatedCost,
            ActualCost = project.ActualCost,
            StartDate = project.StartDate,
            Deadline = project.Deadline,
            CompletedAt = project.CompletedAt,
            BlockedReason = project.BlockedReason,
            CancelledReason = project.CancelledReason,
            TakenAt = DateTimeOffset.UtcNow,
            TriggerAction = triggerAction,
            TriggeredByActorId = triggeredByActorId,
            SchemaVersion = 1
        };
    }
}
