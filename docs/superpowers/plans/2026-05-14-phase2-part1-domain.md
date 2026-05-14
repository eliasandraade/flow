# Phase 2 — Part 1: Domain Foundations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Part sequence:** Part 1 of 4. Complete all tasks here before starting Part 2 (`2026-05-14-phase2-part2-infrastructure.md`).

**Goal:** Establish the domain layer for Phase 2 — enums, domain exception, updated middleware, Idea and Project state machines with full test coverage.

**Architecture:** Domain entities own all state transition logic and throw `DomainException` on invalid transitions. The Application layer catches these as 400 Bad Request via the existing `ExceptionHandlingMiddleware`. No EF or application concerns enter the domain layer.

**Tech Stack:** .NET 8, xUnit, FluentAssertions, Moq. All domain files are in `src/Flow.Domain`. All test files are in `tests/Flow.Application.Tests`.

---

## File Map

**Create:**
- `src/Flow.Domain/Enums/IdeaStatus.cs`
- `src/Flow.Domain/Enums/IdeaPriority.cs`
- `src/Flow.Domain/Enums/ProjectStatus.cs`
- `src/Flow.Domain/Enums/ProjectPriority.cs`
- `src/Flow.Domain/Exceptions/DomainException.cs`
- `src/Flow.Domain/Entities/Idea.cs`
- `src/Flow.Domain/Entities/IdeaComment.cs`
- `src/Flow.Domain/Entities/Project.cs`
- `src/Flow.Domain/Entities/ProjectSnapshot.cs`
- `tests/Flow.Application.Tests/Domain/IdeaEntityTests.cs`
- `tests/Flow.Application.Tests/Domain/ProjectEntityTests.cs`

**Modify:**
- `src/Flow.API/Middleware/ExceptionHandlingMiddleware.cs` — add DomainException → 400 case
- `tests/Flow.API.Tests/Auth/ExceptionMiddlewareTests.cs` — add DomainException test

---

## Task 1: Domain Enums, DomainException, Middleware Update

**Files:**
- Create: `src/Flow.Domain/Enums/IdeaStatus.cs`
- Create: `src/Flow.Domain/Enums/IdeaPriority.cs`
- Create: `src/Flow.Domain/Enums/ProjectStatus.cs`
- Create: `src/Flow.Domain/Enums/ProjectPriority.cs`
- Create: `src/Flow.Domain/Exceptions/DomainException.cs`
- Modify: `src/Flow.API/Middleware/ExceptionHandlingMiddleware.cs`
- Test: `tests/Flow.API.Tests/Auth/ExceptionMiddlewareTests.cs`

- [ ] **Step 1: Create the four domain enums**

`src/Flow.Domain/Enums/IdeaStatus.cs`:
```csharp
namespace Flow.Domain.Enums;

public enum IdeaStatus
{
    Draft,
    UnderReview,
    Approved,
    Rejected
}
```

`src/Flow.Domain/Enums/IdeaPriority.cs`:
```csharp
namespace Flow.Domain.Enums;

public enum IdeaPriority
{
    Low,
    Medium,
    High
}
```

`src/Flow.Domain/Enums/ProjectStatus.cs`:
```csharp
namespace Flow.Domain.Enums;

public enum ProjectStatus
{
    Planned,
    InProgress,
    Completed,
    Cancelled,
    Blocked
}
```

`src/Flow.Domain/Enums/ProjectPriority.cs`:
```csharp
namespace Flow.Domain.Enums;

public enum ProjectPriority
{
    Low,
    Medium,
    High,
    Critical
}
```

- [ ] **Step 2: Create DomainException**

`src/Flow.Domain/Exceptions/DomainException.cs`:
```csharp
namespace Flow.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
```

- [ ] **Step 3: Update ExceptionHandlingMiddleware to map DomainException → 400**

In `src/Flow.API/Middleware/ExceptionHandlingMiddleware.cs`, add a case **before** the wildcard `_` arm in the switch expression:

```csharp
// Existing switch expression — add the DomainException case:
var (statusCode, title, errors) = exception switch
{
    Application.Common.Exceptions.ValidationException ve =>
        (HttpStatusCode.BadRequest, "Validation failed", (object?)ve.Errors),
    NotFoundException nfe =>
        (HttpStatusCode.NotFound, nfe.Message, (object?)null),
    ConflictException ce =>
        (HttpStatusCode.Conflict, ce.Message, (object?)null),
    ForbiddenException fe =>
        (HttpStatusCode.Forbidden, fe.Message, (object?)null),
    Flow.Domain.Exceptions.DomainException de =>          // ← ADD THIS CASE
        (HttpStatusCode.BadRequest, de.Message, (object?)null),
    _ =>
        (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (object?)null)
};
```

- [ ] **Step 4: Write failing test for DomainException → 400**

Open `tests/Flow.API.Tests/Auth/ExceptionMiddlewareTests.cs` and add this test to the existing class:

```csharp
[Fact]
public async Task DomainException_Returns400WithMessage()
{
    var client = _factory.CreateClient();
    ExceptionMiddlewareTestEndpoints.NextException =
        new Flow.Domain.Exceptions.DomainException("Invalid state transition.");

    var response = await client.GetAsync("/test-exception");

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
    body.GetProperty("title").GetString().Should().Be("Invalid state transition.");
}
```

- [ ] **Step 5: Run the new test to verify it fails**

```
dotnet test tests/Flow.API.Tests/Flow.API.Tests.csproj --filter "DomainException_Returns400" -v minimal
```

Expected: FAIL — because the middleware doesn't handle `DomainException` yet (Step 3 above hasn't been applied yet — do Step 3 first if the test fails for the wrong reason).

After applying Step 3, rerun:
```
dotnet test tests/Flow.API.Tests/Flow.API.Tests.csproj --filter "DomainException_Returns400" -v minimal
```

Expected: PASS

- [ ] **Step 6: Run full test suite to confirm no regressions**

```
dotnet test -v minimal
```

Expected: all 38 existing + 1 new = **39 tests passing**, 0 failures.

- [ ] **Step 7: Commit**

```
git add src/Flow.Domain/Enums/ src/Flow.Domain/Exceptions/ src/Flow.API/Middleware/ExceptionHandlingMiddleware.cs tests/Flow.API.Tests/Auth/ExceptionMiddlewareTests.cs
git commit -m "feat: add domain enums, DomainException, and middleware mapping"
```

---

## Task 2: Idea Entity and IdeaComment Entity

**Files:**
- Create: `src/Flow.Domain/Entities/Idea.cs`
- Create: `src/Flow.Domain/Entities/IdeaComment.cs`
- Create: `tests/Flow.Application.Tests/Domain/IdeaEntityTests.cs`

- [ ] **Step 1: Write the failing tests**

`tests/Flow.Application.Tests/Domain/IdeaEntityTests.cs`:
```csharp
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
```

- [ ] **Step 2: Run tests to confirm they fail**

```
dotnet test tests/Flow.Application.Tests/ --filter "IdeaEntityTests" -v minimal
```

Expected: FAIL — `Idea` type does not exist yet.

- [ ] **Step 3: Implement Idea.cs**

`src/Flow.Domain/Entities/Idea.cs`:
```csharp
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
```

- [ ] **Step 4: Implement IdeaComment.cs**

`src/Flow.Domain/Entities/IdeaComment.cs`:
```csharp
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
```

- [ ] **Step 5: Run tests to confirm they pass**

```
dotnet test tests/Flow.Application.Tests/ --filter "IdeaEntityTests" -v minimal
```

Expected: **9 tests passing**, 0 failures.

- [ ] **Step 6: Commit**

```
git add src/Flow.Domain/Entities/Idea.cs src/Flow.Domain/Entities/IdeaComment.cs tests/Flow.Application.Tests/Domain/IdeaEntityTests.cs
git commit -m "feat: add Idea and IdeaComment domain entities with state machine"
```

---

## Task 3: Project Entity and ProjectSnapshot Entity

**Files:**
- Create: `src/Flow.Domain/Entities/Project.cs`
- Create: `src/Flow.Domain/Entities/ProjectSnapshot.cs`
- Create: `tests/Flow.Application.Tests/Domain/ProjectEntityTests.cs`

- [ ] **Step 1: Write the failing tests**

`tests/Flow.Application.Tests/Domain/ProjectEntityTests.cs`:
```csharp
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
        project.Description.Should().Be("Desc");
        project.OwnerId.Should().Be(ownerId);
        project.Status.Should().Be(ProjectStatus.Planned);
        project.Priority.Should().Be(ProjectPriority.High);
        project.BlockedReason.Should().BeNull();
        project.CompletedAt.Should().BeNull();
        project.StartDate.Should().BeNull();
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
        act.Should().Throw<DomainException>()
            .WithMessage("*Planned*");
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
        act.Should().Throw<DomainException>()
            .WithMessage("*Blocked*");
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
        act.Should().Throw<DomainException>()
            .WithMessage("*InProgress*");
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
        snapshot.TakenAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```
dotnet test tests/Flow.Application.Tests/ --filter "ProjectEntityTests|ProjectSnapshotTests" -v minimal
```

Expected: FAIL — `Project` and `ProjectSnapshot` types do not exist yet.

- [ ] **Step 3: Implement Project.cs**

`src/Flow.Domain/Entities/Project.cs`:
```csharp
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
```

- [ ] **Step 4: Implement ProjectSnapshot.cs**

`src/Flow.Domain/Entities/ProjectSnapshot.cs`:
```csharp
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
            TakenAt = DateTimeOffset.UtcNow,
            TriggerAction = triggerAction,
            TriggeredByActorId = triggeredByActorId,
            SchemaVersion = 1
        };
    }
}
```

- [ ] **Step 5: Run all domain tests**

```
dotnet test tests/Flow.Application.Tests/ --filter "ProjectEntityTests|ProjectSnapshotTests|IdeaEntityTests" -v minimal
```

Expected: **23 tests passing** (9 Idea + 13 Project + 1 Snapshot), 0 failures.

- [ ] **Step 6: Run full test suite to confirm no regressions**

```
dotnet test -v minimal
```

Expected: **62 tests passing** (39 existing + 23 new domain tests), 0 failures.

- [ ] **Step 7: Commit**

```
git add src/Flow.Domain/Entities/Project.cs src/Flow.Domain/Entities/ProjectSnapshot.cs tests/Flow.Application.Tests/Domain/ProjectEntityTests.cs
git commit -m "feat: add Project and ProjectSnapshot domain entities with full state machine"
```

---

**End of Part 1. Proceed to `2026-05-14-phase2-part2-infrastructure.md`.**
