using Flow.Domain.Enums;

namespace Flow.Application.Common.Persistence;

/// <summary>Query shape for the idea list and the manager review queue.</summary>
public sealed record IdeaFilter
{
    public Guid? SubmittedBy { get; init; }
    public IdeaStatus? Status { get; init; }
    public IdeaPriority? Priority { get; init; }
    public Guid? LinkedGuidelineId { get; init; }
    public int? MinScore { get; init; }
    public IdeaSortOrder SortBy { get; init; } = IdeaSortOrder.CreatedAtDesc;
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}

public enum IdeaSortOrder
{
    CreatedAtDesc,
    ScoreDesc,
    FlowScoreDesc,
    PriorityDesc
}

public sealed record ProjectFilter
{
    public ProjectStatus? Status { get; init; }
    public ProjectStage? Stage { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? LinkedGuidelineId { get; init; }
    public Guid? SourceIdeaId { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}

public sealed record GuidelineFilter
{
    public GuidelineCategory? Category { get; init; }
    public string? Campaign { get; init; }

    /// <summary>When set, returns only guidelines whose validity period contains this instant.</summary>
    public DateTimeOffset? CurrentAt { get; init; }

    public int Skip { get; init; }
    public int Take { get; init; } = 100;
}

public sealed record NotificationFilter
{
    public Guid UserId { get; init; }
    public bool UnreadOnly { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}
