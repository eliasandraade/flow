# Phase 3 — Results, Dashboard & Gamification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the ROI Results engine, dashboard summary with bottleneck visibility, and gamification points endpoints — all built on top of existing Phase 2 data with simple, transparent logic.

**Architecture:** Three independent subsystems layered on top of Phase 2 infrastructure. Results is an upsert entity per project with independent estimated/actual groups. Dashboard is a single aggregation query over existing tables — no caching, no views, no materialized state. Gamification exposes the PointLedgerEntry data already written by the ApproveIdea handler. Every state-changing operation (ResultRecorded) produces an AuditLog entry via `SaveChangesWithAuditAsync`.

**Tech Stack:** .NET 8, MediatR 12, EF Core 8, xUnit, FluentAssertions, Moq, WebApplicationFactory.

**Baseline:** 97 tests passing (69 Application + 28 API) at `phase2-complete` tag.

---

## File Map

**Create:**
- `src/Flow.Domain/Entities/Result.cs`
- `src/Flow.Infrastructure/Persistence/Configurations/ResultConfiguration.cs`
- `src/Flow.Application/Results/ResultDto.cs`
- `src/Flow.Application/Results/Commands/RecordResult/RecordResultCommand.cs`
- `src/Flow.Application/Results/Commands/RecordResult/RecordResultCommandHandler.cs`
- `src/Flow.Application/Results/Queries/GetResult/GetResultQuery.cs`
- `src/Flow.Application/Results/Queries/GetResult/GetResultQueryHandler.cs`
- `src/Flow.Application/Dashboard/DashboardSummaryDto.cs`
- `src/Flow.Application/Dashboard/BlockedProjectDto.cs`
- `src/Flow.Application/Dashboard/Queries/GetDashboardSummary/GetDashboardSummaryQuery.cs`
- `src/Flow.Application/Dashboard/Queries/GetDashboardSummary/GetDashboardSummaryQueryHandler.cs`
- `src/Flow.Application/Gamification/PointsSummaryDto.cs`
- `src/Flow.Application/Gamification/PointsLedgerEntryDto.cs`
- `src/Flow.Application/Gamification/Queries/GetMyPoints/GetMyPointsQuery.cs`
- `src/Flow.Application/Gamification/Queries/GetMyPoints/GetMyPointsQueryHandler.cs`
- `src/Flow.Application/Gamification/Queries/GetMyPointsLedger/GetMyPointsLedgerQuery.cs`
- `src/Flow.Application/Gamification/Queries/GetMyPointsLedger/GetMyPointsLedgerQueryHandler.cs`
- `src/Flow.Application/Gamification/Queries/GetUserPoints/GetUserPointsQuery.cs`
- `src/Flow.Application/Gamification/Queries/GetUserPoints/GetUserPointsQueryHandler.cs`
- `src/Flow.API/Controllers/ResultsController.cs`
- `src/Flow.API/Controllers/DashboardController.cs`
- `src/Flow.API/Controllers/UsersController.cs`
- `tests/Flow.Application.Tests/Results/ResultEntityTests.cs`
- `tests/Flow.Application.Tests/Results/RecordResultCommandHandlerTests.cs`
- `tests/Flow.API.Tests/Results/ResultsControllerTests.cs`
- `tests/Flow.API.Tests/Dashboard/DashboardControllerTests.cs`
- `tests/Flow.API.Tests/Users/UsersControllerTests.cs`

**Modify:**
- `src/Flow.Application/Common/Interfaces/IApplicationDbContext.cs` — add `DbSet<Result> Results`
- `src/Flow.Infrastructure/Persistence/ApplicationDbContext.cs` — add `Set<Result>()`

---

## Task 10: Result Domain Entity + EF Configuration + Migration

**Files:**
- Create: `src/Flow.Domain/Entities/Result.cs`
- Create: `src/Flow.Infrastructure/Persistence/Configurations/ResultConfiguration.cs`
- Modify: `src/Flow.Application/Common/Interfaces/IApplicationDbContext.cs`
- Modify: `src/Flow.Infrastructure/Persistence/ApplicationDbContext.cs`
- Test: `tests/Flow.Application.Tests/Results/ResultEntityTests.cs`

### Step 10.1 — Write failing entity tests

- [ ] **Step 1: Create ResultEntityTests.cs**

`tests/Flow.Application.Tests/Results/ResultEntityTests.cs`:
```csharp
using Flow.Domain.Entities;
using Flow.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Flow.Application.Tests.Results;

public class ResultEntityTests
{
    private static Result MakeResult() =>
        Result.Create(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Create_ValidInput_SetsProjectIdAndRecordedBy()
    {
        var projectId = Guid.NewGuid();
        var recordedBy = Guid.NewGuid();

        var result = Result.Create(projectId, recordedBy);

        result.ProjectId.Should().Be(projectId);
        result.RecordedBy.Should().Be(recordedBy);
        result.EstimatedROI.Should().BeNull();
        result.ActualROI.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyProjectId_ThrowsDomainException()
    {
        var act = () => Result.Create(Guid.Empty, Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*ProjectId*");
    }

    [Fact]
    public void Create_EmptyRecordedBy_ThrowsDomainException()
    {
        var act = () => Result.Create(Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<DomainException>().WithMessage("*RecordedBy*");
    }

    [Fact]
    public void SetEstimated_ValidValues_ComputesROI()
    {
        var result = MakeResult();

        result.SetEstimated(revenue: 100_000m, savings: 20_000m, cost: 40_000m);

        // ROI = (100000 + 20000 - 40000) / 40000 * 100 = 200
        result.EstimatedROI.Should().Be(200m);
        result.EstimatedRevenue.Should().Be(100_000m);
        result.EstimatedRecordedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetActual_ValidValues_ComputesROI()
    {
        var result = MakeResult();

        result.SetActual(revenue: 80_000m, savings: 10_000m, cost: 30_000m);

        // ROI = (80000 + 10000 - 30000) / 30000 * 100 = 200
        result.ActualROI.Should().Be(200m);
        result.ActualRevenue.Should().Be(80_000m);
        result.ActualRecordedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetEstimated_ZeroCost_ReturnsNullROI()
    {
        var result = MakeResult();

        result.SetEstimated(revenue: 50_000m, savings: 10_000m, cost: 0m);

        result.EstimatedROI.Should().BeNull();
    }

    [Fact]
    public void SetActual_NullCost_ReturnsNullROI()
    {
        var result = MakeResult();

        result.SetActual(revenue: 50_000m, savings: 10_000m, cost: null);

        result.ActualROI.Should().BeNull();
    }

    [Fact]
    public void SetEstimated_DoesNotClearActual()
    {
        var result = MakeResult();
        result.SetActual(revenue: 80_000m, savings: 0m, cost: 40_000m);

        result.SetEstimated(revenue: 100_000m, savings: 0m, cost: 50_000m);

        result.ActualRevenue.Should().Be(80_000m);
        result.ActualROI.Should().NotBeNull();
    }

    [Fact]
    public void SetActual_DoesNotClearEstimated()
    {
        var result = MakeResult();
        result.SetEstimated(revenue: 100_000m, savings: 0m, cost: 50_000m);

        result.SetActual(revenue: 80_000m, savings: 0m, cost: 40_000m);

        result.EstimatedRevenue.Should().Be(100_000m);
        result.EstimatedROI.Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```
dotnet test tests/Flow.Application.Tests/ --filter "ResultEntityTests" -v minimal
```

Expected: FAIL — `Result` type does not exist.

### Step 10.2 — Implement the Result entity

- [ ] **Step 3: Create Result.cs**

`src/Flow.Domain/Entities/Result.cs`:
```csharp
using Flow.Domain.Common;
using Flow.Domain.Exceptions;

namespace Flow.Domain.Entities;

public class Result : BaseEntity
{
    public Guid ProjectId { get; private set; }

    // Estimated phase
    public decimal? EstimatedRevenue { get; private set; }
    public decimal? EstimatedSavings { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public decimal? EstimatedROI { get; private set; }
    public DateTimeOffset? EstimatedRecordedAt { get; private set; }

    // Actual phase
    public decimal? ActualRevenue { get; private set; }
    public decimal? ActualSavings { get; private set; }
    public decimal? ActualCost { get; private set; }
    public decimal? ActualROI { get; private set; }
    public DateTimeOffset? ActualRecordedAt { get; private set; }

    public int? PaybackPeriodMonths { get; private set; }
    public string? Notes { get; private set; }
    public Guid RecordedBy { get; private set; }

    private Result() { }

    public static Result Create(Guid projectId, Guid recordedBy)
    {
        if (projectId == Guid.Empty)
            throw new DomainException("ProjectId is required.");
        if (recordedBy == Guid.Empty)
            throw new DomainException("RecordedBy is required.");

        return new Result
        {
            ProjectId = projectId,
            RecordedBy = recordedBy
        };
    }

    /// <summary>
    /// Updates the estimated ROI group. Only call if at least one estimated value is present.
    /// Does not modify actual values.
    /// </summary>
    public void SetEstimated(decimal? revenue, decimal? savings, decimal? cost)
    {
        EstimatedRevenue = revenue;
        EstimatedSavings = savings;
        EstimatedCost = cost;
        EstimatedROI = ComputeRoi(revenue, savings, cost);
        EstimatedRecordedAt = DateTimeOffset.UtcNow;
        SetUpdated();
    }

    /// <summary>
    /// Updates the actual ROI group. Only call if at least one actual value is present.
    /// Does not modify estimated values.
    /// </summary>
    public void SetActual(decimal? revenue, decimal? savings, decimal? cost)
    {
        ActualRevenue = revenue;
        ActualSavings = savings;
        ActualCost = cost;
        ActualROI = ComputeRoi(revenue, savings, cost);
        ActualRecordedAt = DateTimeOffset.UtcNow;
        SetUpdated();
    }

    public void SetNotes(int? paybackPeriodMonths, string? notes)
    {
        PaybackPeriodMonths = paybackPeriodMonths;
        Notes = notes;
        SetUpdated();
    }

    // ROI = (Revenue + Savings - Cost) / Cost * 100
    // Returns null if Cost is null or zero (division by zero guard).
    private static decimal? ComputeRoi(decimal? revenue, decimal? savings, decimal? cost)
    {
        if (cost == null || cost == 0m) return null;
        return ((revenue ?? 0m) + (savings ?? 0m) - cost.Value) / cost.Value * 100m;
    }
}
```

- [ ] **Step 4: Run entity tests to confirm they pass**

```
dotnet test tests/Flow.Application.Tests/ --filter "ResultEntityTests" -v minimal
```

Expected: **8 tests passing**, 0 failures.

### Step 10.3 — EF Configuration

- [ ] **Step 5: Create ResultConfiguration.cs**

`src/Flow.Infrastructure/Persistence/Configurations/ResultConfiguration.cs`:
```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class ResultConfiguration : IEntityTypeConfiguration<Result>
{
    public void Configure(EntityTypeBuilder<Result> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ProjectId).IsRequired();
        builder.HasIndex(r => r.ProjectId).IsUnique(); // one result per project

        builder.HasOne<Project>()
            .WithOne()
            .HasForeignKey<Result>(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.EstimatedRevenue).HasColumnType("decimal(18,2)");
        builder.Property(r => r.EstimatedSavings).HasColumnType("decimal(18,2)");
        builder.Property(r => r.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(r => r.EstimatedROI).HasColumnType("decimal(18,4)");

        builder.Property(r => r.ActualRevenue).HasColumnType("decimal(18,2)");
        builder.Property(r => r.ActualSavings).HasColumnType("decimal(18,2)");
        builder.Property(r => r.ActualCost).HasColumnType("decimal(18,2)");
        builder.Property(r => r.ActualROI).HasColumnType("decimal(18,4)");

        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.RecordedBy).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
    }
}
```

### Step 10.4 — Update IApplicationDbContext and ApplicationDbContext

- [ ] **Step 6: Add DbSet\<Result\> to IApplicationDbContext**

`src/Flow.Application/Common/Interfaces/IApplicationDbContext.cs` — add one line after `PointLedgerEntries`:
```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Idea> Ideas { get; }
    DbSet<IdeaComment> IdeaComments { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectSnapshot> ProjectSnapshots { get; }
    DbSet<StrategicGuideline> StrategicGuidelines { get; }
    DbSet<PointLedgerEntry> PointLedgerEntries { get; }
    DbSet<Result> Results { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesWithAuditAsync(
        IEnumerable<AuditLog> auditEntries,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 7: Add Set\<Result\>() to ApplicationDbContext**

`src/Flow.Infrastructure/Persistence/ApplicationDbContext.cs` — add one line after `PointLedgerEntries`:
```csharp
public DbSet<Result> Results => Set<Result>();
```

The full file becomes:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Flow.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Idea> Ideas => Set<Idea>();
    public DbSet<IdeaComment> IdeaComments => Set<IdeaComment>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectSnapshot> ProjectSnapshots => Set<ProjectSnapshot>();
    public DbSet<StrategicGuideline> StrategicGuidelines => Set<StrategicGuideline>();
    public DbSet<PointLedgerEntry> PointLedgerEntries => Set<PointLedgerEntry>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public async Task<int> SaveChangesWithAuditAsync(
        IEnumerable<AuditLog> auditEntries,
        CancellationToken cancellationToken = default)
    {
        AuditLogs.AddRange(auditEntries);
        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

### Step 10.5 — Migration

- [ ] **Step 8: Create and apply the EF migration**

```
dotnet ef migrations add AddResults --project src/Flow.Infrastructure --startup-project src/Flow.API
```

Expected: Migration file created at `src/Flow.Infrastructure/Persistence/Migrations/<timestamp>_AddResults.cs`.

Open the generated migration file and confirm it contains:
- `CreateTable("Results", ...)` with columns for all financial fields
- `CreateIndex` for `ProjectId` (unique)
- `AddForeignKey` for `ProjectId → Projects`

If the migration file looks correct, apply it:

```
dotnet ef database update --project src/Flow.Infrastructure --startup-project src/Flow.API
```

Expected: `Done.`

### Step 10.6 — Build and commit

- [ ] **Step 9: Build to confirm no compile errors**

```
dotnet build src/ -v minimal
```

Expected: **Build succeeded**, 0 errors.

- [ ] **Step 10: Run all tests**

```
dotnet test -v minimal
```

Expected: **105 tests passing** (97 existing + 8 new entity tests), 0 failures.

- [ ] **Step 11: Commit**

```
git add src/Flow.Domain/Entities/Result.cs
git add src/Flow.Infrastructure/Persistence/Configurations/ResultConfiguration.cs
git add src/Flow.Infrastructure/Persistence/Migrations/
git add src/Flow.Application/Common/Interfaces/IApplicationDbContext.cs
git add src/Flow.Infrastructure/Persistence/ApplicationDbContext.cs
git add tests/Flow.Application.Tests/Results/ResultEntityTests.cs
git commit -m "feat: add Result domain entity with ROI computation and EF migration"
```

---

## Task 11: Results Application Layer

**Files:**
- Create: `src/Flow.Application/Results/ResultDto.cs`
- Create: `src/Flow.Application/Results/Commands/RecordResult/RecordResultCommand.cs`
- Create: `src/Flow.Application/Results/Commands/RecordResult/RecordResultCommandHandler.cs`
- Create: `src/Flow.Application/Results/Queries/GetResult/GetResultQuery.cs`
- Create: `src/Flow.Application/Results/Queries/GetResult/GetResultQueryHandler.cs`
- Test: `tests/Flow.Application.Tests/Results/RecordResultCommandHandlerTests.cs`

### Step 11.1 — DTO

- [ ] **Step 1: Create ResultDto**

`src/Flow.Application/Results/ResultDto.cs`:
```csharp
namespace Flow.Application.Results;

public record ResultDto(
    Guid Id,
    Guid ProjectId,
    decimal? EstimatedRevenue,
    decimal? EstimatedSavings,
    decimal? EstimatedCost,
    decimal? EstimatedROI,
    DateTimeOffset? EstimatedRecordedAt,
    decimal? ActualRevenue,
    decimal? ActualSavings,
    decimal? ActualCost,
    decimal? ActualROI,
    DateTimeOffset? ActualRecordedAt,
    int? PaybackPeriodMonths,
    string? Notes,
    Guid RecordedBy,
    DateTimeOffset RecordedAt,
    DateTimeOffset UpdatedAt);
```

Note: `RecordedAt` maps to the entity's `CreatedAt` (first creation timestamp).

### Step 11.2 — Write failing handler tests

- [ ] **Step 2: Create RecordResultCommandHandlerTests.cs**

`tests/Flow.Application.Tests/Results/RecordResultCommandHandlerTests.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Application.Results.Commands.RecordResult;
using Flow.Application.Tests.Helpers;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using DomainAuditLog = Flow.Domain.Entities.AuditLog;

namespace Flow.Application.Tests.Results;

public class RecordResultCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Guid _actorId = Guid.NewGuid();

    public RecordResultCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(_actorId);
        _currentUserMock.Setup(u => u.UserName).Returns("Manager");
        _contextMock
            .Setup(c => c.SaveChangesWithAuditAsync(
                It.IsAny<IEnumerable<DomainAuditLog>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_NewResult_CreatesResultAndAuditLog()
    {
        // Arrange
        var project = Project.Create("Test Project", "Desc", Guid.NewGuid(), ProjectPriority.Medium);

        var mockProjectSet = MockDbSetHelper.BuildMockDbSet(new[] { project });
        var mockResultSet = MockDbSetHelper.BuildMockDbSet<Result>(Array.Empty<Result>());

        var capturedResults = new List<Result>();
        mockResultSet.Setup(s => s.Add(It.IsAny<Result>()))
            .Callback<Result>(r => capturedResults.Add(r));

        _contextMock.Setup(c => c.Projects).Returns(mockProjectSet.Object);
        _contextMock.Setup(c => c.Results).Returns(mockResultSet.Object);

        var handler = new RecordResultCommandHandler(_contextMock.Object, _currentUserMock.Object);
        var command = new RecordResultCommand(
            ProjectId: project.Id,
            EstimatedRevenue: 100_000m,
            EstimatedSavings: 20_000m,
            EstimatedCost: 40_000m,
            ActualRevenue: null,
            ActualSavings: null,
            ActualCost: null,
            PaybackPeriodMonths: null,
            Notes: null);

        // Act
        var dto = await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedResults.Should().HaveCount(1);
        capturedResults[0].EstimatedROI.Should().Be(200m);
        capturedResults[0].ActualROI.Should().BeNull();

        dto.EstimatedROI.Should().Be(200m);
        dto.ProjectId.Should().Be(project.Id);

        _contextMock.Verify(
            c => c.SaveChangesWithAuditAsync(
                It.Is<IEnumerable<DomainAuditLog>>(logs =>
                    logs.Any(l => l.Action == "ResultRecorded" && l.EntityId == project.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingResult_UpdatesInsteadOfCreating()
    {
        // Arrange
        var project = Project.Create("Test Project", "Desc", Guid.NewGuid(), ProjectPriority.Medium);
        var existingResult = Result.Create(project.Id, _actorId);
        existingResult.SetEstimated(80_000m, 0m, 40_000m);

        var mockProjectSet = MockDbSetHelper.BuildMockDbSet(new[] { project });
        var mockResultSet = MockDbSetHelper.BuildMockDbSet(new[] { existingResult });
        var capturedResults = new List<Result>();
        mockResultSet.Setup(s => s.Add(It.IsAny<Result>()))
            .Callback<Result>(r => capturedResults.Add(r));

        _contextMock.Setup(c => c.Projects).Returns(mockProjectSet.Object);
        _contextMock.Setup(c => c.Results).Returns(mockResultSet.Object);

        var handler = new RecordResultCommandHandler(_contextMock.Object, _currentUserMock.Object);
        var command = new RecordResultCommand(
            ProjectId: project.Id,
            EstimatedRevenue: null,
            EstimatedSavings: null,
            EstimatedCost: null,
            ActualRevenue: 90_000m,
            ActualSavings: 5_000m,
            ActualCost: 35_000m,
            PaybackPeriodMonths: 6,
            Notes: "Exceeded expectations");

        // Act
        var dto = await handler.Handle(command, CancellationToken.None);

        // Assert — existing record updated, not duplicated
        capturedResults.Should().BeEmpty(); // Add was NOT called
        existingResult.ActualROI.Should().NotBeNull(); // actual was set
        existingResult.EstimatedROI.Should().NotBeNull(); // estimated unchanged
        dto.Notes.Should().Be("Exceeded expectations");
    }
}
```

- [ ] **Step 3: Run tests to confirm they fail**

```
dotnet test tests/Flow.Application.Tests/ --filter "RecordResultCommandHandlerTests" -v minimal
```

Expected: FAIL — handlers do not exist yet.

### Step 11.3 — Implement command and query

- [ ] **Step 4: Create RecordResultCommand.cs**

`src/Flow.Application/Results/Commands/RecordResult/RecordResultCommand.cs`:
```csharp
using MediatR;

namespace Flow.Application.Results.Commands.RecordResult;

public record RecordResultCommand(
    Guid ProjectId,
    decimal? EstimatedRevenue,
    decimal? EstimatedSavings,
    decimal? EstimatedCost,
    decimal? ActualRevenue,
    decimal? ActualSavings,
    decimal? ActualCost,
    int? PaybackPeriodMonths,
    string? Notes) : IRequest<ResultDto>;
```

- [ ] **Step 5: Create RecordResultCommandHandler.cs**

`src/Flow.Application/Results/Commands/RecordResult/RecordResultCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Results.Commands.RecordResult;

public class RecordResultCommandHandler : IRequestHandler<RecordResultCommand, ResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RecordResultCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ResultDto> Handle(RecordResultCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");
        var actorName = _currentUser.UserName ?? "Unknown";

        _ = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        // Find existing or create new — one result per project
        var result = await _context.Results
            .FirstOrDefaultAsync(r => r.ProjectId == request.ProjectId, cancellationToken);

        if (result is null)
        {
            result = Result.Create(request.ProjectId, actorId);
            _context.Results.Add(result);
        }

        // Update estimated group only if at least one estimated value is present
        bool hasEstimated = request.EstimatedRevenue.HasValue
            || request.EstimatedSavings.HasValue
            || request.EstimatedCost.HasValue;

        if (hasEstimated)
            result.SetEstimated(request.EstimatedRevenue, request.EstimatedSavings, request.EstimatedCost);

        // Update actual group only if at least one actual value is present
        bool hasActual = request.ActualRevenue.HasValue
            || request.ActualSavings.HasValue
            || request.ActualCost.HasValue;

        if (hasActual)
            result.SetActual(request.ActualRevenue, request.ActualSavings, request.ActualCost);

        // Notes and payback always updated when provided
        if (request.PaybackPeriodMonths.HasValue || request.Notes is not null)
            result.SetNotes(request.PaybackPeriodMonths, request.Notes);

        var audit = AuditLog.Create(
            "Project", request.ProjectId, "ResultRecorded", actorId, actorName);

        await _context.SaveChangesWithAuditAsync(new[] { audit }, cancellationToken);

        return ToDto(result);
    }

    private static ResultDto ToDto(Result r) => new(
        r.Id, r.ProjectId,
        r.EstimatedRevenue, r.EstimatedSavings, r.EstimatedCost, r.EstimatedROI, r.EstimatedRecordedAt,
        r.ActualRevenue, r.ActualSavings, r.ActualCost, r.ActualROI, r.ActualRecordedAt,
        r.PaybackPeriodMonths, r.Notes,
        r.RecordedBy, r.CreatedAt, r.UpdatedAt);
}
```

- [ ] **Step 6: Create GetResultQuery.cs and GetResultQueryHandler.cs**

`src/Flow.Application/Results/Queries/GetResult/GetResultQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Results.Queries.GetResult;

public record GetResultQuery(Guid ProjectId) : IRequest<ResultDto>;
```

`src/Flow.Application/Results/Queries/GetResult/GetResultQueryHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Results.Queries.GetResult;

public class GetResultQueryHandler : IRequestHandler<GetResultQuery, ResultDto>
{
    private readonly IApplicationDbContext _context;

    public GetResultQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ResultDto> Handle(GetResultQuery request, CancellationToken cancellationToken)
    {
        var r = await _context.Results
            .FirstOrDefaultAsync(x => x.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Result", request.ProjectId);

        return new ResultDto(
            r.Id, r.ProjectId,
            r.EstimatedRevenue, r.EstimatedSavings, r.EstimatedCost, r.EstimatedROI, r.EstimatedRecordedAt,
            r.ActualRevenue, r.ActualSavings, r.ActualCost, r.ActualROI, r.ActualRecordedAt,
            r.PaybackPeriodMonths, r.Notes,
            r.RecordedBy, r.CreatedAt, r.UpdatedAt);
    }
}
```

- [ ] **Step 7: Run handler tests**

```
dotnet test tests/Flow.Application.Tests/ --filter "RecordResultCommandHandlerTests" -v minimal
```

Expected: **2 tests passing**, 0 failures.

- [ ] **Step 8: Build and run all tests**

```
dotnet build src/ -v minimal && dotnet test -v minimal
```

Expected: **107 tests passing** (105 + 2 new handler tests), 0 failures.

- [ ] **Step 9: Commit**

```
git add src/Flow.Application/Results/
git add tests/Flow.Application.Tests/Results/RecordResultCommandHandlerTests.cs
git commit -m "feat: implement Results application layer with upsert command and ROI query"
```

---

## Task 12: Results Controller + Integration Tests

**Files:**
- Create: `src/Flow.API/Controllers/ResultsController.cs`
- Create: `tests/Flow.API.Tests/Results/ResultsControllerTests.cs`

### Step 12.1 — Write failing integration tests

- [ ] **Step 1: Create ResultsControllerTests.cs**

`tests/Flow.API.Tests/Results/ResultsControllerTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Xunit;

namespace Flow.API.Tests.Results;

public class ResultsControllerTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly FlowWebApplicationFactory _factory;

    public ResultsControllerTests(FlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, Guid ProjectId)> CreateProjectAsManagerAsync()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Manager");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ownerId = await GetManagerIdAsync();
        var resp = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "ROI Test Project",
            description = "For result tests",
            priority = "Medium",
            ownerId,
            estimatedCost = (decimal?)null,
            deadline = (DateTimeOffset?)null
        });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var id = Guid.Parse(body.GetProperty("id").GetString()!);
        return (client, id);
    }

    [Fact]
    public async Task GetResult_NoResultExists_Returns404()
    {
        var (client, projectId) = await CreateProjectAsManagerAsync();

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/result");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutResult_WithEstimated_Returns200WithComputedROI()
    {
        var (client, projectId) = await CreateProjectAsManagerAsync();

        var response = await client.PutAsJsonAsync($"/api/v1/projects/{projectId}/result", new
        {
            estimatedRevenue = 100_000m,
            estimatedSavings = 20_000m,
            estimatedCost = 40_000m,
            actualRevenue = (decimal?)null,
            actualSavings = (decimal?)null,
            actualCost = (decimal?)null,
            paybackPeriodMonths = (int?)null,
            notes = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // ROI = (100000 + 20000 - 40000) / 40000 * 100 = 200
        body.GetProperty("estimatedROI").GetDecimal().Should().Be(200m);
        body.GetProperty("actualROI").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task PutResult_UpdateActual_DoesNotClearEstimated()
    {
        var (client, projectId) = await CreateProjectAsManagerAsync();

        // First: set estimated
        await client.PutAsJsonAsync($"/api/v1/projects/{projectId}/result", new
        {
            estimatedRevenue = 100_000m,
            estimatedSavings = 0m,
            estimatedCost = 50_000m,
            actualRevenue = (decimal?)null,
            actualSavings = (decimal?)null,
            actualCost = (decimal?)null,
            paybackPeriodMonths = (int?)null,
            notes = (string?)null
        });

        // Then: set actual only
        var response = await client.PutAsJsonAsync($"/api/v1/projects/{projectId}/result", new
        {
            estimatedRevenue = (decimal?)null,
            estimatedSavings = (decimal?)null,
            estimatedCost = (decimal?)null,
            actualRevenue = 80_000m,
            actualSavings = 5_000m,
            actualCost = 40_000m,
            paybackPeriodMonths = 12,
            notes = "On track"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Estimated group must still be present
        body.GetProperty("estimatedROI").GetDecimal().Should().Be(100m); // (100000 + 0 - 50000) / 50000 * 100
        // Actual group now populated
        body.GetProperty("actualROI").GetDecimal().Should().NotBe(0m);
        body.GetProperty("notes").GetString().Should().Be("On track");
    }

    [Fact]
    public async Task GetResult_AfterPut_ReturnsStoredValues()
    {
        var (client, projectId) = await CreateProjectAsManagerAsync();

        await client.PutAsJsonAsync($"/api/v1/projects/{projectId}/result", new
        {
            estimatedRevenue = 50_000m,
            estimatedSavings = 10_000m,
            estimatedCost = 20_000m,
            actualRevenue = (decimal?)null,
            actualSavings = (decimal?)null,
            actualCost = (decimal?)null,
            paybackPeriodMonths = (int?)null,
            notes = "Initial estimate"
        });

        var getResponse = await client.GetAsync($"/api/v1/projects/{projectId}/result");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("estimatedRevenue").GetDecimal().Should().Be(50_000m);
        body.GetProperty("notes").GetString().Should().Be("Initial estimate");
    }

    private async Task<string> GetManagerIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Flow.Domain.Entities.User>>();
        var email = $"result-owner-{Guid.NewGuid():N}@flow.test";
        var user = Flow.Domain.Entities.User.Create("Result Owner", email, Flow.Domain.Enums.UserRole.Manager);
        await userManager.CreateAsync(user, "Test123!");
        await userManager.AddToRoleAsync(user, "Manager");
        return user.Id.ToString();
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```
dotnet test tests/Flow.API.Tests/ --filter "ResultsControllerTests" -v minimal
```

Expected: FAIL — `ResultsController` does not exist yet.

### Step 12.2 — Implement ResultsController

- [ ] **Step 3: Create ResultsController.cs**

`src/Flow.API/Controllers/ResultsController.cs`:
```csharp
using Flow.Application.Results;
using Flow.Application.Results.Commands.RecordResult;
using Flow.Application.Results.Queries.GetResult;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/result")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ResultsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<ResultDto>> Get(Guid projectId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetResultQuery(projectId), ct);
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<ResultDto>> Upsert(
        Guid projectId,
        [FromBody] RecordResultCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command with { ProjectId = projectId }, ct);
        return Ok(result);
    }
}
```

- [ ] **Step 4: Run integration tests**

```
dotnet test tests/Flow.API.Tests/ --filter "ResultsControllerTests" -v minimal
```

Expected: **4 tests passing**, 0 failures.

- [ ] **Step 5: Run full test suite**

```
dotnet test -v minimal
```

Expected: **111 tests passing** (107 + 4 new), 0 failures.

- [ ] **Step 6: Commit**

```
git add src/Flow.API/Controllers/ResultsController.cs
git add tests/Flow.API.Tests/Results/ResultsControllerTests.cs
git commit -m "feat: implement Results controller with upsert and GET endpoints"
```

---

## Task 13: Dashboard Application Layer + Controller + Integration Tests

**Files:**
- Create: `src/Flow.Application/Dashboard/DashboardSummaryDto.cs`
- Create: `src/Flow.Application/Dashboard/BlockedProjectDto.cs`
- Create: `src/Flow.Application/Dashboard/Queries/GetDashboardSummary/GetDashboardSummaryQuery.cs`
- Create: `src/Flow.Application/Dashboard/Queries/GetDashboardSummary/GetDashboardSummaryQueryHandler.cs`
- Create: `src/Flow.API/Controllers/DashboardController.cs`
- Create: `tests/Flow.API.Tests/Dashboard/DashboardControllerTests.cs`

### Step 13.1 — Write failing integration tests

- [ ] **Step 1: Create DashboardControllerTests.cs**

`tests/Flow.API.Tests/Dashboard/DashboardControllerTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Xunit;

namespace Flow.API.Tests.Dashboard;

public class DashboardControllerTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly FlowWebApplicationFactory _factory;

    public DashboardControllerTests(FlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateManagerClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Manager");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task GetSummary_AsManager_Returns200WithExpectedShape()
    {
        var client = await CreateManagerClientAsync();

        var response = await client.GetAsync("/api/v1/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalIdeas").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("approvedIdeas").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("rejectedIdeas").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("pendingIdeas").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("conversionRate").GetDouble().Should().BeGreaterThanOrEqualTo(0.0);
        body.GetProperty("activeProjects").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("blockedProjects").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("completedProjects").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("totalRoi").GetDecimal().Should().BeGreaterThanOrEqualTo(0m);
        body.GetProperty("averageCompletionDays").GetDouble().Should().BeGreaterThanOrEqualTo(0.0);
        body.GetProperty("bottleneckIndex").GetDouble().Should().BeGreaterThanOrEqualTo(0.0);
        body.GetProperty("blockedProjectList").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetSummary_AsOperator_Returns403()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSummary_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```
dotnet test tests/Flow.API.Tests/ --filter "DashboardControllerTests" -v minimal
```

Expected: FAIL — `DashboardController` does not exist.

### Step 13.2 — Implement DTOs, query, and handler

- [ ] **Step 3: Create DTOs**

`src/Flow.Application/Dashboard/BlockedProjectDto.cs`:
```csharp
namespace Flow.Application.Dashboard;

public record BlockedProjectDto(
    Guid Id,
    string Title,
    Guid OwnerId,
    string BlockedReason,
    int DaysBlocked);
```

`src/Flow.Application/Dashboard/DashboardSummaryDto.cs`:
```csharp
namespace Flow.Application.Dashboard;

public record DashboardSummaryDto(
    int TotalIdeas,
    int ApprovedIdeas,
    int RejectedIdeas,
    int PendingIdeas,
    double ConversionRate,
    int ActiveProjects,
    int BlockedProjects,
    int CompletedProjects,
    decimal TotalRoi,
    double AverageCompletionDays,
    double BottleneckIndex,
    IReadOnlyList<BlockedProjectDto> BlockedProjectList);
```

- [ ] **Step 4: Create GetDashboardSummaryQuery.cs**

`src/Flow.Application/Dashboard/Queries/GetDashboardSummary/GetDashboardSummaryQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;
```

- [ ] **Step 5: Create GetDashboardSummaryQueryHandler.cs**

`src/Flow.Application/Dashboard/Queries/GetDashboardSummary/GetDashboardSummaryQueryHandler.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetDashboardSummaryQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        // --- Idea counts ---
        var totalIdeas = await _context.Ideas.CountAsync(cancellationToken);
        var approvedIdeas = await _context.Ideas
            .CountAsync(i => i.Status == IdeaStatus.Approved, cancellationToken);
        var rejectedIdeas = await _context.Ideas
            .CountAsync(i => i.Status == IdeaStatus.Rejected, cancellationToken);
        var pendingIdeas = await _context.Ideas
            .CountAsync(i => i.Status == IdeaStatus.UnderReview, cancellationToken);

        // --- Conversion rate: approved ideas that became projects ---
        var convertedCount = await _context.Projects
            .CountAsync(p => p.SourceIdeaId != null, cancellationToken);
        var conversionRate = approvedIdeas > 0
            ? Math.Round((double)convertedCount / approvedIdeas * 100, 1)
            : 0.0;

        // --- Project counts by status ---
        var activeProjects = await _context.Projects
            .CountAsync(p => p.Status == ProjectStatus.InProgress, cancellationToken);
        var blockedProjects = await _context.Projects
            .CountAsync(p => p.Status == ProjectStatus.Blocked, cancellationToken);
        var completedProjects = await _context.Projects
            .CountAsync(p => p.Status == ProjectStatus.Completed, cancellationToken);

        // --- Total actual ROI across all results ---
        var totalRoi = await _context.Results
            .Where(r => r.ActualROI.HasValue)
            .Select(r => r.ActualROI!.Value)
            .SumAsync(cancellationToken);

        // --- Average completion time (days from StartDate to CompletedAt) ---
        var completedData = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Completed
                && p.StartDate != null
                && p.CompletedAt != null)
            .Select(p => new { p.StartDate, p.CompletedAt })
            .ToListAsync(cancellationToken);

        var averageCompletionDays = completedData.Count > 0
            ? Math.Round(
                completedData.Average(p =>
                    (p.CompletedAt!.Value - p.StartDate!.Value).TotalDays), 1)
            : 0.0;

        // --- Bottleneck index: % of active+blocked that are blocked ---
        var bottleneckIndex = (activeProjects + blockedProjects) > 0
            ? Math.Round((double)blockedProjects / (activeProjects + blockedProjects) * 100, 1)
            : 0.0;

        // --- Blocked project list with days blocked ---
        var blockedList = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Blocked)
            .Select(p => new { p.Id, p.Title, p.OwnerId, p.BlockedReason })
            .ToListAsync(cancellationToken);

        var blockedIds = blockedList.Select(p => p.Id).ToList();

        // Resolve "blocked since" from the most recent snapshot with TriggerAction == "Blocked"
        var blockedSinceMap = new Dictionary<Guid, DateTimeOffset>();
        if (blockedIds.Count > 0)
        {
            var sinceList = await _context.ProjectSnapshots
                .Where(s => blockedIds.Contains(s.ProjectId) && s.TriggerAction == "Blocked")
                .GroupBy(s => s.ProjectId)
                .Select(g => new { ProjectId = g.Key, BlockedAt = g.Max(s => s.TakenAt) })
                .ToListAsync(cancellationToken);

            blockedSinceMap = sinceList.ToDictionary(x => x.ProjectId, x => x.BlockedAt);
        }

        var now = DateTimeOffset.UtcNow;
        var blockedProjectList = blockedList
            .Select(p => new BlockedProjectDto(
                p.Id,
                p.Title,
                p.OwnerId,
                p.BlockedReason ?? string.Empty,
                blockedSinceMap.TryGetValue(p.Id, out var blockedAt)
                    ? (int)(now - blockedAt).TotalDays
                    : 0))
            .ToList();

        return new DashboardSummaryDto(
            totalIdeas,
            approvedIdeas,
            rejectedIdeas,
            pendingIdeas,
            conversionRate,
            activeProjects,
            blockedProjects,
            completedProjects,
            totalRoi,
            averageCompletionDays,
            bottleneckIndex,
            blockedProjectList);
    }
}
```

### Step 13.3 — Implement DashboardController

- [ ] **Step 6: Create DashboardController.cs**

`src/Flow.API/Controllers/DashboardController.cs`:
```csharp
using Flow.Application.Dashboard;
using Flow.Application.Dashboard.Queries.GetDashboardSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Roles = "Manager,Leadership")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardSummaryQuery(), ct);
        return Ok(result);
    }
}
```

- [ ] **Step 7: Run integration tests**

```
dotnet test tests/Flow.API.Tests/ --filter "DashboardControllerTests" -v minimal
```

Expected: **3 tests passing**, 0 failures.

- [ ] **Step 8: Run full test suite**

```
dotnet test -v minimal
```

Expected: **114 tests passing** (111 + 3 new), 0 failures.

- [ ] **Step 9: Commit**

```
git add src/Flow.Application/Dashboard/
git add src/Flow.API/Controllers/DashboardController.cs
git add tests/Flow.API.Tests/Dashboard/DashboardControllerTests.cs
git commit -m "feat: implement Dashboard summary with bottleneck visibility, ROI totals, and conversion rate"
```

---

## Task 14: Gamification Application Layer + Controller + Integration Tests

**Files:**
- Create: `src/Flow.Application/Gamification/PointsSummaryDto.cs`
- Create: `src/Flow.Application/Gamification/PointsLedgerEntryDto.cs`
- Create: `src/Flow.Application/Gamification/Queries/GetMyPoints/GetMyPointsQuery.cs`
- Create: `src/Flow.Application/Gamification/Queries/GetMyPoints/GetMyPointsQueryHandler.cs`
- Create: `src/Flow.Application/Gamification/Queries/GetMyPointsLedger/GetMyPointsLedgerQuery.cs`
- Create: `src/Flow.Application/Gamification/Queries/GetMyPointsLedger/GetMyPointsLedgerQueryHandler.cs`
- Create: `src/Flow.Application/Gamification/Queries/GetUserPoints/GetUserPointsQuery.cs`
- Create: `src/Flow.Application/Gamification/Queries/GetUserPoints/GetUserPointsQueryHandler.cs`
- Create: `src/Flow.API/Controllers/UsersController.cs`
- Create: `tests/Flow.API.Tests/Users/UsersControllerTests.cs`

### Step 14.1 — Write failing integration tests

- [ ] **Step 1: Create UsersControllerTests.cs**

`tests/Flow.API.Tests/Users/UsersControllerTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Xunit;

namespace Flow.API.Tests.Users;

public class UsersControllerTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly FlowWebApplicationFactory _factory;

    public UsersControllerTests(FlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyPoints_AsOperator_Returns200WithPoints()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/users/me/points");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("points").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("userId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetMyPointsLedger_AsOperator_Returns200WithList()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/users/me/points/ledger");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetMyPoints_AsManager_Returns403()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Manager");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/users/me/points");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserPoints_AsManager_Returns200()
    {
        // Create an operator user to look up
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Flow.Domain.Entities.User>>();
        var email = $"points-target-{Guid.NewGuid():N}@flow.test";
        var targetUser = Flow.Domain.Entities.User.Create("Points Target", email, Flow.Domain.Enums.UserRole.Operator);
        await userManager.CreateAsync(targetUser, "Test123!");
        await userManager.AddToRoleAsync(targetUser, "Operator");

        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Manager");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/v1/users/{targetUser.Id}/points");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("userId").GetString().Should().Be(targetUser.Id.ToString());
        body.GetProperty("points").GetInt32().Should().Be(0); // new user, no points yet
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```
dotnet test tests/Flow.API.Tests/ --filter "UsersControllerTests" -v minimal
```

Expected: FAIL — `UsersController` does not exist.

### Step 14.2 — Implement DTOs and query handlers

- [ ] **Step 3: Create DTOs**

`src/Flow.Application/Gamification/PointsSummaryDto.cs`:
```csharp
namespace Flow.Application.Gamification;

public record PointsSummaryDto(Guid UserId, string UserName, int Points);
```

`src/Flow.Application/Gamification/PointsLedgerEntryDto.cs`:
```csharp
namespace Flow.Application.Gamification;

public record PointsLedgerEntryDto(
    Guid Id,
    int Points,
    string Reason,
    string ReferenceType,
    Guid ReferenceId,
    DateTimeOffset AwardedAt);
```

- [ ] **Step 4: Create GetMyPoints query and handler**

`src/Flow.Application/Gamification/Queries/GetMyPoints/GetMyPointsQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Gamification.Queries.GetMyPoints;

public record GetMyPointsQuery : IRequest<PointsSummaryDto>;
```

`src/Flow.Application/Gamification/Queries/GetMyPoints/GetMyPointsQueryHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Gamification.Queries.GetMyPoints;

public class GetMyPointsQueryHandler : IRequestHandler<GetMyPointsQuery, PointsSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyPointsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PointsSummaryDto> Handle(GetMyPointsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        return new PointsSummaryDto(user.Id, user.Name, user.Points);
    }
}
```

- [ ] **Step 5: Create GetMyPointsLedger query and handler**

`src/Flow.Application/Gamification/Queries/GetMyPointsLedger/GetMyPointsLedgerQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Gamification.Queries.GetMyPointsLedger;

public record GetMyPointsLedgerQuery : IRequest<IReadOnlyList<PointsLedgerEntryDto>>;
```

`src/Flow.Application/Gamification/Queries/GetMyPointsLedger/GetMyPointsLedgerQueryHandler.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Gamification.Queries.GetMyPointsLedger;

public class GetMyPointsLedgerQueryHandler
    : IRequestHandler<GetMyPointsLedgerQuery, IReadOnlyList<PointsLedgerEntryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyPointsLedgerQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PointsLedgerEntryDto>> Handle(
        GetMyPointsLedgerQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user identity could not be resolved.");

        var entries = await _context.PointLedgerEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.AwardedAt)
            .Select(e => new { e.Id, e.Points, e.Reason, e.ReferenceType, e.ReferenceId, e.AwardedAt })
            .ToListAsync(cancellationToken);

        return entries
            .Select(e => new PointsLedgerEntryDto(
                e.Id, e.Points, e.Reason, e.ReferenceType, e.ReferenceId, e.AwardedAt))
            .ToList();
    }
}
```

- [ ] **Step 6: Create GetUserPoints query and handler**

`src/Flow.Application/Gamification/Queries/GetUserPoints/GetUserPointsQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Gamification.Queries.GetUserPoints;

public record GetUserPointsQuery(Guid UserId) : IRequest<PointsSummaryDto>;
```

`src/Flow.Application/Gamification/Queries/GetUserPoints/GetUserPointsQueryHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Gamification.Queries.GetUserPoints;

public class GetUserPointsQueryHandler : IRequestHandler<GetUserPointsQuery, PointsSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserPointsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PointsSummaryDto> Handle(
        GetUserPointsQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        return new PointsSummaryDto(user.Id, user.Name, user.Points);
    }
}
```

### Step 14.3 — Implement UsersController

- [ ] **Step 7: Create UsersController.cs**

`src/Flow.API/Controllers/UsersController.cs`:
```csharp
using Flow.Application.Gamification;
using Flow.Application.Gamification.Queries.GetMyPoints;
using Flow.Application.Gamification.Queries.GetMyPointsLedger;
using Flow.Application.Gamification.Queries.GetUserPoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("me/points")]
    [Authorize(Roles = "Operator")]
    public async Task<ActionResult<PointsSummaryDto>> GetMyPoints(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyPointsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("me/points/ledger")]
    [Authorize(Roles = "Operator")]
    public async Task<ActionResult<IReadOnlyList<PointsLedgerEntryDto>>> GetMyPointsLedger(
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyPointsLedgerQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/points")]
    [Authorize(Roles = "Manager,Leadership")]
    public async Task<ActionResult<PointsSummaryDto>> GetUserPoints(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserPointsQuery(id), ct);
        return Ok(result);
    }
}
```

- [ ] **Step 8: Run gamification integration tests**

```
dotnet test tests/Flow.API.Tests/ --filter "UsersControllerTests" -v minimal
```

Expected: **4 tests passing**, 0 failures.

- [ ] **Step 9: Run full test suite**

```
dotnet test -v minimal
```

Expected: **118 tests passing** (114 + 4 new), 0 failures.

- [ ] **Step 10: Commit**

```
git add src/Flow.Application/Gamification/
git add src/Flow.API/Controllers/UsersController.cs
git add tests/Flow.API.Tests/Users/UsersControllerTests.cs
git commit -m "feat: implement gamification endpoints — operator points summary, ledger, and manager lookup"
```

---

## Phase 3 Complete — Final Verification

- [ ] **Run the complete test suite one final time**

```
dotnet test -v minimal
```

Expected: **118 tests passing** (or more — exact count depends on runtime), 0 failures.

- [ ] **Verify the solution builds cleanly**

```
dotnet build -v minimal
```

Expected: **Build succeeded**, 0 errors, only pre-existing NU1603 package approximation warnings.

- [ ] **Tag Phase 3 completion**

```
git tag phase3-complete
```

---

**Phase 3 delivered.**

| Module | Endpoints | Business Value |
|---|---|---|
| Results | GET/PUT `/projects/{id}/result` | Estimated and actual ROI independently tracked; computed on save |
| Dashboard | GET `/dashboard/summary` | 7 key metrics + bottleneck index + blocked project list with days blocked |
| Gamification | GET `/users/me/points`, `/me/points/ledger`, `/{id}/points` | Operator engagement visibility; manager oversight |

**ROI formula (transparent):**
```
ROI = (Revenue + Savings - Cost) / Cost × 100
Returns null if Cost is null or zero.
```

**Bottleneck formula (transparent):**
```
BottleneckIndex = BlockedProjects / (ActiveProjects + BlockedProjects) × 100
```

**Proceed to Phase 4 (Mobile Application) when ready.**
