# Flow — Phase 1: Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a runnable ASP.NET Core 8 backend with Clean Architecture, JWT authentication (register/login/refresh/logout), role-based access, audit log infrastructure, and base repository — all tested and committed.

**Architecture:** Clean Architecture modular monolith. Domain → Application → Infrastructure → API, with strict inward-only dependency flow. Identity is owned by Infrastructure; the Domain uses `IdentityUser<Guid>` as a pragmatic base. All state-changing operations are routed through MediatR command handlers — never directly from controllers to DbContext.

**Tech Stack:** .NET 8, ASP.NET Core 8, Entity Framework Core 8, ASP.NET Core Identity, MediatR 12, FluentValidation 11, System.IdentityModel.Tokens.Jwt, xUnit, FluentAssertions, Moq, Microsoft.AspNetCore.Mvc.Testing

---

## File Structure

```
Flow/
├── Flow.sln
├── CLAUDE.md
├── PROJECT_MEMORY.md
├── docs/
│   └── superpowers/
│       ├── specs/2026-05-13-flow-mvp-design.md
│       └── plans/2026-05-13-phase-1-foundation.md
├── src/
│   ├── Flow.Domain/
│   │   ├── Flow.Domain.csproj
│   │   ├── Common/
│   │   │   └── BaseEntity.cs                        ← abstract base with Id, CreatedAt, UpdatedAt
│   │   ├── Entities/
│   │   │   ├── User.cs                              ← extends IdentityUser<Guid>; owns Name, Role, Points
│   │   │   ├── RefreshToken.cs                      ← refresh token with IsActive guard
│   │   │   └── AuditLog.cs                          ← immutable event record with Reason field
│   │   └── Enums/
│   │       └── UserRole.cs                          ← Operator | Manager | Leadership
│   ├── Flow.Application/
│   │   ├── Flow.Application.csproj
│   │   ├── DependencyInjection.cs                   ← registers MediatR
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IApplicationDbContext.cs          ← DbSets + SaveChangesWithAuditAsync
│   │   │   │   ├── ICurrentUserService.cs            ← UserId, UserName, IsAuthenticated
│   │   │   │   ├── IJwtTokenService.cs               ← generate/validate tokens
│   │   │   │   └── IRepository.cs                   ← generic CRUD interface
│   │   │   └── Exceptions/
│   │   │       ├── NotFoundException.cs
│   │   │       ├── ForbiddenException.cs
│   │   │       ├── ConflictException.cs
│   │   │       └── ValidationException.cs
│   │   └── Auth/
│   │       ├── AuthResultDto.cs                     ← shared response record
│   │       └── Commands/
│   │           ├── Register/
│   │           │   ├── RegisterCommand.cs
│   │           │   └── RegisterCommandHandler.cs
│   │           ├── Login/
│   │           │   ├── LoginCommand.cs
│   │           │   └── LoginCommandHandler.cs
│   │           ├── RefreshToken/
│   │           │   ├── RefreshTokenCommand.cs
│   │           │   └── RefreshTokenCommandHandler.cs
│   │           └── Logout/
│   │               ├── LogoutCommand.cs
│   │               └── LogoutCommandHandler.cs
│   ├── Flow.Infrastructure/
│   │   ├── Flow.Infrastructure.csproj
│   │   ├── DependencyInjection.cs                   ← registers DbContext, Identity, JwtTokenService
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs               ← IdentityDbContext + IApplicationDbContext
│   │   │   ├── Configurations/
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   ├── RefreshTokenConfiguration.cs
│   │   │   │   └── AuditLogConfiguration.cs
│   │   │   └── Repositories/
│   │   │       └── BaseRepository.cs                ← generic EF Core implementation of IRepository<T>
│   │   └── Auth/
│   │       └── JwtTokenService.cs                   ← access token + refresh token generation/validation
│   └── Flow.API/
│       ├── Flow.API.csproj
│       ├── Program.cs                               ← DI wiring, middleware pipeline, migration + seeding
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Controllers/
│       │   └── AuthController.cs                    ← register, login, refresh, logout endpoints
│       ├── Middleware/
│       │   └── ExceptionHandlingMiddleware.cs        ← maps exceptions to structured HTTP responses
│       └── Services/
│           └── CurrentUserService.cs                ← reads claims from IHttpContextAccessor
└── tests/
    ├── Flow.Application.Tests/
    │   ├── Flow.Application.Tests.csproj
    │   └── Auth/
    │       ├── RegisterCommandHandlerTests.cs
    │       ├── LoginCommandHandlerTests.cs
    │       └── RefreshTokenCommandHandlerTests.cs
    └── Flow.API.Tests/
        ├── Flow.API.Tests.csproj
        ├── Helpers/
        │   └── FlowWebApplicationFactory.cs          ← configures InMemory DB + test JWT settings
        └── Auth/
            └── AuthControllerTests.cs
```

---

## Task 1: Initialize Git and scaffold solution

**Files:**
- Create: `Flow.sln`
- Create: `src/Flow.Domain/Flow.Domain.csproj`
- Create: `src/Flow.Application/Flow.Application.csproj`
- Create: `src/Flow.Infrastructure/Flow.Infrastructure.csproj`
- Create: `src/Flow.API/Flow.API.csproj`
- Create: `tests/Flow.Application.Tests/Flow.Application.Tests.csproj`
- Create: `tests/Flow.API.Tests/Flow.API.Tests.csproj`
- Create: `.gitignore`

- [ ] **Step 1: Initialize git repository**

```bash
cd "C:\Users\Elias\Documents\Faculdade\Flow"
git init
```

- [ ] **Step 2: Create solution and projects**

```bash
dotnet new sln -n Flow
dotnet new classlib -n Flow.Domain -o src/Flow.Domain --framework net8.0
dotnet new classlib -n Flow.Application -o src/Flow.Application --framework net8.0
dotnet new classlib -n Flow.Infrastructure -o src/Flow.Infrastructure --framework net8.0
dotnet new webapi -n Flow.API -o src/Flow.API --framework net8.0
dotnet new xunit -n Flow.Application.Tests -o tests/Flow.Application.Tests --framework net8.0
dotnet new xunit -n Flow.API.Tests -o tests/Flow.API.Tests --framework net8.0
```

- [ ] **Step 3: Add all projects to solution**

```bash
dotnet sln add src/Flow.Domain/Flow.Domain.csproj
dotnet sln add src/Flow.Application/Flow.Application.csproj
dotnet sln add src/Flow.Infrastructure/Flow.Infrastructure.csproj
dotnet sln add src/Flow.API/Flow.API.csproj
dotnet sln add tests/Flow.Application.Tests/Flow.Application.Tests.csproj
dotnet sln add tests/Flow.API.Tests/Flow.API.Tests.csproj
```

- [ ] **Step 4: Wire project references**

```bash
dotnet add src/Flow.Application/Flow.Application.csproj reference src/Flow.Domain/Flow.Domain.csproj
dotnet add src/Flow.Infrastructure/Flow.Infrastructure.csproj reference src/Flow.Application/Flow.Application.csproj
dotnet add src/Flow.Infrastructure/Flow.Infrastructure.csproj reference src/Flow.Domain/Flow.Domain.csproj
dotnet add src/Flow.API/Flow.API.csproj reference src/Flow.Application/Flow.Application.csproj
dotnet add src/Flow.API/Flow.API.csproj reference src/Flow.Infrastructure/Flow.Infrastructure.csproj
dotnet add tests/Flow.Application.Tests/Flow.Application.Tests.csproj reference src/Flow.Application/Flow.Application.csproj
dotnet add tests/Flow.Application.Tests/Flow.Application.Tests.csproj reference src/Flow.Infrastructure/Flow.Infrastructure.csproj
dotnet add tests/Flow.API.Tests/Flow.API.Tests.csproj reference src/Flow.API/Flow.API.csproj
```

- [ ] **Step 5: Install NuGet packages — Domain**

```bash
dotnet add src/Flow.Domain/Flow.Domain.csproj package Microsoft.Extensions.Identity.Core --version 8.0.11
```

- [ ] **Step 6: Install NuGet packages — Application**

```bash
dotnet add src/Flow.Application/Flow.Application.csproj package MediatR --version 12.4.1
dotnet add src/Flow.Application/Flow.Application.csproj package FluentValidation --version 11.11.0
dotnet add src/Flow.Application/Flow.Application.csproj package Microsoft.Extensions.Identity.Core --version 8.0.11
```

- [ ] **Step 7: Install NuGet packages — Infrastructure**

```bash
dotnet add src/Flow.Infrastructure/Flow.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.11
dotnet add src/Flow.Infrastructure/Flow.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Tools --version 8.0.11
dotnet add src/Flow.Infrastructure/Flow.Infrastructure.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11
dotnet add src/Flow.Infrastructure/Flow.Infrastructure.csproj package System.IdentityModel.Tokens.Jwt --version 8.3.4
dotnet add src/Flow.Infrastructure/Flow.Infrastructure.csproj package Microsoft.IdentityModel.Tokens --version 8.3.4
```

- [ ] **Step 8: Install NuGet packages — API**

```bash
dotnet add src/Flow.API/Flow.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11
dotnet add src/Flow.API/Flow.API.csproj package Swashbuckle.AspNetCore --version 6.9.0
```

- [ ] **Step 9: Install NuGet packages — Test projects**

```bash
dotnet add tests/Flow.Application.Tests/Flow.Application.Tests.csproj package FluentAssertions --version 6.12.2
dotnet add tests/Flow.Application.Tests/Flow.Application.Tests.csproj package Moq --version 4.20.72
dotnet add tests/Flow.Application.Tests/Flow.Application.Tests.csproj package Microsoft.Extensions.Identity.Core --version 8.0.11
dotnet add tests/Flow.Application.Tests/Flow.Application.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11
dotnet add tests/Flow.API.Tests/Flow.API.Tests.csproj package FluentAssertions --version 6.12.2
dotnet add tests/Flow.API.Tests/Flow.API.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 8.0.11
dotnet add tests/Flow.API.Tests/Flow.API.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11
```

- [ ] **Step 10: Add .gitignore and delete generated boilerplate**

```bash
dotnet new gitignore
# Delete generated placeholder files
Remove-Item src/Flow.Domain/Class1.cs
Remove-Item src/Flow.Application/Class1.cs
Remove-Item src/Flow.Infrastructure/Class1.cs
Remove-Item src/Flow.API/Controllers/WeatherForecastController.cs
Remove-Item src/Flow.API/WeatherForecast.cs
```

- [ ] **Step 11: Verify solution builds**

```bash
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 12: Commit**

```bash
git add .
git commit -m "chore: initialize solution with Clean Architecture project structure"
```

---

## Task 2: Domain — BaseEntity, UserRole, User

**Files:**
- Create: `src/Flow.Domain/Common/BaseEntity.cs`
- Create: `src/Flow.Domain/Enums/UserRole.cs`
- Create: `src/Flow.Domain/Entities/User.cs`
- Create: `tests/Flow.Application.Tests/Auth/UserEntityTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/Flow.Application.Tests/Auth/UserEntityTests.cs`:

```csharp
using FluentAssertions;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Xunit;

namespace Flow.Application.Tests.Auth;

public class UserEntityTests
{
    [Fact]
    public void Create_ValidInputs_SetsAllProperties()
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Operator);

        user.Name.Should().Be("Ana Lima");
        user.Email.Should().Be("ana@example.com");
        user.UserName.Should().Be("ana@example.com");
        user.Role.Should().Be(UserRole.Operator);
        user.Points.Should().Be(0);
        user.Id.Should().NotBeEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AddPoints_PositiveAmount_IncreasesTotal()
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Operator);

        user.AddPoints(50);

        user.Points.Should().Be(50);
    }

    [Fact]
    public void AddPoints_ZeroOrNegative_ThrowsArgumentException()
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Operator);

        var act = () => user.AddPoints(0);

        act.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "UserEntityTests"
```

Expected: FAIL — `User` type not found.

- [ ] **Step 3: Create BaseEntity**

Create `src/Flow.Domain/Common/BaseEntity.cs`:

```csharp
namespace Flow.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    protected void SetUpdated() => UpdatedAt = DateTimeOffset.UtcNow;
}
```

- [ ] **Step 4: Create UserRole enum**

Create `src/Flow.Domain/Enums/UserRole.cs`:

```csharp
namespace Flow.Domain.Enums;

public enum UserRole
{
    Operator = 1,
    Manager = 2,
    Leadership = 3
}
```

- [ ] **Step 5: Create User entity**

Create `src/Flow.Domain/Entities/User.cs`:

```csharp
using Flow.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Flow.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public int Points { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private User() { }

    public static User Create(string name, string email, UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            Role = role,
            Points = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddPoints(int points)
    {
        if (points <= 0) throw new ArgumentException("Points must be positive.", nameof(points));
        Points += points;
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "UserEntityTests"
```

Expected: `Passed: 3`

- [ ] **Step 7: Commit**

```bash
git add src/Flow.Domain/ tests/Flow.Application.Tests/Auth/UserEntityTests.cs
git commit -m "feat(domain): add User entity with role and points"
```

---

## Task 3: Domain — RefreshToken and AuditLog entities

**Files:**
- Create: `src/Flow.Domain/Entities/RefreshToken.cs`
- Create: `src/Flow.Domain/Entities/AuditLog.cs`
- Create: `tests/Flow.Application.Tests/Auth/RefreshTokenEntityTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Flow.Application.Tests/Auth/RefreshTokenEntityTests.cs`:

```csharp
using FluentAssertions;
using Flow.Domain.Entities;
using Xunit;

namespace Flow.Application.Tests.Auth;

public class RefreshTokenEntityTests
{
    [Fact]
    public void Create_ValidInputs_IsActiveAndNotRevoked()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        var token = RefreshToken.Create(userId, "token-value", expiresAt);

        token.UserId.Should().Be(userId);
        token.Token.Should().Be("token-value");
        token.IsActive.Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsIsRevokedTrue_AndIsActiveFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token-value", DateTimeOffset.UtcNow.AddDays(7));

        token.Revoke();

        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ExpiredToken_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "token-value", DateTimeOffset.UtcNow.AddSeconds(-1));

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void AuditLog_Create_SetsAllFields()
    {
        var actorId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var log = AuditLog.Create("Idea", entityId, "Approved", actorId, "Maria", null, "approved", "Budget approved");

        log.EntityType.Should().Be("Idea");
        log.EntityId.Should().Be(entityId);
        log.Action.Should().Be("Approved");
        log.ActorId.Should().Be(actorId);
        log.ActorName.Should().Be("Maria");
        log.NewValue.Should().Be("approved");
        log.Reason.Should().Be("Budget approved");
        log.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "RefreshTokenEntityTests"
```

Expected: FAIL — types not found.

- [ ] **Step 3: Create RefreshToken entity**

Create `src/Flow.Domain/Entities/RefreshToken.cs`:

```csharp
namespace Flow.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }

    public bool IsActive => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, DateTimeOffset expiresAt)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        };
    }

    public void Revoke() => IsRevoked = true;
}
```

- [ ] **Step 4: Create AuditLog entity**

Create `src/Flow.Domain/Entities/AuditLog.cs`:

```csharp
namespace Flow.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public Guid ActorId { get; private set; }
    public string ActorName { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string entityType,
        Guid entityId,
        string action,
        Guid actorId,
        string actorName,
        string? oldValue = null,
        string? newValue = null,
        string? reason = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ActorId = actorId,
            ActorName = actorName,
            OldValue = oldValue,
            NewValue = newValue,
            Reason = reason,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "RefreshTokenEntityTests"
```

Expected: `Passed: 4`

- [ ] **Step 6: Commit**

```bash
git add src/Flow.Domain/Entities/ tests/Flow.Application.Tests/Auth/RefreshTokenEntityTests.cs
git commit -m "feat(domain): add RefreshToken and AuditLog entities"
```

---

## Task 4: Application — interfaces and exceptions

**Files:**
- Create: `src/Flow.Application/Common/Interfaces/IApplicationDbContext.cs`
- Create: `src/Flow.Application/Common/Interfaces/ICurrentUserService.cs`
- Create: `src/Flow.Application/Common/Interfaces/IJwtTokenService.cs`
- Create: `src/Flow.Application/Common/Interfaces/IRepository.cs`
- Create: `src/Flow.Application/Common/Exceptions/NotFoundException.cs`
- Create: `src/Flow.Application/Common/Exceptions/ForbiddenException.cs`
- Create: `src/Flow.Application/Common/Exceptions/ConflictException.cs`
- Create: `src/Flow.Application/Common/Exceptions/ValidationException.cs`
- Create: `src/Flow.Application/Auth/AuthResultDto.cs`
- Create: `src/Flow.Application/DependencyInjection.cs`

- [ ] **Step 1: Create IApplicationDbContext**

Create `src/Flow.Application/Common/Interfaces/IApplicationDbContext.cs`:

```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Atomically appends auditEntries and saves all pending changes in one transaction.
    // All state-changing domain operations must call this instead of SaveChangesAsync
    // when they produce audit log entries.
    Task<int> SaveChangesWithAuditAsync(
        IEnumerable<AuditLog> auditEntries,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Create ICurrentUserService**

Create `src/Flow.Application/Common/Interfaces/ICurrentUserService.cs`:

```csharp
namespace Flow.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
```

- [ ] **Step 3: Create IJwtTokenService**

Create `src/Flow.Application/Common/Interfaces/IJwtTokenService.cs`:

```csharp
using Flow.Domain.Entities;

namespace Flow.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IList<string> roles);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string accessToken);
}
```

- [ ] **Step 4: Create IRepository**

Create `src/Flow.Application/Common/Interfaces/IRepository.cs`:

```csharp
namespace Flow.Application.Common.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}
```

- [ ] **Step 5: Create exception types**

Create `src/Flow.Application/Common/Exceptions/NotFoundException.cs`:

```csharp
namespace Flow.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} '{key}' was not found.") { }
}
```

Create `src/Flow.Application/Common/Exceptions/ForbiddenException.cs`:

```csharp
namespace Flow.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Access denied.") : base(message) { }
}
```

Create `src/Flow.Application/Common/Exceptions/ConflictException.cs`:

```csharp
namespace Flow.Application.Common.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
```

Create `src/Flow.Application/Common/Exceptions/ValidationException.cs`:

```csharp
namespace Flow.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
```

- [ ] **Step 6: Create AuthResultDto**

Create `src/Flow.Application/Auth/AuthResultDto.cs`:

```csharp
namespace Flow.Application.Auth;

public record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Name,
    string Email,
    string Role
);
```

- [ ] **Step 7: Create prepared-only abstractions (no-op implementations deferred to Phase 2+)**

Create `src/Flow.Application/Common/Interfaces/ICacheService.cs`:

```csharp
namespace Flow.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
```

Create `src/Flow.Application/Common/Interfaces/IAuthProvider.cs`:

```csharp
namespace Flow.Application.Common.Interfaces;

// Seam for swapping email/password auth for Azure AD SSO.
// Currently implemented by ASP.NET Core Identity via RegisterCommandHandler.
// Replace this interface's registration in DI to wire Azure AD.
public interface IAuthProvider
{
    Task<bool> ValidateExternalTokenAsync(string token, CancellationToken cancellationToken = default);
}
```

Create `src/Flow.Application/Common/Interfaces/INotificationService.cs`:

```csharp
namespace Flow.Application.Common.Interfaces;

// No-op in MVP. Replace registration in DI to enable push/email notifications.
public interface INotificationService
{
    Task SendAsync(Guid userId, string title, string body, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 8: Create Application DependencyInjection**

Create `src/Flow.Application/DependencyInjection.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Flow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
```

- [ ] **Step 9: Verify build**

```bash
dotnet build src/Flow.Application/Flow.Application.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 10: Commit**

```bash
git add src/Flow.Application/
git commit -m "feat(application): add interfaces, exceptions, AuthResultDto, prepared abstractions, and DI setup"
```

---

## Task 5: Application — RegisterCommandHandler (TDD)

**Files:**
- Create: `src/Flow.Application/Auth/Commands/Register/RegisterCommand.cs`
- Create: `src/Flow.Application/Auth/Commands/Register/RegisterCommandHandler.cs`
- Create: `tests/Flow.Application.Tests/Auth/RegisterCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Flow.Application.Tests/Auth/RegisterCommandHandlerTests.cs`:

```csharp
using FluentAssertions;
using Flow.Application.Auth;
using Flow.Application.Auth.Commands.Register;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Flow.Application.Tests.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<DbSet<RefreshToken>> _refreshTokenDbSetMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _jwtServiceMock = new Mock<IJwtTokenService>();
        _contextMock = new Mock<IApplicationDbContext>();
        _refreshTokenDbSetMock = new Mock<DbSet<RefreshToken>>();

        _contextMock.Setup(c => c.RefreshTokens).Returns(_refreshTokenDbSetMock.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new RegisterCommandHandler(
            _userManagerMock.Object, _jwtServiceMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsAuthResultWithTokens()
    {
        var command = new RegisterCommand("Ana Lima", "ana@example.com", "Password123!");

        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), UserRole.Operator.ToString()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "Operator" });
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>()))
            .Returns("access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken())
            .Returns("refresh-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.Email.Should().Be("ana@example.com");
        result.Role.Should().Be("Operator");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        var command = new RegisterCommand("Ana Lima", "existing@example.com", "Password123!");
        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync(User.Create("Existing", "existing@example.com", UserRole.Operator));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_IdentityFailure_ThrowsValidationException()
    {
        var command = new RegisterCommand("Ana Lima", "ana@example.com", "weak");
        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Password too short." }));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "RegisterCommandHandlerTests"
```

Expected: FAIL — `RegisterCommandHandler` type not found.

- [ ] **Step 3: Create RegisterCommand**

Create `src/Flow.Application/Auth/Commands/Register/RegisterCommand.cs`:

```csharp
using Flow.Application.Auth;
using MediatR;

namespace Flow.Application.Auth.Commands.Register;

public record RegisterCommand(string Name, string Email, string Password) : IRequest<AuthResultDto>;
```

- [ ] **Step 4: Create RegisterCommandHandler**

Create `src/Flow.Application/Auth/Commands/Register/RegisterCommandHandler.cs`:

```csharp
using Flow.Application.Auth;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Flow.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResultDto>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IApplicationDbContext _context;

    public RegisterCommandHandler(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        IApplicationDbContext context)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
    }

    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new ConflictException($"A user with email '{request.Email}' already exists.");

        var user = User.Create(request.Name, request.Email, UserRole.Operator);
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            throw new ValidationException(errors);
        }

        await _userManager.AddToRoleAsync(user, UserRole.Operator.ToString());

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, DateTimeOffset.UtcNow.AddDays(7));

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            UserId: user.Id,
            Name: user.Name,
            Email: user.Email!,
            Role: user.Role.ToString()
        );
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "RegisterCommandHandlerTests"
```

Expected: `Passed: 3`

- [ ] **Step 6: Commit**

```bash
git add src/Flow.Application/Auth/Commands/Register/ tests/Flow.Application.Tests/Auth/RegisterCommandHandlerTests.cs
git commit -m "feat(application): add RegisterCommandHandler with tests"
```

---

## Task 6: Application — LoginCommandHandler (TDD)

**Files:**
- Create: `src/Flow.Application/Auth/Commands/Login/LoginCommand.cs`
- Create: `src/Flow.Application/Auth/Commands/Login/LoginCommandHandler.cs`
- Create: `tests/Flow.Application.Tests/Auth/LoginCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Flow.Application.Tests/Auth/LoginCommandHandlerTests.cs`:

```csharp
using FluentAssertions;
using Flow.Application.Auth.Commands.Login;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Flow.Application.Tests.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _jwtServiceMock = new Mock<IJwtTokenService>();
        _contextMock = new Mock<IApplicationDbContext>();

        _contextMock.Setup(c => c.RefreshTokens).Returns(new Mock<DbSet<RefreshToken>>().Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new LoginCommandHandler(_userManagerMock.Object, _jwtServiceMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResult()
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Manager);
        var command = new LoginCommand("ana@example.com", "Password123!");

        _userManagerMock.Setup(m => m.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, command.Password)).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Manager" });
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user, It.IsAny<IList<string>>())).Returns("access");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.Role.Should().Be("Manager");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new LoginCommand("unknown@example.com", "pass"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsForbiddenException()
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Operator);
        _userManagerMock.Setup(m => m.FindByEmailAsync("ana@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var act = () => _handler.Handle(new LoginCommand("ana@example.com", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "LoginCommandHandlerTests"
```

Expected: FAIL — `LoginCommandHandler` type not found.

- [ ] **Step 3: Create LoginCommand**

Create `src/Flow.Application/Auth/Commands/Login/LoginCommand.cs`:

```csharp
using Flow.Application.Auth;
using MediatR;

namespace Flow.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;
```

- [ ] **Step 4: Create LoginCommandHandler**

Create `src/Flow.Application/Auth/Commands/Login/LoginCommandHandler.cs`:

```csharp
using Flow.Application.Auth;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Flow.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IApplicationDbContext _context;

    public LoginCommandHandler(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        IApplicationDbContext context)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new NotFoundException(nameof(User), request.Email);

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid) throw new ForbiddenException("Invalid credentials.");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, DateTimeOffset.UtcNow.AddDays(7));

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            UserId: user.Id,
            Name: user.Name,
            Email: user.Email!,
            Role: user.Role.ToString()
        );
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "LoginCommandHandlerTests"
```

Expected: `Passed: 3`

- [ ] **Step 6: Commit**

```bash
git add src/Flow.Application/Auth/Commands/Login/ tests/Flow.Application.Tests/Auth/LoginCommandHandlerTests.cs
git commit -m "feat(application): add LoginCommandHandler with tests"
```

---

## Task 7: Application — RefreshTokenCommand and LogoutCommand

**Files:**
- Create: `src/Flow.Application/Auth/Commands/RefreshToken/RefreshTokenCommand.cs`
- Create: `src/Flow.Application/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs`
- Create: `src/Flow.Application/Auth/Commands/Logout/LogoutCommand.cs`
- Create: `src/Flow.Application/Auth/Commands/Logout/LogoutCommandHandler.cs`
- Create: `tests/Flow.Application.Tests/Auth/RefreshTokenCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Flow.Application.Tests/Auth/RefreshTokenCommandHandlerTests.cs`:

```csharp
using FluentAssertions;
using Flow.Application.Auth.Commands.RefreshToken;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Flow.Application.Tests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly Mock<IApplicationDbContext> _contextMock;

    public RefreshTokenCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _jwtServiceMock = new Mock<IJwtTokenService>();
        _contextMock = new Mock<IApplicationDbContext>();
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_InvalidAccessToken_ThrowsForbiddenException()
    {
        _jwtServiceMock.Setup(j => j.GetUserIdFromToken("bad-token")).Returns((Guid?)null);

        var handler = new RefreshTokenCommandHandler(
            _userManagerMock.Object, _jwtServiceMock.Object, _contextMock.Object);

        var act = () => handler.Handle(
            new RefreshTokenCommand("bad-token", "any-refresh"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_RevokedRefreshToken_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var refreshToken = RefreshToken.Create(userId, "revoked-token", DateTimeOffset.UtcNow.AddDays(7));
        refreshToken.Revoke();

        _jwtServiceMock.Setup(j => j.GetUserIdFromToken("access")).Returns(userId);

        var tokens = new List<RefreshToken> { refreshToken }.AsQueryable();
        var mockDbSet = new Mock<DbSet<RefreshToken>>();
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.Provider).Returns(tokens.Provider);
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.Expression).Returns(tokens.Expression);
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.ElementType).Returns(tokens.ElementType);
        mockDbSet.As<IQueryable<RefreshToken>>().Setup(m => m.GetEnumerator()).Returns(tokens.GetEnumerator());
        mockDbSet.As<IAsyncEnumerable<RefreshToken>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<RefreshToken>(tokens.GetEnumerator()));

        _contextMock.Setup(c => c.RefreshTokens).Returns(mockDbSet.Object);

        var handler = new RefreshTokenCommandHandler(
            _userManagerMock.Object, _jwtServiceMock.Object, _contextMock.Object);

        var act = () => handler.Handle(new RefreshTokenCommand("access", "revoked-token"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}

// Helper for async enumeration in tests
file class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "RefreshTokenCommandHandlerTests"
```

Expected: FAIL — type not found.

- [ ] **Step 3: Create RefreshTokenCommand**

Create `src/Flow.Application/Auth/Commands/RefreshToken/RefreshTokenCommand.cs`:

```csharp
using Flow.Application.Auth;
using MediatR;

namespace Flow.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResultDto>;
```

- [ ] **Step 4: Create RefreshTokenCommandHandler**

Create `src/Flow.Application/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs`:

```csharp
using Flow.Application.Auth;
using Flow.Application.Common.Exceptions;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IApplicationDbContext _context;

    public RefreshTokenCommandHandler(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        IApplicationDbContext context)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _jwtTokenService.GetUserIdFromToken(request.AccessToken)
            ?? throw new ForbiddenException("Invalid access token.");

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId, cancellationToken)
            ?? throw new ForbiddenException("Refresh token not found.");

        if (!storedToken.IsActive)
            throw new ForbiddenException("Refresh token is expired or revoked.");

        storedToken.Revoke();

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException(nameof(User), userId);

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var newRefreshValue = _jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = RefreshToken.Create(user.Id, newRefreshValue, DateTimeOffset.UtcNow.AddDays(7));

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshValue,
            UserId: user.Id,
            Name: user.Name,
            Email: user.Email!,
            Role: user.Role.ToString()
        );
    }
}
```

- [ ] **Step 5: Create LogoutCommand and handler**

Create `src/Flow.Application/Auth/Commands/Logout/LogoutCommand.cs`:

```csharp
using MediatR;

namespace Flow.Application.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;
```

Create `src/Flow.Application/Auth/Commands/Logout/LogoutCommandHandler.cs`:

```csharp
using Flow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Flow.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public LogoutCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(
                rt => rt.Token == request.RefreshToken && rt.UserId == _currentUser.UserId,
                cancellationToken);

        if (token is not null && token.IsActive)
        {
            token.Revoke();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "RefreshTokenCommandHandlerTests"
```

Expected: `Passed: 2`

- [ ] **Step 7: Commit**

```bash
git add src/Flow.Application/Auth/Commands/RefreshToken/ src/Flow.Application/Auth/Commands/Logout/ tests/Flow.Application.Tests/Auth/RefreshTokenCommandHandlerTests.cs
git commit -m "feat(application): add RefreshTokenCommand and LogoutCommand handlers"
```

---

## Task 8: Infrastructure — ApplicationDbContext and entity configurations

**Files:**
- Create: `src/Flow.Infrastructure/Persistence/ApplicationDbContext.cs`
- Create: `src/Flow.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- Create: `src/Flow.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- Create: `src/Flow.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs`

- [ ] **Step 1: Create ApplicationDbContext**

Create `src/Flow.Infrastructure/Persistence/ApplicationDbContext.cs`:

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

- [ ] **Step 2: Create UserConfiguration**

Create `src/Flow.Infrastructure/Persistence/Configurations/UserConfiguration.cs`:

```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.Points)
            .IsRequired()
            .HasDefaultValue(0);
    }
}
```

- [ ] **Step 3: Create RefreshTokenConfiguration**

Create `src/Flow.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`:

```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(rt => rt.Token).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 4: Create AuditLogConfiguration**

Create `src/Flow.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs`:

```csharp
using Flow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flow.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.ActorName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.OldValue).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValue).HasColumnType("nvarchar(max)");
        builder.Property(a => a.Reason).HasMaxLength(1000);

        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.Timestamp);
    }
}
```

- [ ] **Step 5: Verify build**

```bash
dotnet build src/Flow.Infrastructure/Flow.Infrastructure.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add src/Flow.Infrastructure/Persistence/
git commit -m "feat(infrastructure): add ApplicationDbContext with Identity and entity configurations"
```

---

## Task 9: Infrastructure — JwtTokenService (TDD)

**Files:**
- Create: `src/Flow.Infrastructure/Auth/JwtTokenService.cs`
- Create: `tests/Flow.Application.Tests/Auth/JwtTokenServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Flow.Application.Tests/Auth/JwtTokenServiceTests.cs`:

```csharp
using FluentAssertions;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Flow.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Flow.Application.Tests.Auth;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "super-secret-key-at-least-256-bits-long-for-testing-purposes",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:ExpiryMinutes"] = "15"
            })
            .Build();

        _service = new JwtTokenService(config);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsNonEmptyJwt()
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Manager);

        var token = _service.GenerateAccessToken(user, new List<string> { "Manager" });

        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts
    }

    [Fact]
    public void GetUserIdFromToken_ValidToken_ReturnsCorrectUserId()
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Manager);
        var token = _service.GenerateAccessToken(user, new List<string> { "Manager" });

        var extractedId = _service.GetUserIdFromToken(token);

        extractedId.Should().Be(user.Id);
    }

    [Fact]
    public void GetUserIdFromToken_InvalidToken_ReturnsNull()
    {
        var result = _service.GetUserIdFromToken("not.a.valid.token");

        result.Should().BeNull();
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueTokens()
    {
        var token1 = _service.GenerateRefreshToken();
        var token2 = _service.GenerateRefreshToken();

        token1.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "JwtTokenServiceTests"
```

Expected: FAIL — `JwtTokenService` type not found.

- [ ] **Step 3: Create JwtTokenService**

Create `src/Flow.Infrastructure/Auth/JwtTokenService.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Flow.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user, IList<string> roles)
    {
        var settings = _configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = int.Parse(settings["ExpiryMinutes"] ?? "15");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: settings["Issuer"],
            audience: settings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public Guid? GetUserIdFromToken(string accessToken)
    {
        var settings = _configuration.GetSection("JwtSettings");
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings["SecretKey"]!)),
            ValidateIssuer = true,
            ValidIssuer = settings["Issuer"],
            ValidateAudience = true,
            ValidAudience = settings["Audience"],
            ValidateLifetime = false
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(accessToken, validationParams, out _);
            var claim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/Flow.Application.Tests/Flow.Application.Tests.csproj --filter "JwtTokenServiceTests"
```

Expected: `Passed: 4`

- [ ] **Step 5: Commit**

```bash
git add src/Flow.Infrastructure/Auth/JwtTokenService.cs tests/Flow.Application.Tests/Auth/JwtTokenServiceTests.cs
git commit -m "feat(infrastructure): add JwtTokenService with tests"
```

---

## Task 10: Infrastructure — BaseRepository and DependencyInjection

**Files:**
- Create: `src/Flow.Infrastructure/Persistence/Repositories/BaseRepository.cs`
- Create: `src/Flow.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Create BaseRepository**

Create `src/Flow.Infrastructure/Persistence/Repositories/BaseRepository.cs`:

```csharp
using Flow.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flow.Infrastructure.Persistence.Repositories;

public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected BaseRepository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.ToListAsync(cancellationToken);

    public void Add(T entity) => DbSet.Add(entity);

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);
}
```

- [ ] **Step 2: Create Infrastructure DependencyInjection**

Create `src/Flow.Infrastructure/DependencyInjection.cs`:

```csharp
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Infrastructure.Auth;
using Flow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build src/Flow.Infrastructure/Flow.Infrastructure.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add src/Flow.Infrastructure/
git commit -m "feat(infrastructure): add BaseRepository and Infrastructure DI registration"
```

---

## Task 11: API — ExceptionHandlingMiddleware (TDD)

**Files:**
- Create: `src/Flow.API/Middleware/ExceptionHandlingMiddleware.cs`
- Create: `tests/Flow.API.Tests/Helpers/FlowWebApplicationFactory.cs`
- Create: `tests/Flow.API.Tests/Auth/ExceptionMiddlewareTests.cs`

- [ ] **Step 1: Create ExceptionHandlingMiddleware**

Create `src/Flow.API/Middleware/ExceptionHandlingMiddleware.cs`:

```csharp
using System.Net;
using System.Text.Json;
using Flow.Application.Common.Exceptions;

namespace Flow.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
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
            _ =>
                (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (object?)null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception.");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var body = JsonSerializer.Serialize(
            new { title, status = (int)statusCode, errors },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(body);
    }
}
```

- [ ] **Step 2: Create FlowWebApplicationFactory**

Create `tests/Flow.API.Tests/Helpers/FlowWebApplicationFactory.cs`:

```csharp
using Flow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.API.Tests.Helpers;

public class FlowWebApplicationFactory : WebApplicationFactory<Program>
{
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
                options.UseInMemoryDatabase("TestDb-" + Guid.NewGuid()));
        });
    }
}
```

- [ ] **Step 3: Write exception middleware integration tests**

Create `tests/Flow.API.Tests/Auth/ExceptionMiddlewareTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Xunit;

namespace Flow.API.Tests.Auth;

public class ExceptionMiddlewareTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ExceptionMiddlewareTests(FlowWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns403WithStructuredBody()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "nobody@example.com", password = "Password123!" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be(403);
        body.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var payload = new { name = "Test", email = "dup@example.com", password = "Password123!" };
        await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

```bash
dotnet test tests/Flow.API.Tests/Flow.API.Tests.csproj --filter "ExceptionMiddlewareTests"
```

Expected: FAIL — `Program` class not found or middleware not wired yet.

- [ ] **Step 5: Commit middleware (tests wired in Task 15 after Program.cs)**

```bash
git add src/Flow.API/Middleware/ tests/Flow.API.Tests/Helpers/ tests/Flow.API.Tests/Auth/ExceptionMiddlewareTests.cs
git commit -m "feat(api): add ExceptionHandlingMiddleware and test factory scaffold"
```

---

## Task 12: API — CurrentUserService and AuthController

**Files:**
- Create: `src/Flow.API/Services/CurrentUserService.cs`
- Create: `src/Flow.API/Controllers/AuthController.cs`

- [ ] **Step 1: Create CurrentUserService**

Create `src/Flow.API/Services/CurrentUserService.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Flow.Application.Common.Interfaces;

namespace Flow.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User
                .FindFirst(JwtRegisteredClaimNames.Sub)
                ?? _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier);

            return Guid.TryParse(claim?.Value, out var id) ? id : null;
        }
    }

    public string? UserName =>
        _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value
        ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
```

- [ ] **Step 2: Create AuthController**

Create `src/Flow.API/Controllers/AuthController.cs`:

```csharp
using Flow.Application.Auth;
using Flow.Application.Auth.Commands.Login;
using Flow.Application.Auth.Commands.Logout;
using Flow.Application.Auth.Commands.RefreshToken;
using Flow.Application.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Register(
        [FromBody] RegisterCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Login(
        [FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResultDto>> Refresh(
        [FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Flow.API/Services/ src/Flow.API/Controllers/
git commit -m "feat(api): add CurrentUserService and AuthController"
```

---

## Task 13: API — Program.cs and appsettings

**Files:**
- Modify: `src/Flow.API/Program.cs`
- Create: `src/Flow.API/appsettings.json`
- Create: `src/Flow.API/appsettings.Development.json`

- [ ] **Step 1: Write Program.cs**

Replace the contents of `src/Flow.API/Program.cs`:

```csharp
using System.Text;
using Flow.API.Middleware;
using Flow.API.Services;
using Flow.Application;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Infrastructure;
using Flow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Flow API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in new[] { "Operator", "Manager", "Leadership" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }
}

app.Run();

public partial class Program { }
```

- [ ] **Step 2: Write appsettings.json**

Replace `src/Flow.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FlowDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "JwtSettings": {
    "SecretKey": "CHANGE-THIS-IN-PRODUCTION-must-be-at-least-32-chars",
    "Issuer": "FlowAPI",
    "Audience": "FlowApp",
    "ExpiryMinutes": "15"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 3: Write appsettings.Development.json**

Replace `src/Flow.API/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

- [ ] **Step 4: Verify the API builds**

```bash
dotnet build src/Flow.API/Flow.API.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add src/Flow.API/Program.cs src/Flow.API/appsettings.json src/Flow.API/appsettings.Development.json
git commit -m "feat(api): wire Program.cs with DI, JWT, middleware pipeline, and role seeding"
```

---

## Task 14: Database — EF Core migration and smoke test

**Files:**
- Create: `src/Flow.Infrastructure/Persistence/Migrations/` (auto-generated)

- [ ] **Step 1: Install EF Core CLI tools (if not present)**

```bash
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
```

- [ ] **Step 2: Create the initial migration**

Run from the solution root:

```bash
dotnet ef migrations add InitialCreate `
  --project src/Flow.Infrastructure/Flow.Infrastructure.csproj `
  --startup-project src/Flow.API/Flow.API.csproj `
  --output-dir Persistence/Migrations
```

Expected: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 3: Verify migration was generated**

```bash
Get-ChildItem src/Flow.Infrastructure/Persistence/Migrations/
```

Expected: Three files — `*_InitialCreate.cs`, `*_InitialCreate.Designer.cs`, `ApplicationDbContextModelSnapshot.cs`

- [ ] **Step 4: Run all tests**

```bash
dotnet test
```

Expected: All tests pass. Any `ExceptionMiddlewareTests` failures should now resolve since `Program` is defined.

- [ ] **Step 5: Start the API and verify it launches**

```bash
dotnet run --project src/Flow.API/Flow.API.csproj
```

Expected output:
```
info: Microsoft.EntityFrameworkCore.Database.Command[...] Applied migration 'InitialCreate'.
info: Microsoft.Hosting.Lifetime[14] Now listening on: https://localhost:PORT
```

Open `https://localhost:{PORT}/swagger` in a browser. The Swagger UI should load with four auth endpoints visible: `POST /api/v1/auth/register`, `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout`.

- [ ] **Step 6: Smoke test register via Swagger**

In the Swagger UI, execute `POST /api/v1/auth/register` with:

```json
{
  "name": "Test Operator",
  "email": "operator@example.com",
  "password": "Password123!"
}
```

Expected: `200 OK` with body containing `accessToken`, `refreshToken`, `userId`, `name`, `email`, `role: "Operator"`.

- [ ] **Step 7: Smoke test login via Swagger**

Execute `POST /api/v1/auth/login` with:

```json
{
  "email": "operator@example.com",
  "password": "Password123!"
}
```

Expected: `200 OK` with fresh token pair.

- [ ] **Step 8: Stop the API and run final test suite**

```bash
dotnet test --logger "console;verbosity=normal"
```

Expected: All tests pass, zero failures.

- [ ] **Step 9: Commit**

```bash
git add src/Flow.Infrastructure/Persistence/Migrations/
git commit -m "feat(infrastructure): add initial EF Core migration for full domain schema"
```

---

## Task 15: Integration tests — AuthController end-to-end

**Files:**
- Create: `tests/Flow.API.Tests/Auth/AuthControllerTests.cs`

- [ ] **Step 1: Write the integration tests**

Create `tests/Flow.API.Tests/Auth/AuthControllerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Xunit;

namespace Flow.API.Tests.Auth;

public class AuthControllerTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(FlowWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidPayload_Returns200WithTokenPair()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name = "Ana Lima",
            email = "ana@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("role").GetString().Should().Be("Operator");
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokenPair()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name = "Login User",
            email = "loginuser@example.com",
            password = "Password123!"
        });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "loginuser@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns403()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name = "Wrong Pass",
            email = "wrongpass@example.com",
            password = "Password123!"
        });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "wrongpass@example.com",
            password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Refresh_ValidTokenPair_Returns200WithNewTokens()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name = "Refresh User",
            email = "refreshuser@example.com",
            password = "Password123!"
        });

        var tokens = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokens.GetProperty("accessToken").GetString()!;
        var refreshToken = tokens.GetProperty("refreshToken").GetString()!;

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            accessToken,
            refreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newTokens = await response.Content.ReadFromJsonAsync<JsonElement>();
        newTokens.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Logout_AuthenticatedUser_Returns204()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name = "Logout User",
            email = "logoutuser@example.com",
            password = "Password123!"
        });

        var tokens = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokens.GetProperty("accessToken").GetString()!;
        var refreshToken = tokens.GetProperty("refreshToken").GetString()!;

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test tests/Flow.API.Tests/Flow.API.Tests.csproj --logger "console;verbosity=normal"
```

Expected: `Passed: 5` (plus `ExceptionMiddlewareTests` passing too — total 7 in this project).

- [ ] **Step 3: Run full test suite**

```bash
dotnet test --logger "console;verbosity=normal"
```

Expected: All tests pass across both test projects.

- [ ] **Step 4: Final commit**

```bash
git add tests/Flow.API.Tests/Auth/AuthControllerTests.cs
git commit -m "test(api): add end-to-end integration tests for auth endpoints"
```

---

## Phase 1 Complete

At this point the following is delivered and verified:

| Deliverable | Status |
|---|---|
| Clean Architecture solution structure | ✅ |
| Domain entities: User, RefreshToken, AuditLog | ✅ |
| Application interfaces and exception types | ✅ |
| MediatR command handlers: Register, Login, Refresh, Logout | ✅ |
| JWT access + refresh token service | ✅ |
| ApplicationDbContext with Identity and entity configurations | ✅ |
| `SaveChangesWithAuditAsync` for atomic audit log writes | ✅ |
| BaseRepository pattern | ✅ |
| Role-based DI registration (Operator, Manager, Leadership) | ✅ |
| Exception handling middleware with structured error responses | ✅ |
| Initial EF Core migration | ✅ |
| Role seeding on startup | ✅ |
| Swagger UI with JWT auth | ✅ |
| Unit tests: User, RefreshToken, AuditLog, Handlers, JwtTokenService | ✅ |
| Integration tests: all four auth endpoints | ✅ |

**Next:** Phase 2 — Ideas module, Projects module, state machine, AuditLog writes, ProjectSnapshots.
