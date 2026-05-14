# Phase 2 — Part 4: Projects Feature Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Part sequence:** Part 4 of 4. Requires Parts 1–3 complete. Completing this part finishes Phase 2.

**Goal:** Deliver the complete Projects feature — all CRUD, state transitions (Start/Complete/Cancel/Block/Unblock), convert-from-idea, audit log writes, and ProjectSnapshot creation on every transition — with unit tests and integration tests.

**Architecture:** Every project state transition creates both an `AuditLog` entry and a `ProjectSnapshot` in the same database transaction via `SaveChangesWithAuditAsync`. The snapshot captures full project state including the owner's name (resolved from the Users table). Role-based visibility filtering mirrors the Ideas pattern: Operators see only projects they own; Managers and Leadership see all.

**Tech Stack:** .NET 8, MediatR 12, EF Core 8, xUnit, FluentAssertions, Moq, WebApplicationFactory.

---

## File Map

**Create:**
- `src/Flow.Application/Projects/ProjectSummaryDto.cs`
- `src/Flow.Application/Projects/ProjectDetailDto.cs`
- `src/Flow.Application/Projects/TimelineEntryDto.cs`
- `src/Flow.Application/Projects/ProjectSnapshotDto.cs`
- `src/Flow.Application/Projects/Commands/CreateProject/CreateProjectCommand.cs`
- `src/Flow.Application/Projects/Commands/CreateProject/CreateProjectCommandHandler.cs`
- `src/Flow.Application/Projects/Commands/ConvertIdeaToProject/ConvertIdeaToProjectCommand.cs`
- `src/Flow.Application/Projects/Commands/ConvertIdeaToProject/ConvertIdeaToProjectCommandHandler.cs`
- `src/Flow.Application/Projects/Commands/UpdateProject/UpdateProjectCommand.cs`
- `src/Flow.Application/Projects/Commands/UpdateProject/UpdateProjectCommandHandler.cs`
- `src/Flow.Application/Projects/Commands/StartProject/StartProjectCommand.cs`
- `src/Flow.Application/Projects/Commands/StartProject/StartProjectCommandHandler.cs`
- `src/Flow.Application/Projects/Commands/CompleteProject/CompleteProjectCommand.cs`
- `src/Flow.Application/Projects/Commands/CompleteProject/CompleteProjectCommandHandler.cs`
- `src/Flow.Application/Projects/Commands/CancelProject/CancelProjectCommand.cs`
- `src/Flow.Application/Projects/Commands/CancelProject/CancelProjectCommandHandler.cs`
- `src/Flow.Application/Projects/Commands/BlockProject/BlockProjectCommand.cs`
- `src/Flow.Application/Projects/Commands/BlockProject/BlockProjectCommandHandler.cs`
- `src/Flow.Application/Projects/Commands/UnblockProject/UnblockProjectCommand.cs`
- `src/Flow.Application/Projects/Commands/UnblockProject/UnblockProjectCommandHandler.cs`
- `src/Flow.Application/Projects/Queries/GetProjects/GetProjectsQuery.cs`
- `src/Flow.Application/Projects/Queries/GetProjects/GetProjectsQueryHandler.cs`
- `src/Flow.Application/Projects/Queries/GetProjectById/GetProjectByIdQuery.cs`
- `src/Flow.Application/Projects/Queries/GetProjectById/GetProjectByIdQueryHandler.cs`
- `src/Flow.Application/Projects/Queries/GetProjectTimeline/GetProjectTimelineQuery.cs`
- `src/Flow.Application/Projects/Queries/GetProjectTimeline/GetProjectTimelineQueryHandler.cs`
- `src/Flow.Application/Projects/Queries/GetProjectSnapshots/GetProjectSnapshotsQuery.cs`
- `src/Flow.Application/Projects/Queries/GetProjectSnapshots/GetProjectSnapshotsQueryHandler.cs`
- `tests/Flow.Application.Tests/Projects/StartProjectCommandHandlerTests.cs`
- `tests/Flow.Application.Tests/Projects/BlockProjectCommandHandlerTests.cs`
- `src/Flow.API/Controllers/ProjectsController.cs`
- `tests/Flow.API.Tests/Projects/ProjectsControllerTests.cs`

---

## Task 8: Projects Application Layer

### Step 8.1 — DTOs

- [ ] **Step 1: Create Project DTOs**

`src/Flow.Application/Projects/ProjectSummaryDto.cs`:
```csharp
namespace Flow.Application.Projects;

public record ProjectSummaryDto(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    Guid OwnerId,
    Guid? SourceIdeaId,
    DateTimeOffset? Deadline,
    string? BlockedReason,
    DateTimeOffset CreatedAt);
```

`src/Flow.Application/Projects/ProjectDetailDto.cs`:
```csharp
namespace Flow.Application.Projects;

public record ProjectDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    Guid OwnerId,
    Guid? SourceIdeaId,
    decimal? EstimatedCost,
    decimal? ActualCost,
    DateTimeOffset? StartDate,
    DateTimeOffset? Deadline,
    DateTimeOffset? CompletedAt,
    string? BlockedReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
```

`src/Flow.Application/Projects/TimelineEntryDto.cs`:
```csharp
namespace Flow.Application.Projects;

public record TimelineEntryDto(
    string Action,
    Guid ActorId,
    string ActorName,
    string? OldValue,
    string? NewValue,
    string? Reason,
    DateTimeOffset Timestamp);
```

`src/Flow.Application/Projects/ProjectSnapshotDto.cs`:
```csharp
namespace Flow.Application.Projects;

public record ProjectSnapshotDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Status,
    string Priority,
    Guid OwnerId,
    string OwnerName,
    decimal? EstimatedCost,
    decimal? ActualCost,
    DateTimeOffset? StartDate,
    DateTimeOffset? Deadline,
    DateTimeOffset? CompletedAt,
    string? BlockedReason,
    string TriggerAction,
    DateTimeOffset TakenAt);
```

### Step 8.2 — Shared snapshot+audit helper pattern

Every project state transition handler follows this exact pattern:
```
1. Find project — NotFoundException if missing
2. Resolve owner (FirstOrDefaultAsync on Users by project.OwnerId)
3. Record oldStatus = project.Status.ToString()
4. Call domain method — can throw DomainException → 400
5. Create snapshot: ProjectSnapshot.Create(project, ownerName, triggerAction, actorId)
6. Create audit: AuditLog.Create("Project", project.Id, triggerAction, actorId, actorName, oldStatus, newStatus)
7. _context.ProjectSnapshots.Add(snapshot)
8. await _context.SaveChangesWithAuditAsync(new[] { audit }, ct)
```

### Step 8.3 — CreateProject

- [ ] **Step 2: Implement CreateProject**

`src/Flow.Application/Projects/Commands/CreateProject/CreateProjectCommand.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using Flow.Domain.Enums;
using MediatR;

namespace Flow.Application.Projects.Commands.CreateProject;

public record CreateProjectCommand(
    [Required] string Title,
    [Required] string Description,
    ProjectPriority Priority,
    Guid OwnerId,
    decimal? EstimatedCost,
    DateTimeOffset? Deadline) : IRequest<ProjectSummaryDto>;
```

`src/Flow.Application/Projects/Commands/CreateProject/CreateProjectCommandHandler.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;

namespace Flow.Application.Projects.Commands.CreateProject;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ProjectSummaryDto> Handle(
        CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId!.Value;
        var actorName = _currentUser.UserName ?? "Unknown";

        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.OwnerId, cancellationToken)
            ?? throw new Flow.Application.Common.Exceptions.NotFoundException("User", request.OwnerId);

        var project = Project.Create(
            request.Title, request.Description, request.OwnerId,
            request.Priority, sourceIdeaId: null,
            request.EstimatedCost, request.Deadline);

        var snapshot = ProjectSnapshot.Create(project, owner.Name, "Created", actorId);
        _context.Projects.Add(project);
        _context.ProjectSnapshots.Add(snapshot);

        var audit = AuditLog.Create(
            "Project", project.Id, "Created", actorId, actorName,
            newValue: project.Status.ToString());

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);

        return new ProjectSummaryDto(
            project.Id, project.Title, project.Status.ToString(), project.Priority.ToString(),
            project.OwnerId, project.SourceIdeaId, project.Deadline, project.BlockedReason,
            project.CreatedAt);
    }
}
```

### Step 8.4 — ConvertIdeaToProject

- [ ] **Step 3: Implement ConvertIdeaToProject**

`src/Flow.Application/Projects/Commands/ConvertIdeaToProject/ConvertIdeaToProjectCommand.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using Flow.Domain.Enums;
using MediatR;

namespace Flow.Application.Projects.Commands.ConvertIdeaToProject;

public record ConvertIdeaToProjectCommand(
    Guid IdeaId,
    [Required] string Title,
    [Required] string Description,
    ProjectPriority Priority,
    Guid OwnerId,
    decimal? EstimatedCost,
    DateTimeOffset? Deadline) : IRequest<ProjectSummaryDto>;
```

`src/Flow.Application/Projects/Commands/ConvertIdeaToProject/ConvertIdeaToProjectCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Commands.ConvertIdeaToProject;

public class ConvertIdeaToProjectCommandHandler
    : IRequestHandler<ConvertIdeaToProjectCommand, ProjectSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ConvertIdeaToProjectCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ProjectSummaryDto> Handle(
        ConvertIdeaToProjectCommand request, CancellationToken cancellationToken)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        if (idea.Status != IdeaStatus.Approved)
            throw new Flow.Domain.Exceptions.DomainException(
                "Only Approved ideas can be converted to projects.");

        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.OwnerId, cancellationToken)
            ?? throw new NotFoundException("User", request.OwnerId);

        var actorId = _currentUser.UserId!.Value;
        var actorName = _currentUser.UserName ?? "Unknown";

        var project = Project.Create(
            request.Title, request.Description, request.OwnerId,
            request.Priority, sourceIdeaId: request.IdeaId,
            request.EstimatedCost, request.Deadline);

        var snapshot = ProjectSnapshot.Create(project, owner.Name, "Created", actorId);
        _context.Projects.Add(project);
        _context.ProjectSnapshots.Add(snapshot);

        var audit = AuditLog.Create(
            "Project", project.Id, "Created", actorId, actorName,
            newValue: project.Status.ToString());

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);

        return new ProjectSummaryDto(
            project.Id, project.Title, project.Status.ToString(), project.Priority.ToString(),
            project.OwnerId, project.SourceIdeaId, project.Deadline, project.BlockedReason,
            project.CreatedAt);
    }
}
```

### Step 8.5 — UpdateProject

- [ ] **Step 4: Implement UpdateProject**

`src/Flow.Application/Projects/Commands/UpdateProject/UpdateProjectCommand.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using Flow.Domain.Enums;
using MediatR;

namespace Flow.Application.Projects.Commands.UpdateProject;

public record UpdateProjectCommand(
    Guid ProjectId,
    [Required] string Title,
    [Required] string Description,
    ProjectPriority Priority,
    Guid OwnerId,
    decimal? EstimatedCost,
    decimal? ActualCost,
    DateTimeOffset? Deadline) : IRequest;
```

`src/Flow.Application/Projects/Commands/UpdateProject/UpdateProjectCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Commands.UpdateProject;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var actorId = _currentUser.UserId!.Value;
        var actorName = _currentUser.UserName ?? "Unknown";

        project.Update(request.Title, request.Description, request.Priority,
            request.OwnerId, request.EstimatedCost, request.ActualCost, request.Deadline);

        var audit = AuditLog.Create(
            "Project", project.Id, "Updated", actorId, actorName);

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
```

### Step 8.6 — Transition Commands (Start, Complete, Cancel, Block, Unblock)

- [ ] **Step 5: Write the failing unit tests for StartProject and BlockProject**

`tests/Flow.Application.Tests/Projects/StartProjectCommandHandlerTests.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Application.Projects.Commands.StartProject;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;

namespace Flow.Application.Tests.Projects;

public class StartProjectCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    public StartProjectCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _currentUserMock.Setup(u => u.UserName).Returns("Manager");
        _contextMock
            .Setup(c => c.SaveChangesWithAuditAsync(
                It.IsAny<IEnumerable<DomainAuditLog>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_PlannedProject_StartsAndCreatesSnapshotAndAudit()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("My Project", "Desc", ownerId, ProjectPriority.Medium);
        var owner = User.Create("Owner", "owner@test.com", UserRole.Manager);

        var mockProjectSet = MockDbSetHelper.BuildMockDbSet(new[] { project });
        var mockUserSet = MockDbSetHelper.BuildMockDbSet(new[] { owner });

        var capturedSnapshots = new List<ProjectSnapshot>();
        var mockSnapshotSet = MockDbSetHelper.BuildMockDbSet<ProjectSnapshot>(Array.Empty<ProjectSnapshot>());
        mockSnapshotSet.Setup(s => s.Add(It.IsAny<ProjectSnapshot>()))
            .Callback<ProjectSnapshot>(s => capturedSnapshots.Add(s));

        _contextMock.Setup(c => c.Projects).Returns(mockProjectSet.Object);
        _contextMock.Setup(c => c.Users).Returns(mockUserSet.Object);
        _contextMock.Setup(c => c.ProjectSnapshots).Returns(mockSnapshotSet.Object);

        var handler = new StartProjectCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(new StartProjectCommand(project.Id), CancellationToken.None);

        project.Status.Should().Be(ProjectStatus.InProgress);
        project.StartDate.Should().NotBeNull();

        capturedSnapshots.Should().HaveCount(1);
        capturedSnapshots[0].TriggerAction.Should().Be("Started");
        capturedSnapshots[0].Status.Should().Be(ProjectStatus.InProgress);

        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l => l.Action == "Started" && l.EntityId == project.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

`tests/Flow.Application.Tests/Projects/BlockProjectCommandHandlerTests.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Application.Projects.Commands.BlockProject;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;

namespace Flow.Application.Tests.Projects;

public class BlockProjectCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    public BlockProjectCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _currentUserMock.Setup(u => u.UserName).Returns("Manager");
        _contextMock
            .Setup(c => c.SaveChangesWithAuditAsync(
                It.IsAny<IEnumerable<DomainAuditLog>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_PlannedProject_BlocksAndCreatesSnapshotWithReason()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Project", "Desc", ownerId, ProjectPriority.High);
        var owner = User.Create("Owner", "owner@test.com", UserRole.Manager);

        var mockProjectSet = MockDbSetHelper.BuildMockDbSet(new[] { project });
        var mockUserSet = MockDbSetHelper.BuildMockDbSet(new[] { owner });

        var capturedSnapshots = new List<ProjectSnapshot>();
        var mockSnapshotSet = MockDbSetHelper.BuildMockDbSet<ProjectSnapshot>(Array.Empty<ProjectSnapshot>());
        mockSnapshotSet.Setup(s => s.Add(It.IsAny<ProjectSnapshot>()))
            .Callback<ProjectSnapshot>(s => capturedSnapshots.Add(s));

        _contextMock.Setup(c => c.Projects).Returns(mockProjectSet.Object);
        _contextMock.Setup(c => c.Users).Returns(mockUserSet.Object);
        _contextMock.Setup(c => c.ProjectSnapshots).Returns(mockSnapshotSet.Object);

        var handler = new BlockProjectCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(
            new BlockProjectCommand(project.Id, "Awaiting budget approval."),
            CancellationToken.None);

        project.Status.Should().Be(ProjectStatus.Blocked);
        project.BlockedReason.Should().Be("Awaiting budget approval.");

        capturedSnapshots.Should().HaveCount(1);
        capturedSnapshots[0].TriggerAction.Should().Be("Blocked");
        capturedSnapshots[0].BlockedReason.Should().Be("Awaiting budget approval.");

        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l =>
                        l.Action == "Blocked" &&
                        l.Reason == "Awaiting budget approval." &&
                        l.EntityId == project.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

- [ ] **Step 6: Run tests to confirm they fail**

```
dotnet test tests/Flow.Application.Tests/ --filter "StartProjectCommandHandlerTests|BlockProjectCommandHandlerTests" -v minimal
```

Expected: FAIL — handlers do not exist yet.

- [ ] **Step 7: Implement all five transition commands and handlers**

`src/Flow.Application/Projects/Commands/StartProject/StartProjectCommand.cs`:
```csharp
using MediatR;

namespace Flow.Application.Projects.Commands.StartProject;

public record StartProjectCommand(Guid ProjectId) : IRequest;
```

`src/Flow.Application/Projects/Commands/StartProject/StartProjectCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Commands.StartProject;

public class StartProjectCommandHandler : IRequestHandler<StartProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public StartProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(StartProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == project.OwnerId, cancellationToken);
        var ownerName = owner?.Name ?? "Unknown";

        var actorId = _currentUser.UserId!.Value;
        var actorName = _currentUser.UserName ?? "Unknown";
        var oldStatus = project.Status.ToString();

        project.Start();

        var snapshot = ProjectSnapshot.Create(project, ownerName, "Started", actorId);
        _context.ProjectSnapshots.Add(snapshot);

        var audit = AuditLog.Create(
            "Project", project.Id, "Started", actorId, actorName,
            oldValue: oldStatus, newValue: project.Status.ToString());

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
```

`src/Flow.Application/Projects/Commands/CompleteProject/CompleteProjectCommand.cs`:
```csharp
using MediatR;

namespace Flow.Application.Projects.Commands.CompleteProject;

public record CompleteProjectCommand(Guid ProjectId) : IRequest;
```

`src/Flow.Application/Projects/Commands/CompleteProject/CompleteProjectCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Commands.CompleteProject;

public class CompleteProjectCommandHandler : IRequestHandler<CompleteProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CompleteProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(CompleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == project.OwnerId, cancellationToken);
        var ownerName = owner?.Name ?? "Unknown";

        var actorId = _currentUser.UserId!.Value;
        var actorName = _currentUser.UserName ?? "Unknown";
        var oldStatus = project.Status.ToString();

        project.Complete();

        var snapshot = ProjectSnapshot.Create(project, ownerName, "Completed", actorId);
        _context.ProjectSnapshots.Add(snapshot);

        var audit = AuditLog.Create(
            "Project", project.Id, "Completed", actorId, actorName,
            oldValue: oldStatus, newValue: project.Status.ToString());

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
```

`src/Flow.Application/Projects/Commands/CancelProject/CancelProjectCommand.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Projects.Commands.CancelProject;

public record CancelProjectCommand(Guid ProjectId, [Required] string Reason) : IRequest;
```

`src/Flow.Application/Projects/Commands/CancelProject/CancelProjectCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Commands.CancelProject;

public class CancelProjectCommandHandler : IRequestHandler<CancelProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CancelProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(CancelProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == project.OwnerId, cancellationToken);
        var ownerName = owner?.Name ?? "Unknown";

        var actorId = _currentUser.UserId!.Value;
        var actorName = _currentUser.UserName ?? "Unknown";
        var oldStatus = project.Status.ToString();

        project.Cancel(request.Reason);

        var snapshot = ProjectSnapshot.Create(project, ownerName, "Cancelled", actorId);
        _context.ProjectSnapshots.Add(snapshot);

        var audit = AuditLog.Create(
            "Project", project.Id, "Cancelled", actorId, actorName,
            oldValue: oldStatus, newValue: project.Status.ToString(),
            reason: request.Reason);

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
```

`src/Flow.Application/Projects/Commands/BlockProject/BlockProjectCommand.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Projects.Commands.BlockProject;

public record BlockProjectCommand(Guid ProjectId, [Required] string Reason) : IRequest;
```

`src/Flow.Application/Projects/Commands/BlockProject/BlockProjectCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Commands.BlockProject;

public class BlockProjectCommandHandler : IRequestHandler<BlockProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public BlockProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(BlockProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == project.OwnerId, cancellationToken);
        var ownerName = owner?.Name ?? "Unknown";

        var actorId = _currentUser.UserId!.Value;
        var actorName = _currentUser.UserName ?? "Unknown";
        var oldStatus = project.Status.ToString();

        project.Block(request.Reason);

        var snapshot = ProjectSnapshot.Create(project, ownerName, "Blocked", actorId);
        _context.ProjectSnapshots.Add(snapshot);

        var audit = AuditLog.Create(
            "Project", project.Id, "Blocked", actorId, actorName,
            oldValue: oldStatus, newValue: project.Status.ToString(),
            reason: request.Reason);

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
```

`src/Flow.Application/Projects/Commands/UnblockProject/UnblockProjectCommand.cs`:
```csharp
using MediatR;

namespace Flow.Application.Projects.Commands.UnblockProject;

public record UnblockProjectCommand(Guid ProjectId) : IRequest;
```

`src/Flow.Application/Projects/Commands/UnblockProject/UnblockProjectCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Commands.UnblockProject;

public class UnblockProjectCommandHandler : IRequestHandler<UnblockProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UnblockProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UnblockProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == project.OwnerId, cancellationToken);
        var ownerName = owner?.Name ?? "Unknown";

        var actorId = _currentUser.UserId!.Value;
        var actorName = _currentUser.UserName ?? "Unknown";
        var oldStatus = project.Status.ToString();

        project.Unblock();

        var snapshot = ProjectSnapshot.Create(project, ownerName, "Unblocked", actorId);
        _context.ProjectSnapshots.Add(snapshot);

        var audit = AuditLog.Create(
            "Project", project.Id, "Unblocked", actorId, actorName,
            oldValue: oldStatus, newValue: project.Status.ToString());

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
```

- [ ] **Step 8: Run transition handler tests**

```
dotnet test tests/Flow.Application.Tests/ --filter "StartProjectCommandHandlerTests|BlockProjectCommandHandlerTests" -v minimal
```

Expected: **2 tests passing**, 0 failures.

### Step 8.7 — Queries

- [ ] **Step 9: Implement all four Project queries**

`src/Flow.Application/Projects/Queries/GetProjects/GetProjectsQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Projects.Queries.GetProjects;

public record GetProjectsQuery(Guid? OwnerId) : IRequest<IReadOnlyList<ProjectSummaryDto>>;
```

`src/Flow.Application/Projects/Queries/GetProjects/GetProjectsQueryHandler.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Queries.GetProjects;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, IReadOnlyList<ProjectSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProjectSummaryDto>> Handle(
        GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Projects.AsQueryable();

        if (request.OwnerId.HasValue)
            query = query.Where(p => p.OwnerId == request.OwnerId.Value);

        var projects = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return projects
            .Select(p => new ProjectSummaryDto(
                p.Id, p.Title, p.Status.ToString(), p.Priority.ToString(),
                p.OwnerId, p.SourceIdeaId, p.Deadline, p.BlockedReason, p.CreatedAt))
            .ToList();
    }
}
```

`src/Flow.Application/Projects/Queries/GetProjectById/GetProjectByIdQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDetailDto>;
```

`src/Flow.Application/Projects/Queries/GetProjectById/GetProjectByIdQueryHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Queries.GetProjectById;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetProjectByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ProjectDetailDto> Handle(
        GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        return new ProjectDetailDto(
            project.Id, project.Title, project.Description,
            project.Status.ToString(), project.Priority.ToString(),
            project.OwnerId, project.SourceIdeaId,
            project.EstimatedCost, project.ActualCost,
            project.StartDate, project.Deadline, project.CompletedAt,
            project.BlockedReason, project.CreatedAt, project.UpdatedAt);
    }
}
```

`src/Flow.Application/Projects/Queries/GetProjectTimeline/GetProjectTimelineQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Projects.Queries.GetProjectTimeline;

public record GetProjectTimelineQuery(Guid ProjectId) : IRequest<IReadOnlyList<TimelineEntryDto>>;
```

`src/Flow.Application/Projects/Queries/GetProjectTimeline/GetProjectTimelineQueryHandler.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Queries.GetProjectTimeline;

public class GetProjectTimelineQueryHandler
    : IRequestHandler<GetProjectTimelineQuery, IReadOnlyList<TimelineEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectTimelineQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<TimelineEntryDto>> Handle(
        GetProjectTimelineQuery request, CancellationToken cancellationToken)
    {
        var entries = await _context.AuditLogs
            .Where(a => a.EntityType == "Project" && a.EntityId == request.ProjectId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        return entries
            .Select(a => new TimelineEntryDto(
                a.Action, a.ActorId, a.ActorName,
                a.OldValue, a.NewValue, a.Reason, a.Timestamp))
            .ToList();
    }
}
```

`src/Flow.Application/Projects/Queries/GetProjectSnapshots/GetProjectSnapshotsQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Projects.Queries.GetProjectSnapshots;

public record GetProjectSnapshotsQuery(Guid ProjectId) : IRequest<IReadOnlyList<ProjectSnapshotDto>>;
```

`src/Flow.Application/Projects/Queries/GetProjectSnapshots/GetProjectSnapshotsQueryHandler.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Projects.Queries.GetProjectSnapshots;

public class GetProjectSnapshotsQueryHandler
    : IRequestHandler<GetProjectSnapshotsQuery, IReadOnlyList<ProjectSnapshotDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectSnapshotsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProjectSnapshotDto>> Handle(
        GetProjectSnapshotsQuery request, CancellationToken cancellationToken)
    {
        var snapshots = await _context.ProjectSnapshots
            .Where(s => s.ProjectId == request.ProjectId)
            .OrderBy(s => s.TakenAt)
            .ToListAsync(cancellationToken);

        return snapshots
            .Select(s => new ProjectSnapshotDto(
                s.Id, s.ProjectId, s.Title,
                s.Status.ToString(), s.Priority.ToString(),
                s.OwnerId, s.OwnerName,
                s.EstimatedCost, s.ActualCost,
                s.StartDate, s.Deadline, s.CompletedAt,
                s.BlockedReason, s.TriggerAction, s.TakenAt))
            .ToList();
    }
}
```

- [ ] **Step 10: Build to confirm no compile errors**

```
dotnet build src/Flow.Application/Flow.Application.csproj -v minimal
```

Expected: **Build succeeded**, 0 errors.

- [ ] **Step 11: Run all tests so far**

```
dotnet test -v minimal
```

Expected: **81 tests passing** (79 from Part 3 + 2 new handler tests), 0 failures.

- [ ] **Step 12: Commit application layer**

```
git add src/Flow.Application/Projects/
git add tests/Flow.Application.Tests/Projects/
git commit -m "feat: implement Projects application layer with state transitions, snapshots, and audit logs"
```

---

## Task 9: Projects Controller and Integration Tests

- [ ] **Step 1: Write the failing integration tests**

`tests/Flow.API.Tests/Projects/ProjectsControllerTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Xunit;

namespace Flow.API.Tests.Projects;

public class ProjectsControllerTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly FlowWebApplicationFactory _factory;

    public ProjectsControllerTests(FlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, string ManagerToken)> CreateManagerClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Manager");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token);
    }

    [Fact]
    public async Task CreateProject_AsManager_Returns201WithPlannedStatus()
    {
        var (client, _) = await CreateManagerClientAsync();

        // OwnerId must be a real user — use the manager's own ID via /api/v1/me or re-register
        // For simplicity, create a second manager to use as owner
        var ownerId = await GetManagerIdAsync();

        var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "Automate Onboarding",
            description = "Reduce onboarding time by 50%.",
            priority = "High",
            ownerId,
            estimatedCost = 15000.00m,
            deadline = DateTimeOffset.UtcNow.AddMonths(3)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Planned");
        body.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task StartProject_AsManager_Returns204AndStatusIsInProgress()
    {
        var (client, _) = await CreateManagerClientAsync();
        var ownerId = await GetManagerIdAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "Project To Start",
            description = "Desc",
            priority = "Medium",
            ownerId,
            estimatedCost = (decimal?)null,
            deadline = (DateTimeOffset?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var startResponse = await client.PostAsync($"/api/v1/projects/{id}/start", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await client.GetAsync($"/api/v1/projects/{id}");
        var body = await detail.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("InProgress");
    }

    [Fact]
    public async Task BlockProject_AsManager_Returns204WithBlockedReason()
    {
        var (client, _) = await CreateManagerClientAsync();
        var ownerId = await GetManagerIdAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "Project To Block",
            description = "Desc",
            priority = "Low",
            ownerId,
            estimatedCost = (decimal?)null,
            deadline = (DateTimeOffset?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var blockResponse = await client.PostAsJsonAsync(
            $"/api/v1/projects/{id}/block",
            new { reason = "Awaiting board approval." });

        blockResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await client.GetAsync($"/api/v1/projects/{id}");
        var body = await detail.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Blocked");
        body.GetProperty("blockedReason").GetString().Should().Be("Awaiting board approval.");
    }

    [Fact]
    public async Task GetProjectTimeline_AfterTransitions_ReturnsAuditEntries()
    {
        var (client, _) = await CreateManagerClientAsync();
        var ownerId = await GetManagerIdAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "Timeline Project",
            description = "Desc",
            priority = "Medium",
            ownerId,
            estimatedCost = (decimal?)null,
            deadline = (DateTimeOffset?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        await client.PostAsync($"/api/v1/projects/{id}/start", null);

        var timelineResponse = await client.GetAsync($"/api/v1/projects/{id}/timeline");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var timeline = await timelineResponse.Content.ReadFromJsonAsync<JsonElement>();
        timeline.GetArrayLength().Should().BeGreaterThanOrEqualTo(2); // Created + Started
    }

    [Fact]
    public async Task GetProjectSnapshots_AfterTransitions_ReturnsSnapshots()
    {
        var (client, _) = await CreateManagerClientAsync();
        var ownerId = await GetManagerIdAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "Snapshot Project",
            description = "Desc",
            priority = "High",
            ownerId,
            estimatedCost = (decimal?)null,
            deadline = (DateTimeOffset?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        await client.PostAsync($"/api/v1/projects/{id}/start", null);

        var snapshotsResponse = await client.GetAsync($"/api/v1/projects/{id}/snapshots");
        snapshotsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var snapshots = await snapshotsResponse.Content.ReadFromJsonAsync<JsonElement>();
        snapshots.GetArrayLength().Should().BeGreaterThanOrEqualTo(2); // Created + Started
    }

    [Fact]
    public async Task ConvertIdeaToProject_ApprovedIdea_Returns201()
    {
        // Create and approve an idea
        var operatorClient = _factory.CreateClient();
        var operatorToken = await _factory.GetTokenForRoleAsync("Operator");
        operatorClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", operatorToken);

        var ideaResponse = await operatorClient.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "Convertible Idea",
            description = "To be converted",
            problem = "Problem",
            linkedGuidelineId = (string?)null
        });
        var idea = await ideaResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ideaId = idea.GetProperty("id").GetString()!;
        await operatorClient.PostAsync($"/api/v1/ideas/{ideaId}/submit", null);

        var (managerClient, _) = await CreateManagerClientAsync();
        await managerClient.PostAsJsonAsync($"/api/v1/ideas/{ideaId}/approve",
            new { managerComment = (string?)null });

        // Convert to project
        var ownerId = await GetManagerIdAsync();
        var convertResponse = await managerClient.PostAsJsonAsync(
            $"/api/v1/ideas/{ideaId}/convert",
            new
            {
                title = "Project from Idea",
                description = "Desc",
                priority = "Medium",
                ownerId,
                estimatedCost = (decimal?)null,
                deadline = (DateTimeOffset?)null
            });

        convertResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = await convertResponse.Content.ReadFromJsonAsync<JsonElement>();
        project.GetProperty("status").GetString().Should().Be("Planned");
    }

    // Helper: create a Manager user and return their UserId as string
    private async Task<string> GetManagerIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Flow.Domain.Entities.User>>();
        var email = $"owner-{Guid.NewGuid():N}@flow.test";
        var user = Flow.Domain.Entities.User.Create("Project Owner", email, Flow.Domain.Enums.UserRole.Manager);
        await userManager.CreateAsync(user, "Test123!");
        await userManager.AddToRoleAsync(user, "Manager");
        return user.Id.ToString();
    }
}
```

- [ ] **Step 2: Run failing tests**

```
dotnet test tests/Flow.API.Tests/ --filter "ProjectsControllerTests" -v minimal
```

Expected: FAIL — `ProjectsController` does not exist yet.

- [ ] **Step 3: Implement ProjectsController**

`src/Flow.API/Controllers/ProjectsController.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Application.Projects;
using Flow.Application.Projects.Commands.BlockProject;
using Flow.Application.Projects.Commands.CancelProject;
using Flow.Application.Projects.Commands.CompleteProject;
using Flow.Application.Projects.Commands.ConvertIdeaToProject;
using Flow.Application.Projects.Commands.CreateProject;
using Flow.Application.Projects.Commands.StartProject;
using Flow.Application.Projects.Commands.UnblockProject;
using Flow.Application.Projects.Commands.UpdateProject;
using Flow.Application.Projects.Queries.GetProjectById;
using Flow.Application.Projects.Queries.GetProjectSnapshots;
using Flow.Application.Projects.Queries.GetProjectTimeline;
using Flow.Application.Projects.Queries.GetProjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ProjectsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("projects")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<ProjectSummaryDto>> Create(
        [FromBody] CreateProjectCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("ideas/{ideaId:guid}/convert")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<ProjectSummaryDto>> ConvertFromIdea(
        Guid ideaId, [FromBody] ConvertIdeaToProjectCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command with { IdeaId = ideaId }, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("projects")]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> GetAll(CancellationToken ct)
    {
        var isOperator = User.IsInRole("Operator");
        var ownerId = isOperator ? _currentUser.UserId : null;
        var result = await _mediator.Send(new GetProjectsQuery(ownerId), ct);
        return Ok(result);
    }

    [HttpGet("projects/{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPut("projects/{id:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateProjectCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { ProjectId = id }, ct);
        return NoContent();
    }

    [HttpPost("projects/{id:guid}/start")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new StartProjectCommand(id), ct);
        return NoContent();
    }

    [HttpPost("projects/{id:guid}/complete")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CompleteProjectCommand(id), ct);
        return NoContent();
    }

    [HttpPost("projects/{id:guid}/cancel")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelProjectCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { ProjectId = id }, ct);
        return NoContent();
    }

    [HttpPost("projects/{id:guid}/block")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Block(
        Guid id, [FromBody] BlockProjectCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { ProjectId = id }, ct);
        return NoContent();
    }

    [HttpPost("projects/{id:guid}/unblock")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new UnblockProjectCommand(id), ct);
        return NoContent();
    }

    [HttpGet("projects/{id:guid}/timeline")]
    public async Task<ActionResult<IReadOnlyList<TimelineEntryDto>>> GetTimeline(
        Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectTimelineQuery(id), ct);
        return Ok(result);
    }

    [HttpGet("projects/{id:guid}/snapshots")]
    [Authorize(Roles = "Manager,Leadership")]
    public async Task<ActionResult<IReadOnlyList<ProjectSnapshotDto>>> GetSnapshots(
        Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectSnapshotsQuery(id), ct);
        return Ok(result);
    }
}
```

- [ ] **Step 4: Run Projects integration tests**

```
dotnet test tests/Flow.API.Tests/ --filter "ProjectsControllerTests" -v minimal
```

Expected: **6 tests passing**, 0 failures.

- [ ] **Step 5: Run the full test suite**

```
dotnet test -v minimal
```

Expected: **87 tests passing** (81 + 6 new), 0 failures.

- [ ] **Step 6: Commit**

```
git add src/Flow.API/Controllers/ProjectsController.cs tests/Flow.API.Tests/Projects/
git commit -m "feat: implement Projects controller with full state machine, timeline, and snapshot endpoints"
```

---

## Phase 2 Complete — Final Verification

- [ ] **Run the complete test suite one final time**

```
dotnet test -v minimal
```

Expected output (exact counts depend on implementation — use as a reference):
```
Aprovado! – Com falha: 0, Aprovado: 87, Ignorado: 0
```

- [ ] **Verify the solution builds cleanly**

```
dotnet build -v minimal
```

Expected: **Build succeeded**, 0 errors, warnings only for NU1603 (package version approximation — pre-existing, not new).

- [ ] **Tag Phase 2 completion**

```
git tag phase2-complete
```

---

**Phase 2 delivered.**

The following are now implemented, tested, and working end-to-end:

| Module | Endpoints | State Machine | Audit Log | Snapshot |
|---|---|---|---|---|
| Strategic Guidelines | GET, POST, PUT, DELETE | — | — | — |
| Ideas | POST, GET, GET/:id, PUT, /submit, /approve, /reject, /comments | ✅ Draft→UnderReview→Approved/Rejected | ✅ | — |
| Projects | POST, GET, GET/:id, PUT, /start, /complete, /cancel, /block, /unblock, /timeline, /snapshots | ✅ Full 5-state machine | ✅ | ✅ |
| Convert idea→project | POST /ideas/:id/convert | — | ✅ | ✅ |

**Proceed to Phase 3 (`2026-05-14-phase3-results-dashboard.md`) for ROI engine, dashboard aggregations, and gamification endpoints.**
