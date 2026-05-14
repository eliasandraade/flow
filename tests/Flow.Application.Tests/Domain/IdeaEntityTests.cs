using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Flow.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Flow.Application.Tests.Domain;

public class IdeaEntityTests
{
    private static Idea CreateDraftIdea()
        => Idea.Create("Test Title", "Description", "Problem statement", Guid.NewGuid());

    [Fact]
    public void Create_ValidArgs_ReturnsDraftIdeaWithDefaultPriority()
    {
        var submittedBy = Guid.NewGuid();
        var idea = Idea.Create("My Idea", "Description", "The problem", submittedBy);

        idea.Title.Should().Be("My Idea");
        idea.Description.Should().Be("Description");
        idea.Problem.Should().Be("The problem");
        idea.SubmittedBy.Should().Be(submittedBy);
        idea.Status.Should().Be(IdeaStatus.Draft);
        idea.Priority.Should().Be(IdeaPriority.Medium);
        idea.ManagerComment.Should().BeNull();
        idea.LinkedGuidelineId.Should().BeNull();
    }

    [Fact]
    public void Submit_DraftIdea_ChangesStatusToUnderReview()
    {
        var idea = CreateDraftIdea();
        idea.Submit();
        idea.Status.Should().Be(IdeaStatus.UnderReview);
    }

    [Fact]
    public void Submit_NonDraftIdea_ThrowsDomainException()
    {
        var idea = CreateDraftIdea();
        idea.Submit(); // now UnderReview

        var act = () => idea.Submit();
        act.Should().Throw<DomainException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void Approve_UnderReviewIdea_ChangesStatusToApproved()
    {
        var idea = CreateDraftIdea();
        idea.Submit();
        idea.Approve("Great idea!");

        idea.Status.Should().Be(IdeaStatus.Approved);
        idea.ManagerComment.Should().Be("Great idea!");
    }

    [Fact]
    public void Approve_DraftIdea_ThrowsDomainException()
    {
        var idea = CreateDraftIdea();
        var act = () => idea.Approve();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reject_UnderReviewIdea_ChangesStatusToRejected()
    {
        var idea = CreateDraftIdea();
        idea.Submit();
        idea.Reject("Not aligned with strategy.");

        idea.Status.Should().Be(IdeaStatus.Rejected);
        idea.ManagerComment.Should().Be("Not aligned with strategy.");
    }

    [Fact]
    public void Reject_DraftIdea_ThrowsDomainException()
    {
        var idea = CreateDraftIdea();
        var act = () => idea.Reject("reason");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_DraftIdea_UpdatesFields()
    {
        var idea = CreateDraftIdea();
        var guidelineId = Guid.NewGuid();

        idea.Update("New Title", "New Desc", "New Problem", guidelineId);

        idea.Title.Should().Be("New Title");
        idea.Description.Should().Be("New Desc");
        idea.Problem.Should().Be("New Problem");
        idea.LinkedGuidelineId.Should().Be(guidelineId);
    }

    [Fact]
    public void Update_SubmittedIdea_ThrowsDomainException()
    {
        var idea = CreateDraftIdea();
        idea.Submit();

        var act = () => idea.Update("T", "D", "P", null);
        act.Should().Throw<DomainException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void SetPriority_AnyIdea_UpdatesPriority()
    {
        var idea = CreateDraftIdea();
        idea.SetPriority(IdeaPriority.High);
        idea.Priority.Should().Be(IdeaPriority.High);
    }
}
