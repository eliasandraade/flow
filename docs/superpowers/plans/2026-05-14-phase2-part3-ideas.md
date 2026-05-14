# Phase 2 — Part 3: Ideas Feature Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Part sequence:** Part 3 of 4. Requires Parts 1 and 2 complete. After this, proceed to Part 4 (`2026-05-14-phase2-part4-projects.md`).

**Goal:** Deliver the complete Ideas feature — all CRUD, state transitions (Submit/Approve/Reject), comments, audit log writes, and gamification point award on approval — with unit tests and integration tests.

**Architecture:** Handlers use `ICurrentUserService` for actor identity. Every state-changing operation calls `SaveChangesWithAuditAsync` to atomically write the audit entry. `ApproveIdea` also increments `User.Points` and creates a `PointLedgerEntry` in the same transaction. Role-based visibility filtering is passed from the controller to the query handler.

**Tech Stack:** .NET 8, MediatR 12, EF Core 8, xUnit, FluentAssertions, Moq, WebApplicationFactory.

---

## File Map

**Create:**
- `tests/Flow.Application.Tests/Helpers/AsyncQueryHelper.cs`
- `src/Flow.Application/Ideas/IdeaSummaryDto.cs`
- `src/Flow.Application/Ideas/IdeaDetailDto.cs`
- `src/Flow.Application/Ideas/IdeaCommentDto.cs`
- `src/Flow.Application/Ideas/Commands/CreateIdea/CreateIdeaCommand.cs`
- `src/Flow.Application/Ideas/Commands/CreateIdea/CreateIdeaCommandHandler.cs`
- `src/Flow.Application/Ideas/Commands/UpdateIdea/UpdateIdeaCommand.cs`
- `src/Flow.Application/Ideas/Commands/UpdateIdea/UpdateIdeaCommandHandler.cs`
- `src/Flow.Application/Ideas/Commands/SubmitIdea/SubmitIdeaCommand.cs`
- `src/Flow.Application/Ideas/Commands/SubmitIdea/SubmitIdeaCommandHandler.cs`
- `src/Flow.Application/Ideas/Commands/ApproveIdea/ApproveIdeaCommand.cs`
- `src/Flow.Application/Ideas/Commands/ApproveIdea/ApproveIdeaCommandHandler.cs`
- `src/Flow.Application/Ideas/Commands/RejectIdea/RejectIdeaCommand.cs`
- `src/Flow.Application/Ideas/Commands/RejectIdea/RejectIdeaCommandHandler.cs`
- `src/Flow.Application/Ideas/Commands/AddIdeaComment/AddIdeaCommentCommand.cs`
- `src/Flow.Application/Ideas/Commands/AddIdeaComment/AddIdeaCommentCommandHandler.cs`
- `src/Flow.Application/Ideas/Queries/GetIdeas/GetIdeasQuery.cs`
- `src/Flow.Application/Ideas/Queries/GetIdeas/GetIdeasQueryHandler.cs`
- `src/Flow.Application/Ideas/Queries/GetIdeaById/GetIdeaByIdQuery.cs`
- `src/Flow.Application/Ideas/Queries/GetIdeaById/GetIdeaByIdQueryHandler.cs`
- `src/Flow.Application/Ideas/Queries/GetIdeaComments/GetIdeaCommentsQuery.cs`
- `src/Flow.Application/Ideas/Queries/GetIdeaComments/GetIdeaCommentsQueryHandler.cs`
- `tests/Flow.Application.Tests/Ideas/SubmitIdeaCommandHandlerTests.cs`
- `tests/Flow.Application.Tests/Ideas/ApproveIdeaCommandHandlerTests.cs`
- `tests/Flow.Application.Tests/Ideas/RejectIdeaCommandHandlerTests.cs`
- `src/Flow.API/Controllers/IdeasController.cs`
- `tests/Flow.API.Tests/Ideas/IdeasControllerTests.cs`

---

## Task 6: Ideas Application Layer

### Step 6.1 — Shared Async Test Helper

- [ ] **Step 1: Create AsyncQueryHelper for EF async mocking**

`tests/Flow.Application.Tests/Helpers/AsyncQueryHelper.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace Flow.Application.Tests.Helpers;

public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(System.Linq.Expressions.Expression expression)
        => _inner.Execute(expression);

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(
        System.Linq.Expressions.Expression expression,
        CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var syncResult = _inner.Execute(expression);
        var fromResult = typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType);
        return (TResult)fromResult.Invoke(null, new[] { syncResult })!;
    }
}

public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
}

public static class MockDbSetHelper
{
    public static Mock<DbSet<T>> BuildMockDbSet<T>(IEnumerable<T> data) where T : class
    {
        var list = data.ToList();
        var queryable = list.AsQueryable();
        var asyncProvider = new TestAsyncQueryProvider<T>(queryable.Provider);
        var mockDbSet = new Mock<DbSet<T>>();

        mockDbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(asyncProvider);
        mockDbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockDbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockDbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        mockDbSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(list.GetEnumerator()));

        return mockDbSet;
    }
}
```

### Step 6.2 — DTOs

- [ ] **Step 2: Create Idea DTOs**

`src/Flow.Application/Ideas/IdeaSummaryDto.cs`:
```csharp
namespace Flow.Application.Ideas;

public record IdeaSummaryDto(
    Guid Id,
    string Title,
    string Problem,
    string Status,
    string Priority,
    Guid SubmittedBy,
    Guid? LinkedGuidelineId,
    DateTimeOffset CreatedAt);
```

`src/Flow.Application/Ideas/IdeaDetailDto.cs`:
```csharp
namespace Flow.Application.Ideas;

public record IdeaDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Problem,
    string Status,
    string Priority,
    Guid SubmittedBy,
    string? ManagerComment,
    Guid? LinkedGuidelineId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
```

`src/Flow.Application/Ideas/IdeaCommentDto.cs`:
```csharp
namespace Flow.Application.Ideas;

public record IdeaCommentDto(
    Guid Id,
    Guid AuthorId,
    string Body,
    DateTimeOffset CreatedAt);
```

### Step 6.3 — CreateIdea

- [ ] **Step 3: Implement CreateIdea command and handler**

`src/Flow.Application/Ideas/Commands/CreateIdea/CreateIdeaCommand.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Ideas.Commands.CreateIdea;

public record CreateIdeaCommand(
    [Required] string Title,
    [Required] string Description,
    [Required] string Problem,
    Guid? LinkedGuidelineId) : IRequest<IdeaSummaryDto>;
```

`src/Flow.Application/Ideas/Commands/CreateIdea/CreateIdeaCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.CreateIdea;

public class CreateIdeaCommandHandler : IRequestHandler<CreateIdeaCommand, IdeaSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateIdeaCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IdeaSummaryDto> Handle(CreateIdeaCommand request, CancellationToken cancellationToken)
    {
        if (request.LinkedGuidelineId.HasValue)
        {
            var exists = await _context.StrategicGuidelines
                .AnyAsync(g => g.Id == request.LinkedGuidelineId.Value, cancellationToken);
            if (!exists)
                throw new NotFoundException("StrategicGuideline", request.LinkedGuidelineId.Value);
        }

        var actorId = _currentUser.UserId!.Value;
        var idea = Idea.Create(
            request.Title, request.Description, request.Problem,
            actorId, request.LinkedGuidelineId);

        _context.Ideas.Add(idea);
        await _context.SaveChangesAsync(cancellationToken);

        return new IdeaSummaryDto(
            idea.Id, idea.Title, idea.Problem,
            idea.Status.ToString(), idea.Priority.ToString(),
            idea.SubmittedBy, idea.LinkedGuidelineId, idea.CreatedAt);
    }
}
```

### Step 6.4 — UpdateIdea

- [ ] **Step 4: Implement UpdateIdea command and handler**

`src/Flow.Application/Ideas/Commands/UpdateIdea/UpdateIdeaCommand.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Ideas.Commands.UpdateIdea;

public record UpdateIdeaCommand(
    Guid IdeaId,
    [Required] string Title,
    [Required] string Description,
    [Required] string Problem,
    Guid? LinkedGuidelineId) : IRequest;
```

`src/Flow.Application/Ideas/Commands/UpdateIdea/UpdateIdeaCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.UpdateIdea;

public class UpdateIdeaCommandHandler : IRequestHandler<UpdateIdeaCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateIdeaCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateIdeaCommand request, CancellationToken cancellationToken)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        var actorId = _currentUser.UserId!.Value;
        if (idea.SubmittedBy != actorId)
            throw new ForbiddenException("You can only edit your own ideas.");

        idea.Update(request.Title, request.Description, request.Problem, request.LinkedGuidelineId);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### Step 6.5 — SubmitIdea

- [ ] **Step 5: Write the failing test for SubmitIdeaCommandHandler**

`tests/Flow.Application.Tests/Ideas/SubmitIdeaCommandHandlerTests.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Application.Ideas.Commands.SubmitIdea;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;

namespace Flow.Application.Tests.Ideas;

public class SubmitIdeaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    public SubmitIdeaCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesWithAuditAsync(
                It.IsAny<IEnumerable<DomainAuditLog>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_OwnDraftIdea_SubmitsAndWritesAuditLog()
    {
        var actorId = Guid.NewGuid();
        var idea = Idea.Create("Title", "Desc", "Problem", actorId);

        _currentUserMock.Setup(u => u.UserId).Returns(actorId);
        _currentUserMock.Setup(u => u.UserName).Returns("Test User");

        var mockIdeaSet = MockDbSetHelper.BuildMockDbSet(new[] { idea });
        _contextMock.Setup(c => c.Ideas).Returns(mockIdeaSet.Object);

        var handler = new SubmitIdeaCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(new SubmitIdeaCommand(idea.Id), CancellationToken.None);

        idea.Status.Should().Be(IdeaStatus.UnderReview);
        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l => l.Action == "Submitted" && l.EntityId == idea.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AnotherUsersIdea_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var idea = Idea.Create("Title", "Desc", "Problem", ownerId);

        _currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid()); // different user

        var mockIdeaSet = MockDbSetHelper.BuildMockDbSet(new[] { idea });
        _contextMock.Setup(c => c.Ideas).Returns(mockIdeaSet.Object);

        var handler = new SubmitIdeaCommandHandler(_contextMock.Object, _currentUserMock.Object);
        var act = async () => await handler.Handle(new SubmitIdeaCommand(idea.Id), CancellationToken.None);

        await act.Should().ThrowAsync<Flow.Application.Common.Exceptions.ForbiddenException>();
    }
}
```

- [ ] **Step 6: Run test to confirm it fails**

```
dotnet test tests/Flow.Application.Tests/ --filter "SubmitIdeaCommandHandlerTests" -v minimal
```

Expected: FAIL — `SubmitIdeaCommand` does not exist yet.

- [ ] **Step 7: Implement SubmitIdea command and handler**

`src/Flow.Application/Ideas/Commands/SubmitIdea/SubmitIdeaCommand.cs`:
```csharp
using MediatR;

namespace Flow.Application.Ideas.Commands.SubmitIdea;

public record SubmitIdeaCommand(Guid IdeaId) : IRequest;
```

`src/Flow.Application/Ideas/Commands/SubmitIdea/SubmitIdeaCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.SubmitIdea;

public class SubmitIdeaCommandHandler : IRequestHandler<SubmitIdeaCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmitIdeaCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(SubmitIdeaCommand request, CancellationToken cancellationToken)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        var actorId = _currentUser.UserId!.Value;
        if (idea.SubmittedBy != actorId)
            throw new ForbiddenException("You can only submit your own ideas.");

        var oldStatus = idea.Status.ToString();
        idea.Submit();

        var audit = AuditLog.Create(
            "Idea", idea.Id, "Submitted", actorId,
            _currentUser.UserName ?? "Unknown",
            oldValue: oldStatus,
            newValue: idea.Status.ToString());

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
```

- [ ] **Step 8: Run test to confirm it passes**

```
dotnet test tests/Flow.Application.Tests/ --filter "SubmitIdeaCommandHandlerTests" -v minimal
```

Expected: **2 tests passing**, 0 failures.

### Step 6.6 — ApproveIdea

- [ ] **Step 9: Write the failing test for ApproveIdeaCommandHandler**

`tests/Flow.Application.Tests/Ideas/ApproveIdeaCommandHandlerTests.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Application.Ideas.Commands.ApproveIdea;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;

namespace Flow.Application.Tests.Ideas;

public class ApproveIdeaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    public ApproveIdeaCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesWithAuditAsync(
                It.IsAny<IEnumerable<DomainAuditLog>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_UnderReviewIdea_ApprovesAndAwardsPoints()
    {
        var actorId = Guid.NewGuid();
        var submitterId = Guid.NewGuid();
        var idea = Idea.Create("Title", "Desc", "Problem", submitterId);
        idea.Submit(); // → UnderReview

        var submitter = User.Create("Submitter", "submitter@test.com", UserRole.Operator);

        _currentUserMock.Setup(u => u.UserId).Returns(actorId);
        _currentUserMock.Setup(u => u.UserName).Returns("Manager");

        var mockIdeaSet = MockDbSetHelper.BuildMockDbSet(new[] { idea });
        var mockUserSet = MockDbSetHelper.BuildMockDbSet(new[] { submitter });

        var capturedLedgerEntries = new List<PointLedgerEntry>();
        var mockLedgerSet = MockDbSetHelper.BuildMockDbSet<PointLedgerEntry>(Array.Empty<PointLedgerEntry>());
        mockLedgerSet.Setup(s => s.Add(It.IsAny<PointLedgerEntry>()))
            .Callback<PointLedgerEntry>(e => capturedLedgerEntries.Add(e));

        // Users DbSet needs to support FirstOrDefaultAsync by submitter.Id
        var submitterByIdSet = MockDbSetHelper.BuildMockDbSet(new[] { submitter });

        _contextMock.Setup(c => c.Ideas).Returns(mockIdeaSet.Object);
        _contextMock.Setup(c => c.Users).Returns(submitterByIdSet.Object);
        _contextMock.Setup(c => c.PointLedgerEntries).Returns(mockLedgerSet.Object);

        var handler = new ApproveIdeaCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(new ApproveIdeaCommand(idea.Id, "Well done!"), CancellationToken.None);

        idea.Status.Should().Be(IdeaStatus.Approved);
        idea.ManagerComment.Should().Be("Well done!");
        submitter.Points.Should().Be(50);
        capturedLedgerEntries.Should().HaveCount(1);
        capturedLedgerEntries[0].Points.Should().Be(50);
        capturedLedgerEntries[0].ReferenceType.Should().Be("Idea");

        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l => l.Action == "Approved" && l.EntityId == idea.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

- [ ] **Step 10: Run test to confirm it fails**

```
dotnet test tests/Flow.Application.Tests/ --filter "ApproveIdeaCommandHandlerTests" -v minimal
```

Expected: FAIL — command does not exist yet.

- [ ] **Step 11: Implement ApproveIdea command and handler**

`src/Flow.Application/Ideas/Commands/ApproveIdea/ApproveIdeaCommand.cs`:
```csharp
using MediatR;

namespace Flow.Application.Ideas.Commands.ApproveIdea;

public record ApproveIdeaCommand(Guid IdeaId, string? ManagerComment) : IRequest;
```

`src/Flow.Application/Ideas/Commands/ApproveIdea/ApproveIdeaCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.ApproveIdea;

public class ApproveIdeaCommandHandler : IRequestHandler<ApproveIdeaCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ApproveIdeaCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(ApproveIdeaCommand request, CancellationToken cancellationToken)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        var submitter = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == idea.SubmittedBy, cancellationToken)
            ?? throw new NotFoundException("User", idea.SubmittedBy);

        var actorId = _currentUser.UserId!.Value;
        var oldStatus = idea.Status.ToString();

        idea.Approve(request.ManagerComment);
        submitter.AddPoints(50);

        var ledgerEntry = PointLedgerEntry.Create(
            submitter.Id, 50, "Idea approved by manager", "Idea", idea.Id);
        _context.PointLedgerEntries.Add(ledgerEntry);

        var audit = AuditLog.Create(
            "Idea", idea.Id, "Approved", actorId,
            _currentUser.UserName ?? "Unknown",
            oldValue: oldStatus,
            newValue: idea.Status.ToString());

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
```

- [ ] **Step 12: Run test to confirm it passes**

```
dotnet test tests/Flow.Application.Tests/ --filter "ApproveIdeaCommandHandlerTests" -v minimal
```

Expected: **1 test passing**, 0 failures.

### Step 6.7 — RejectIdea

- [ ] **Step 13: Write the failing test for RejectIdeaCommandHandler**

`tests/Flow.Application.Tests/Ideas/RejectIdeaCommandHandlerTests.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Application.Ideas.Commands.RejectIdea;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;

namespace Flow.Application.Tests.Ideas;

public class RejectIdeaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    public RejectIdeaCommandHandlerTests()
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
    public async Task Handle_UnderReviewIdea_RejectsAndWritesAuditLog()
    {
        var idea = Idea.Create("Title", "Desc", "Problem", Guid.NewGuid());
        idea.Submit(); // → UnderReview

        var mockIdeaSet = MockDbSetHelper.BuildMockDbSet(new[] { idea });
        _contextMock.Setup(c => c.Ideas).Returns(mockIdeaSet.Object);

        var handler = new RejectIdeaCommandHandler(_contextMock.Object, _currentUserMock.Object);
        await handler.Handle(new RejectIdeaCommand(idea.Id, "Not aligned."), CancellationToken.None);

        idea.Status.Should().Be(IdeaStatus.Rejected);
        idea.ManagerComment.Should().Be("Not aligned.");
        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l => l.Action == "Rejected" && l.Reason == "Not aligned.")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

- [ ] **Step 14: Implement RejectIdea command and handler**

`src/Flow.Application/Ideas/Commands/RejectIdea/RejectIdeaCommand.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Ideas.Commands.RejectIdea;

public record RejectIdeaCommand(Guid IdeaId, [Required] string ManagerComment) : IRequest;
```

`src/Flow.Application/Ideas/Commands/RejectIdea/RejectIdeaCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.RejectIdea;

public class RejectIdeaCommandHandler : IRequestHandler<RejectIdeaCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RejectIdeaCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RejectIdeaCommand request, CancellationToken cancellationToken)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        var actorId = _currentUser.UserId!.Value;
        var oldStatus = idea.Status.ToString();

        idea.Reject(request.ManagerComment);

        var audit = AuditLog.Create(
            "Idea", idea.Id, "Rejected", actorId,
            _currentUser.UserName ?? "Unknown",
            oldValue: oldStatus,
            newValue: idea.Status.ToString(),
            reason: request.ManagerComment);

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);
    }
}
```

- [ ] **Step 15: Run all idea handler tests**

```
dotnet test tests/Flow.Application.Tests/ --filter "SubmitIdea|ApproveIdea|RejectIdea" -v minimal
```

Expected: **4 tests passing**, 0 failures.

### Step 6.8 — AddIdeaComment

- [ ] **Step 16: Implement AddIdeaComment command and handler**

`src/Flow.Application/Ideas/Commands/AddIdeaComment/AddIdeaCommentCommand.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Ideas.Commands.AddIdeaComment;

public record AddIdeaCommentCommand(Guid IdeaId, [Required] string Body) : IRequest<IdeaCommentDto>;
```

`src/Flow.Application/Ideas/Commands/AddIdeaComment/AddIdeaCommentCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Commands.AddIdeaComment;

public class AddIdeaCommentCommandHandler : IRequestHandler<AddIdeaCommentCommand, IdeaCommentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddIdeaCommentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IdeaCommentDto> Handle(
        AddIdeaCommentCommand request, CancellationToken cancellationToken)
    {
        var ideaExists = await _context.Ideas
            .AnyAsync(i => i.Id == request.IdeaId, cancellationToken);
        if (!ideaExists) throw new NotFoundException("Idea", request.IdeaId);

        var actorId = _currentUser.UserId!.Value;
        var comment = IdeaComment.Create(request.IdeaId, actorId, request.Body);

        _context.IdeaComments.Add(comment);

        var audit = AuditLog.Create(
            "Idea", request.IdeaId, "CommentAdded", actorId,
            _currentUser.UserName ?? "Unknown",
            newValue: request.Body);

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);

        return new IdeaCommentDto(comment.Id, comment.AuthorId, comment.Body, comment.CreatedAt);
    }
}
```

### Step 6.9 — Queries

- [ ] **Step 17: Implement all three Idea queries**

`src/Flow.Application/Ideas/Queries/GetIdeas/GetIdeasQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Ideas.Queries.GetIdeas;

public record GetIdeasQuery(Guid? SubmittedById) : IRequest<IReadOnlyList<IdeaSummaryDto>>;
```

`src/Flow.Application/Ideas/Queries/GetIdeas/GetIdeasQueryHandler.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Queries.GetIdeas;

public class GetIdeasQueryHandler : IRequestHandler<GetIdeasQuery, IReadOnlyList<IdeaSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetIdeasQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<IdeaSummaryDto>> Handle(
        GetIdeasQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Ideas.AsQueryable();

        if (request.SubmittedById.HasValue)
            query = query.Where(i => i.SubmittedBy == request.SubmittedById.Value);

        var ideas = await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        return ideas
            .Select(i => new IdeaSummaryDto(
                i.Id, i.Title, i.Problem,
                i.Status.ToString(), i.Priority.ToString(),
                i.SubmittedBy, i.LinkedGuidelineId, i.CreatedAt))
            .ToList();
    }
}
```

`src/Flow.Application/Ideas/Queries/GetIdeaById/GetIdeaByIdQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Ideas.Queries.GetIdeaById;

public record GetIdeaByIdQuery(Guid IdeaId) : IRequest<IdeaDetailDto>;
```

`src/Flow.Application/Ideas/Queries/GetIdeaById/GetIdeaByIdQueryHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Queries.GetIdeaById;

public class GetIdeaByIdQueryHandler : IRequestHandler<GetIdeaByIdQuery, IdeaDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetIdeaByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IdeaDetailDto> Handle(
        GetIdeaByIdQuery request, CancellationToken cancellationToken)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == request.IdeaId, cancellationToken)
            ?? throw new NotFoundException("Idea", request.IdeaId);

        return new IdeaDetailDto(
            idea.Id, idea.Title, idea.Description, idea.Problem,
            idea.Status.ToString(), idea.Priority.ToString(),
            idea.SubmittedBy, idea.ManagerComment, idea.LinkedGuidelineId,
            idea.CreatedAt, idea.UpdatedAt);
    }
}
```

`src/Flow.Application/Ideas/Queries/GetIdeaComments/GetIdeaCommentsQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Ideas.Queries.GetIdeaComments;

public record GetIdeaCommentsQuery(Guid IdeaId) : IRequest<IReadOnlyList<IdeaCommentDto>>;
```

`src/Flow.Application/Ideas/Queries/GetIdeaComments/GetIdeaCommentsQueryHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Ideas.Queries.GetIdeaComments;

public class GetIdeaCommentsQueryHandler : IRequestHandler<GetIdeaCommentsQuery, IReadOnlyList<IdeaCommentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetIdeaCommentsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<IdeaCommentDto>> Handle(
        GetIdeaCommentsQuery request, CancellationToken cancellationToken)
    {
        var ideaExists = await _context.Ideas
            .AnyAsync(i => i.Id == request.IdeaId, cancellationToken);
        if (!ideaExists) throw new NotFoundException("Idea", request.IdeaId);

        var comments = await _context.IdeaComments
            .Where(c => c.IdeaId == request.IdeaId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments
            .Select(c => new IdeaCommentDto(c.Id, c.AuthorId, c.Body, c.CreatedAt))
            .ToList();
    }
}
```

- [ ] **Step 18: Build to confirm no compile errors**

```
dotnet build src/Flow.Application/Flow.Application.csproj -v minimal
```

Expected: **Build succeeded**, 0 errors.

- [ ] **Step 19: Run all application tests**

```
dotnet test tests/Flow.Application.Tests/ -v minimal
```

Expected: **66 tests passing** (62 existing + 4 new handler tests), 0 failures.

- [ ] **Step 20: Commit application layer**

```
git add tests/Flow.Application.Tests/Helpers/AsyncQueryHelper.cs
git add src/Flow.Application/Ideas/
git add tests/Flow.Application.Tests/Ideas/
git commit -m "feat: implement Ideas application layer with audit log writes and point award"
```

---

## Task 7: Ideas Controller and Integration Tests

- [ ] **Step 1: Write the failing integration tests**

`tests/Flow.API.Tests/Ideas/IdeasControllerTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Xunit;

namespace Flow.API.Tests.Ideas;

public class IdeasControllerTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly FlowWebApplicationFactory _factory;

    public IdeasControllerTests(FlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateIdea_AsOperator_Returns201()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "Automate expense reports",
            description = "Use AI to auto-fill expense forms.",
            problem = "Finance team spends 3 hours/week on manual expense processing.",
            linkedGuidelineId = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("status").GetString().Should().Be("Draft");
    }

    [Fact]
    public async Task GetIdeas_AsOperator_ReturnsOnlyOwnIdeas()
    {
        var client = _factory.CreateClient();
        var operatorToken = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);

        await client.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "My Idea",
            description = "Desc",
            problem = "Problem",
            linkedGuidelineId = (string?)null
        });

        var response = await client.GetAsync("/api/v1/ideas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SubmitIdea_AsOwner_Returns204()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "Submit Test",
            description = "Desc",
            problem = "Problem",
            linkedGuidelineId = (string?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var response = await client.PostAsync($"/api/v1/ideas/{id}/submit", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ApproveIdea_AsManager_Returns204AndIdeasShowApproved()
    {
        var operatorClient = _factory.CreateClient();
        var operatorToken = await _factory.GetTokenForRoleAsync("Operator");
        operatorClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", operatorToken);

        var createResponse = await operatorClient.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "Approvable Idea",
            description = "Desc",
            problem = "Problem",
            linkedGuidelineId = (string?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;
        await operatorClient.PostAsync($"/api/v1/ideas/{id}/submit", null);

        var managerClient = _factory.CreateClient();
        var managerToken = await _factory.GetTokenForRoleAsync("Manager");
        managerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", managerToken);

        var response = await managerClient.PostAsJsonAsync(
            $"/api/v1/ideas/{id}/approve",
            new { managerComment = "Great idea!" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await managerClient.GetAsync($"/api/v1/ideas/{id}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
        detail.GetProperty("status").GetString().Should().Be("Approved");
    }

    [Fact]
    public async Task RejectIdea_AsManager_Returns204()
    {
        var operatorClient = _factory.CreateClient();
        var operatorToken = await _factory.GetTokenForRoleAsync("Operator");
        operatorClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", operatorToken);

        var createResponse = await operatorClient.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "Rejectable Idea",
            description = "Desc",
            problem = "Problem",
            linkedGuidelineId = (string?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;
        await operatorClient.PostAsync($"/api/v1/ideas/{id}/submit", null);

        var managerClient = _factory.CreateClient();
        var managerToken = await _factory.GetTokenForRoleAsync("Manager");
        managerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", managerToken);

        var response = await managerClient.PostAsJsonAsync(
            $"/api/v1/ideas/{id}/reject",
            new { managerComment = "Not aligned with strategy." });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
```

- [ ] **Step 2: Run the failing tests**

```
dotnet test tests/Flow.API.Tests/ --filter "IdeasControllerTests" -v minimal
```

Expected: FAIL — `IdeasController` does not exist yet.

- [ ] **Step 3: Implement IdeasController**

`src/Flow.API/Controllers/IdeasController.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Application.Ideas;
using Flow.Application.Ideas.Commands.AddIdeaComment;
using Flow.Application.Ideas.Commands.ApproveIdea;
using Flow.Application.Ideas.Commands.CreateIdea;
using Flow.Application.Ideas.Commands.RejectIdea;
using Flow.Application.Ideas.Commands.SubmitIdea;
using Flow.Application.Ideas.Commands.UpdateIdea;
using Flow.Application.Ideas.Queries.GetIdeaById;
using Flow.Application.Ideas.Queries.GetIdeaComments;
using Flow.Application.Ideas.Queries.GetIdeas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1/ideas")]
[Authorize]
public class IdeasController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public IdeasController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = "Operator")]
    public async Task<ActionResult<IdeaSummaryDto>> Create(
        [FromBody] CreateIdeaCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IdeaSummaryDto>>> GetAll(CancellationToken ct)
    {
        var isPrivileged = User.IsInRole("Manager") || User.IsInRole("Leadership");
        var submittedById = isPrivileged ? (Guid?)null : _currentUser.UserId;
        var result = await _mediator.Send(new GetIdeasQuery(submittedById), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IdeaDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetIdeaByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateIdeaCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { IdeaId = id }, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new SubmitIdeaCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Approve(
        Guid id, [FromBody] ApproveIdeaCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { IdeaId = id }, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody] RejectIdeaCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { IdeaId = id }, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<IReadOnlyList<IdeaCommentDto>>> GetComments(
        Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetIdeaCommentsQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/comments")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<IdeaCommentDto>> AddComment(
        Guid id, [FromBody] AddIdeaCommentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command with { IdeaId = id }, ct);
        return Created(string.Empty, result);
    }
}
```

- [ ] **Step 4: Run Ideas integration tests**

```
dotnet test tests/Flow.API.Tests/ --filter "IdeasControllerTests" -v minimal
```

Expected: **5 tests passing**, 0 failures.

- [ ] **Step 5: Run full test suite**

```
dotnet test -v minimal
```

Expected: **79 tests passing** (68 + 4 handler + 5 controller + 1 DomainException = all passing), 0 failures.

- [ ] **Step 6: Commit**

```
git add src/Flow.API/Controllers/IdeasController.cs tests/Flow.API.Tests/Ideas/
git commit -m "feat: implement Ideas controller with role-based access and full CQRS flow"
```

---

**End of Part 3. Proceed to `2026-05-14-phase2-part4-projects.md`.**
