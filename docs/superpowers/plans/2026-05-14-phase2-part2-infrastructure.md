# Phase 2 — Part 2: Infrastructure & Strategic Guidelines Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Part sequence:** Part 2 of 4. Requires Part 1 complete. After this, proceed to Part 3 (`2026-05-14-phase2-part3-ideas.md`).

**Goal:** Wire all new domain entities into EF Core, run the migration, and deliver the Strategic Guidelines feature end-to-end.

**Architecture:** EF configurations are one-class-per-entity in `Flow.Infrastructure.Persistence.Configurations`. `IApplicationDbContext` and `ApplicationDbContext` are extended with new DbSets. The Strategic Guidelines feature follows the same CQRS-lite pattern established in Phase 1: MediatR commands/queries handled in the Application layer, thin controller in the API layer.

**Tech Stack:** EF Core 8, ASP.NET Core 8, MediatR 12, xUnit, FluentAssertions, WebApplicationFactory.

---

## File Map

**Create:**
- `src/Flow.Domain/Entities/StrategicGuideline.cs`
- `src/Flow.Domain/Entities/PointLedgerEntry.cs`
- `src/Flow.Infrastructure/Persistence/Configurations/IdeaConfiguration.cs`
- `src/Flow.Infrastructure/Persistence/Configurations/IdeaCommentConfiguration.cs`
- `src/Flow.Infrastructure/Persistence/Configurations/ProjectConfiguration.cs`
- `src/Flow.Infrastructure/Persistence/Configurations/ProjectSnapshotConfiguration.cs`
- `src/Flow.Infrastructure/Persistence/Configurations/StrategicGuidelineConfiguration.cs`
- `src/Flow.Infrastructure/Persistence/Configurations/PointLedgerEntryConfiguration.cs`
- `src/Flow.Application/Guidelines/GuidelineDto.cs`
- `src/Flow.Application/Guidelines/Commands/CreateGuideline/CreateGuidelineCommand.cs`
- `src/Flow.Application/Guidelines/Commands/CreateGuideline/CreateGuidelineCommandHandler.cs`
- `src/Flow.Application/Guidelines/Commands/UpdateGuideline/UpdateGuidelineCommand.cs`
- `src/Flow.Application/Guidelines/Commands/UpdateGuideline/UpdateGuidelineCommandHandler.cs`
- `src/Flow.Application/Guidelines/Commands/DeleteGuideline/DeleteGuidelineCommand.cs`
- `src/Flow.Application/Guidelines/Commands/DeleteGuideline/DeleteGuidelineCommandHandler.cs`
- `src/Flow.Application/Guidelines/Queries/GetGuidelines/GetGuidelinesQuery.cs`
- `src/Flow.Application/Guidelines/Queries/GetGuidelines/GetGuidelinesQueryHandler.cs`
- `src/Flow.Application/Guidelines/Queries/GetGuidelineById/GetGuidelineByIdQuery.cs`
- `src/Flow.Application/Guidelines/Queries/GetGuidelineById/GetGuidelineByIdQueryHandler.cs`
- `src/Flow.API/Controllers/GuidelinesController.cs`
- `tests/Flow.API.Tests/Guidelines/GuidelinesControllerTests.cs`

**Modify:**
- `src/Flow.Application/Common/Interfaces/IApplicationDbContext.cs` — add new DbSets
- `src/Flow.Infrastructure/Persistence/ApplicationDbContext.cs` — add new DbSets

---

## Task 4: Supporting Entities, EF Configurations, DbContext Update, Migration

**Files:**
- Create: `src/Flow.Domain/Entities/StrategicGuideline.cs`
- Create: `src/Flow.Domain/Entities/PointLedgerEntry.cs`
- Create: 6 EF configuration files
- Modify: `src/Flow.Application/Common/Interfaces/IApplicationDbContext.cs`
- Modify: `src/Flow.Infrastructure/Persistence/ApplicationDbContext.cs`

- [ ] **Step 1: Create StrategicGuideline entity**

`src/Flow.Domain/Entities/StrategicGuideline.cs`:
```csharp
namespace Flow.Domain.Entities;

public class StrategicGuideline
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private StrategicGuideline() { }

    public static StrategicGuideline Create(string title, string description, Guid createdBy)
    {
        var now = DateTimeOffset.UtcNow;
        return new StrategicGuideline
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string description)
    {
        Title = title;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

- [ ] **Step 2: Create PointLedgerEntry entity**

`src/Flow.Domain/Entities/PointLedgerEntry.cs`:
```csharp
namespace Flow.Domain.Entities;

public class PointLedgerEntry
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int Points { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid ReferenceId { get; private set; }
    public DateTimeOffset AwardedAt { get; private set; }

    private PointLedgerEntry() { }

    public static PointLedgerEntry Create(
        Guid userId,
        int points,
        string reason,
        string referenceType,
        Guid referenceId)
    {
        return new PointLedgerEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Points = points,
            Reason = reason,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            AwardedAt = DateTimeOffset.UtcNow
        };
    }
}
```

- [ ] **Step 3: Create all EF Core configurations**

`src/Flow.Infrastructure/Persistence/Configurations/IdeaConfiguration.cs`:
```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class IdeaConfiguration : IEntityTypeConfiguration<Idea>
{
    public void Configure(EntityTypeBuilder<Idea> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Title).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(i => i.Problem).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(i => i.SubmittedBy).IsRequired();
        builder.Property(i => i.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(i => i.Priority).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(i => i.ManagerComment).HasMaxLength(2000);
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();

        builder.HasIndex(i => i.SubmittedBy);
        builder.HasIndex(i => i.Status);
    }
}
```

`src/Flow.Infrastructure/Persistence/Configurations/IdeaCommentConfiguration.cs`:
```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class IdeaCommentConfiguration : IEntityTypeConfiguration<IdeaComment>
{
    public void Configure(EntityTypeBuilder<IdeaComment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Body).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(c => c.AuthorId).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.IdeaId);
    }
}
```

`src/Flow.Infrastructure/Persistence/Configurations/ProjectConfiguration.cs`:
```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(p => p.OwnerId).IsRequired();
        builder.Property(p => p.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.Priority).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ActualCost).HasColumnType("decimal(18,2)");
        builder.Property(p => p.BlockedReason).HasMaxLength(1000);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.OwnerId);
    }
}
```

`src/Flow.Infrastructure/Persistence/Configurations/ProjectSnapshotConfiguration.cs`:
```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class ProjectSnapshotConfiguration : IEntityTypeConfiguration<ProjectSnapshot>
{
    public void Configure(EntityTypeBuilder<ProjectSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProjectId).IsRequired();
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(s => s.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.Priority).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.OwnerId).IsRequired();
        builder.Property(s => s.OwnerName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(s => s.ActualCost).HasColumnType("decimal(18,2)");
        builder.Property(s => s.TriggerAction).IsRequired().HasMaxLength(100);
        builder.Property(s => s.TriggeredByActorId).IsRequired();
        builder.Property(s => s.TakenAt).IsRequired();

        builder.HasIndex(s => s.ProjectId);
    }
}
```

`src/Flow.Infrastructure/Persistence/Configurations/StrategicGuidelineConfiguration.cs`:
```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class StrategicGuidelineConfiguration : IEntityTypeConfiguration<StrategicGuideline>
{
    public void Configure(EntityTypeBuilder<StrategicGuideline> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Title).IsRequired().HasMaxLength(200);
        builder.Property(g => g.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(g => g.CreatedBy).IsRequired();
        builder.Property(g => g.CreatedAt).IsRequired();
        builder.Property(g => g.UpdatedAt).IsRequired();
    }
}
```

`src/Flow.Infrastructure/Persistence/Configurations/PointLedgerEntryConfiguration.cs`:
```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class PointLedgerEntryConfiguration : IEntityTypeConfiguration<PointLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PointLedgerEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Points).IsRequired();
        builder.Property(e => e.Reason).IsRequired().HasMaxLength(500);
        builder.Property(e => e.ReferenceType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ReferenceId).IsRequired();
        builder.Property(e => e.AwardedAt).IsRequired();

        builder.HasIndex(e => e.UserId);
    }
}
```

- [ ] **Step 4: Update IApplicationDbContext with new DbSets**

Replace the full content of `src/Flow.Application/Common/Interfaces/IApplicationDbContext.cs`:
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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Atomically appends auditEntries and saves all pending changes in one transaction.
    // All domain operations that produce audit events must call this instead of SaveChangesAsync.
    Task<int> SaveChangesWithAuditAsync(
        IEnumerable<AuditLog> auditEntries,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: Update ApplicationDbContext with new DbSets**

Add the following properties to `src/Flow.Infrastructure/Persistence/ApplicationDbContext.cs` (after the existing `AuditLogs` property):
```csharp
public DbSet<Idea> Ideas => Set<Idea>();
public DbSet<IdeaComment> IdeaComments => Set<IdeaComment>();
public DbSet<Project> Projects => Set<Project>();
public DbSet<ProjectSnapshot> ProjectSnapshots => Set<ProjectSnapshot>();
public DbSet<StrategicGuideline> StrategicGuidelines => Set<StrategicGuideline>();
public DbSet<PointLedgerEntry> PointLedgerEntries => Set<PointLedgerEntry>();
```

The full file after modification:
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

- [ ] **Step 6: Build to confirm no compile errors**

```
dotnet build src/Flow.Infrastructure/Flow.Infrastructure.csproj -v minimal
```

Expected: **Build succeeded**, 0 errors.

- [ ] **Step 7: Add EF Core migration**

Run from the solution root (`C:\Users\Elias\Documents\Faculdade\Flow`):
```
dotnet ef migrations add AddCoreDomain --project src/Flow.Infrastructure --startup-project src/Flow.API
```

Expected: Migration file created at `src/Flow.Infrastructure/Persistence/Migrations/<timestamp>_AddCoreDomain.cs` with tables for Ideas, IdeaComments, Projects, ProjectSnapshots, StrategicGuidelines, PointLedgerEntries.

- [ ] **Step 8: Run full test suite to confirm no regressions**

```
dotnet test -v minimal
```

Expected: **62 tests passing**, 0 failures. (In-memory DB automatically picks up new schema; no migration needed for tests.)

- [ ] **Step 9: Commit**

```
git add src/Flow.Domain/Entities/StrategicGuideline.cs src/Flow.Domain/Entities/PointLedgerEntry.cs
git add src/Flow.Infrastructure/Persistence/Configurations/
git add src/Flow.Application/Common/Interfaces/IApplicationDbContext.cs
git add src/Flow.Infrastructure/Persistence/ApplicationDbContext.cs
git add src/Flow.Infrastructure/Persistence/Migrations/
git commit -m "feat: add core domain entities, EF configurations, and AddCoreDomain migration"
```

---

## Task 5: Strategic Guidelines Feature

**Files:**
- Create: `src/Flow.Application/Guidelines/GuidelineDto.cs`
- Create: `src/Flow.Application/Guidelines/Commands/CreateGuideline/CreateGuidelineCommand.cs`
- Create: `src/Flow.Application/Guidelines/Commands/CreateGuideline/CreateGuidelineCommandHandler.cs`
- Create: `src/Flow.Application/Guidelines/Commands/UpdateGuideline/UpdateGuidelineCommand.cs`
- Create: `src/Flow.Application/Guidelines/Commands/UpdateGuideline/UpdateGuidelineCommandHandler.cs`
- Create: `src/Flow.Application/Guidelines/Commands/DeleteGuideline/DeleteGuidelineCommand.cs`
- Create: `src/Flow.Application/Guidelines/Commands/DeleteGuideline/DeleteGuidelineCommandHandler.cs`
- Create: `src/Flow.Application/Guidelines/Queries/GetGuidelines/GetGuidelinesQuery.cs`
- Create: `src/Flow.Application/Guidelines/Queries/GetGuidelines/GetGuidelinesQueryHandler.cs`
- Create: `src/Flow.Application/Guidelines/Queries/GetGuidelineById/GetGuidelineByIdQuery.cs`
- Create: `src/Flow.Application/Guidelines/Queries/GetGuidelineById/GetGuidelineByIdQueryHandler.cs`
- Create: `src/Flow.API/Controllers/GuidelinesController.cs`
- Create: `tests/Flow.API.Tests/Guidelines/GuidelinesControllerTests.cs`

- [ ] **Step 1: Write the failing integration tests**

`tests/Flow.API.Tests/Guidelines/GuidelinesControllerTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Xunit;

namespace Flow.API.Tests.Guidelines;

public class GuidelinesControllerTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly FlowWebApplicationFactory _factory;

    public GuidelinesControllerTests(FlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetGuidelines_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/guidelines");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateGuideline_AsLeadership_Returns201WithGuideline()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Leadership");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/guidelines", new
        {
            title = "Operational Excellence",
            description = "Focus on reducing waste in core processes."
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("title").GetString().Should().Be("Operational Excellence");
    }

    [Fact]
    public async Task CreateGuideline_AsOperator_Returns403()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/guidelines", new
        {
            title = "Some Guideline",
            description = "Desc"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetGuidelines_AsOperator_Returns200WithList()
    {
        var client = _factory.CreateClient();

        // Create a guideline as Leadership first
        var leaderToken = await _factory.GetTokenForRoleAsync("Leadership");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        await client.PostAsJsonAsync("/api/v1/guidelines", new
        {
            title = "Digital Transformation",
            description = "Accelerate digital adoption."
        });

        // Read as Operator
        var operatorToken = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);
        var response = await client.GetAsync("/api/v1/guidelines");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateGuideline_AsLeadership_Returns204()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Leadership");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/v1/guidelines", new
        {
            title = "Original Title",
            description = "Original description."
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/guidelines/{id}", new
        {
            title = "Updated Title",
            description = "Updated description."
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteGuideline_AsLeadership_Returns204()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Leadership");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/v1/guidelines", new
        {
            title = "To Be Deleted",
            description = "This will be removed."
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var deleteResponse = await client.DeleteAsync($"/api/v1/guidelines/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
```

- [ ] **Step 2: Add `GetTokenForRoleAsync` helper to FlowWebApplicationFactory**

Add the following using directives and method to `tests/Flow.API.Tests/Helpers/FlowWebApplicationFactory.cs`:

```csharp
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
```

Add this method to the `FlowWebApplicationFactory` class:
```csharp
public async Task<string> GetTokenForRoleAsync(string role)
{
    using var scope = Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

    var email = $"test-{role.ToLower()}-{Guid.NewGuid():N}@flow.test";
    var userRole = Enum.Parse<UserRole>(role);
    var user = User.Create($"Test {role}", email, userRole);

    await userManager.CreateAsync(user, "Test123!");
    await userManager.AddToRoleAsync(user, role);

    var roles = await userManager.GetRolesAsync(user);
    return jwtService.GenerateAccessToken(user, roles);
}
```

The full updated `FlowWebApplicationFactory.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Flow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.API.Tests.Helpers;

public class FlowWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb-" + Guid.NewGuid();
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-key-must-be-at-least-256-bits-long-for-hmac",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:ExpiryMinutes"] = "15"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName, _dbRoot));
        });
    }

    public async Task<string> GetTokenForRoleAsync(string role)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var email = $"test-{role.ToLower()}-{Guid.NewGuid():N}@flow.test";
        var userRole = Enum.Parse<UserRole>(role);
        var user = User.Create($"Test {role}", email, userRole);

        await userManager.CreateAsync(user, "Test123!");
        await userManager.AddToRoleAsync(user, role);

        var roles = await userManager.GetRolesAsync(user);
        return jwtService.GenerateAccessToken(user, roles);
    }
}
```

- [ ] **Step 3: Run the failing tests**

```
dotnet test tests/Flow.API.Tests/ --filter "GuidelinesControllerTests" -v minimal
```

Expected: FAIL — controller and application layer do not exist yet.

- [ ] **Step 4: Implement GuidelineDto**

`src/Flow.Application/Guidelines/GuidelineDto.cs`:
```csharp
namespace Flow.Application.Guidelines;

public record GuidelineDto(
    Guid Id,
    string Title,
    string Description,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
```

- [ ] **Step 5: Implement CreateGuideline command and handler**

`src/Flow.Application/Guidelines/Commands/CreateGuideline/CreateGuidelineCommand.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Guidelines.Commands.CreateGuideline;

public record CreateGuidelineCommand(
    [Required] string Title,
    [Required] string Description) : IRequest<GuidelineDto>;
```

`src/Flow.Application/Guidelines/Commands/CreateGuideline/CreateGuidelineCommandHandler.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;

namespace Flow.Application.Guidelines.Commands.CreateGuideline;

public class CreateGuidelineCommandHandler : IRequestHandler<CreateGuidelineCommand, GuidelineDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateGuidelineCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<GuidelineDto> Handle(CreateGuidelineCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId!.Value;
        var guideline = StrategicGuideline.Create(request.Title, request.Description, actorId);

        _context.StrategicGuidelines.Add(guideline);
        await _context.SaveChangesAsync(cancellationToken);

        return new GuidelineDto(
            guideline.Id, guideline.Title, guideline.Description,
            guideline.CreatedBy, guideline.CreatedAt, guideline.UpdatedAt);
    }
}
```

- [ ] **Step 6: Implement UpdateGuideline command and handler**

`src/Flow.Application/Guidelines/Commands/UpdateGuideline/UpdateGuidelineCommand.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Flow.Application.Guidelines.Commands.UpdateGuideline;

public record UpdateGuidelineCommand(
    Guid Id,
    [Required] string Title,
    [Required] string Description) : IRequest;
```

`src/Flow.Application/Guidelines/Commands/UpdateGuideline/UpdateGuidelineCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Guidelines.Commands.UpdateGuideline;

public class UpdateGuidelineCommandHandler : IRequestHandler<UpdateGuidelineCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateGuidelineCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateGuidelineCommand request, CancellationToken cancellationToken)
    {
        var guideline = await _context.StrategicGuidelines
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Guideline", request.Id);

        guideline.Update(request.Title, request.Description);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 7: Implement DeleteGuideline command and handler**

`src/Flow.Application/Guidelines/Commands/DeleteGuideline/DeleteGuidelineCommand.cs`:
```csharp
using MediatR;

namespace Flow.Application.Guidelines.Commands.DeleteGuideline;

public record DeleteGuidelineCommand(Guid Id) : IRequest;
```

`src/Flow.Application/Guidelines/Commands/DeleteGuideline/DeleteGuidelineCommandHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Guidelines.Commands.DeleteGuideline;

public class DeleteGuidelineCommandHandler : IRequestHandler<DeleteGuidelineCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteGuidelineCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteGuidelineCommand request, CancellationToken cancellationToken)
    {
        var guideline = await _context.StrategicGuidelines
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Guideline", request.Id);

        _context.StrategicGuidelines.Remove(guideline);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 8: Implement GetGuidelines query and handler**

`src/Flow.Application/Guidelines/Queries/GetGuidelines/GetGuidelinesQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Guidelines.Queries.GetGuidelines;

public record GetGuidelinesQuery : IRequest<IReadOnlyList<GuidelineDto>>;
```

`src/Flow.Application/Guidelines/Queries/GetGuidelines/GetGuidelinesQueryHandler.cs`:
```csharp
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Guidelines.Queries.GetGuidelines;

public class GetGuidelinesQueryHandler : IRequestHandler<GetGuidelinesQuery, IReadOnlyList<GuidelineDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGuidelinesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<GuidelineDto>> Handle(
        GetGuidelinesQuery request, CancellationToken cancellationToken)
    {
        var guidelines = await _context.StrategicGuidelines
            .OrderBy(g => g.Title)
            .ToListAsync(cancellationToken);

        return guidelines
            .Select(g => new GuidelineDto(
                g.Id, g.Title, g.Description, g.CreatedBy, g.CreatedAt, g.UpdatedAt))
            .ToList();
    }
}
```

- [ ] **Step 9: Implement GetGuidelineById query and handler**

`src/Flow.Application/Guidelines/Queries/GetGuidelineById/GetGuidelineByIdQuery.cs`:
```csharp
using MediatR;

namespace Flow.Application.Guidelines.Queries.GetGuidelineById;

public record GetGuidelineByIdQuery(Guid Id) : IRequest<GuidelineDto>;
```

`src/Flow.Application/Guidelines/Queries/GetGuidelineById/GetGuidelineByIdQueryHandler.cs`:
```csharp
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Guidelines.Queries.GetGuidelineById;

public class GetGuidelineByIdQueryHandler : IRequestHandler<GetGuidelineByIdQuery, GuidelineDto>
{
    private readonly IApplicationDbContext _context;

    public GetGuidelineByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<GuidelineDto> Handle(
        GetGuidelineByIdQuery request, CancellationToken cancellationToken)
    {
        var guideline = await _context.StrategicGuidelines
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Guideline", request.Id);

        return new GuidelineDto(
            guideline.Id, guideline.Title, guideline.Description,
            guideline.CreatedBy, guideline.CreatedAt, guideline.UpdatedAt);
    }
}
```

- [ ] **Step 10: Implement GuidelinesController**

`src/Flow.API/Controllers/GuidelinesController.cs`:
```csharp
using Flow.Application.Guidelines;
using Flow.Application.Guidelines.Commands.CreateGuideline;
using Flow.Application.Guidelines.Commands.DeleteGuideline;
using Flow.Application.Guidelines.Commands.UpdateGuideline;
using Flow.Application.Guidelines.Queries.GetGuidelineById;
using Flow.Application.Guidelines.Queries.GetGuidelines;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1/guidelines")]
[Authorize]
public class GuidelinesController : ControllerBase
{
    private readonly IMediator _mediator;

    public GuidelinesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GuidelineDto>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGuidelinesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GuidelineDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGuidelineByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Leadership")]
    public async Task<ActionResult<GuidelineDto>> Create(
        [FromBody] CreateGuidelineCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Leadership")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateGuidelineCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Leadership")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteGuidelineCommand(id), ct);
        return NoContent();
    }
}
```

- [ ] **Step 11: Run Guidelines integration tests**

```
dotnet test tests/Flow.API.Tests/ --filter "GuidelinesControllerTests" -v minimal
```

Expected: **6 tests passing**, 0 failures.

- [ ] **Step 12: Run full test suite**

```
dotnet test -v minimal
```

Expected: **68 tests passing**, 0 failures.

- [ ] **Step 13: Commit**

```
git add src/Flow.Application/Guidelines/ src/Flow.API/Controllers/GuidelinesController.cs
git add tests/Flow.API.Tests/Guidelines/ tests/Flow.API.Tests/Helpers/FlowWebApplicationFactory.cs
git commit -m "feat: implement Strategic Guidelines feature with role-based access"
```

---

**End of Part 2. Proceed to `2026-05-14-phase2-part3-ideas.md`.**
