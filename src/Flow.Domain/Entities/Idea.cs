using Flow.Domain.Common;
using Flow.Domain.Enums;
using Flow.Domain.Exceptions;

namespace Flow.Domain.Entities;

public class Idea : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Problem { get; private set; } = string.Empty;
    public Guid SubmittedBy { get; private set; }
    public IdeaStatus Status { get; private set; }
    public IdeaPriority Priority { get; private set; }
    public string? ManagerComment { get; private set; }
    public Guid? LinkedGuidelineId { get; private set; }

    private Idea() { }

    public static Idea Create(
        string title,
        string description,
        string problem,
        Guid submittedBy,
        Guid? linkedGuidelineId = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Idea
        {
            Title = title,
            Description = description,
            Problem = problem,
            SubmittedBy = submittedBy,
            Status = IdeaStatus.Draft,
            Priority = IdeaPriority.Medium,
            LinkedGuidelineId = linkedGuidelineId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string description, string problem, Guid? linkedGuidelineId)
    {
        if (Status != IdeaStatus.Draft)
            throw new DomainException("Only Draft ideas can be edited.");

        Title = title;
        Description = description;
        Problem = problem;
        LinkedGuidelineId = linkedGuidelineId;
        SetUpdated();
    }

    public void Submit()
    {
        if (Status != IdeaStatus.Draft)
            throw new DomainException("Only Draft ideas can be submitted.");

        Status = IdeaStatus.UnderReview;
        SetUpdated();
    }

    public void Approve(string? managerComment = null)
    {
        if (Status != IdeaStatus.UnderReview)
            throw new DomainException("Only ideas under review can be approved.");

        Status = IdeaStatus.Approved;
        ManagerComment = managerComment;
        SetUpdated();
    }

    public void Reject(string? managerComment = null)
    {
        if (Status != IdeaStatus.UnderReview)
            throw new DomainException("Only ideas under review can be rejected.");

        Status = IdeaStatus.Rejected;
        ManagerComment = managerComment;
        SetUpdated();
    }

    public void SetPriority(IdeaPriority priority)
    {
        Priority = priority;
        SetUpdated();
    }
}
