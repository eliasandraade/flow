using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Flow.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Flow.Application.Tests.Domain;

public class ProjectEntityTests
{
    private static Project CreatePlannedProject()
        => Project.Create("Test Project", "Description", Guid.NewGuid(), ProjectPriority.Medium);

    // ─── Create ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidArgs_ReturnsPlannedProject()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("My Project", "Desc", ownerId, ProjectPriority.High);

        project.Title.Should().Be("My Project");
        project.OwnerId.Should().Be(ownerId);
        project.Status.Should().Be(ProjectStatus.Planned);
        project.Priority.Should().Be(ProjectPriority.High);
        project.BlockedReason.Should().BeNull();
        project.CompletedAt.Should().BeNull();
        project.StartDate.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyTitle_ThrowsDomainException()
    {
        var act = () => Project.Create("", "Desc", Guid.NewGuid(), ProjectPriority.Low);
        act.Should().Throw<DomainException>().WithMessage("*title*");
    }

    // ─── Start ──────────────────────────────────────────────────────────────

    [Fact]
    public void Start_PlannedProject_ChangesStatusToInProgressAndSetsStartDate()
    {
        var project = CreatePlannedProject();
        var before = DateTimeOffset.UtcNow;
        project.Start();

        project.Status.Should().Be(ProjectStatus.InProgress);
        project.StartDate.Should().NotBeNull();
        project.StartDate!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Start_InProgressProject_ThrowsDomainException()
    {
        var project = CreatePlannedProject();
        project.Start();

        var act = () => project.Start();
        act.Should().Throw<DomainException>().WithMessage("*Planned*");
    }

    // ─── Block ──────────────────────────────────────────────────────────────

    [Fact]
    public void Block_PlannedProject_ChangesStatusToBlockedAndSetsReason()
    {
        var project = CreatePlannedProject();
        project.Block("Awaiting budget approval.");

        project.Status.Should().Be(ProjectStatus.Blocked);
        project.BlockedReason.Should().Be("Awaiting budget approval.");
    }

    [Fact]
    public void Block_InProgressProject_ChangesStatusToBlocked()
    {
        var project = CreatePlannedProject();
        project.Start();
        project.Block("Dependency not met.");

        project.Status.Should().Be(ProjectStatus.Blocked);
        project.BlockedReason.Should().Be("Dependency not met.");
    }

    [Fact]
    public void Block_CompletedProject_ThrowsDomainException()
    {
        var project = CreatePlannedProject();
        project.Start();
        project.Complete();

        var act = () => project.Block("reason");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Block_EmptyReason_ThrowsDomainException()
    {
        var project = CreatePlannedProject();
        var act = () => project.Block("   ");
        act.Should().Throw<DomainException>().WithMessage("*reason*");
    }

    // ─── Unblock ────────────────────────────────────────────────────────────

    [Fact]
    public void Unblock_BlockedProject_ChangesStatusToInProgressAndClearsReason()
    {
        var project = CreatePlannedProject();
        project.Block("reason");
        project.Unblock();

        project.Status.Should().Be(ProjectStatus.InProgress);
        project.BlockedReason.Should().BeNull();
    }

    [Fact]
    public void Unblock_InProgressProject_ThrowsDomainException()
    {
        var project = CreatePlannedProject();
        project.Start();

        var act = () => project.Unblock();
        act.Should().Throw<DomainException>().WithMessage("*Blocked*");
    }

    // ─── Complete ───────────────────────────────────────────────────────────

    [Fact]
    public void Complete_InProgressProject_SetsCompletedAtAndChangesStatus()
    {
        var project = CreatePlannedProject();
        project.Start();
        var before = DateTimeOffset.UtcNow;
        project.Complete();

        project.Status.Should().Be(ProjectStatus.Completed);
        project.CompletedAt.Should().NotBeNull();
        project.CompletedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Complete_PlannedProject_ThrowsDomainException()
    {
        var project = CreatePlannedProject();
        var act = () => project.Complete();
        act.Should().Throw<DomainException>().WithMessage("*InProgress*");
    }

    // ─── Cancel ─────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_InProgressProject_ChangesStatusToCancelled()
    {
        var project = CreatePlannedProject();
        project.Start();
        project.Cancel("No longer needed.");

        project.Status.Should().Be(ProjectStatus.Cancelled);
    }

    [Fact]
    public void Cancel_BlockedProject_ChangesStatusToCancelled()
    {
        var project = CreatePlannedProject();
        project.Block("reason");
        project.Cancel("Cancelled while blocked.");

        project.Status.Should().Be(ProjectStatus.Cancelled);
    }

    [Fact]
    public void Cancel_PlannedProject_ThrowsDomainException()
    {
        var project = CreatePlannedProject();
        var act = () => project.Cancel("reason");
        act.Should().Throw<DomainException>();
    }
}

public class ProjectSnapshotTests
{
    [Fact]
    public void Create_FromProject_CapturesAllFields()
    {
        var ownerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var project = Project.Create("My Project", "Desc", ownerId, ProjectPriority.High);
        project.Start();

        var snapshot = ProjectSnapshot.Create(project, "John Doe", "Started", actorId);

        snapshot.ProjectId.Should().Be(project.Id);
        snapshot.Title.Should().Be("My Project");
        snapshot.Status.Should().Be(ProjectStatus.InProgress);
        snapshot.Priority.Should().Be(ProjectPriority.High);
        snapshot.OwnerId.Should().Be(ownerId);
        snapshot.OwnerName.Should().Be("John Doe");
        snapshot.TriggerAction.Should().Be("Started");
        snapshot.TriggeredByActorId.Should().Be(actorId);
        snapshot.SchemaVersion.Should().Be(1);
        snapshot.StartDate.Should().NotBeNull();
        snapshot.TakenAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
