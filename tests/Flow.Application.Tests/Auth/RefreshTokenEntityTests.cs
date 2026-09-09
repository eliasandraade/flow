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
        token.Id.Should().NotBeEmpty();
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
        log.OldValue.Should().BeNull();
        log.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        log.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void AuditLog_Create_WithoutReason_ReasonIsNull()
    {
        var log = AuditLog.Create("Project", Guid.NewGuid(), "Started", Guid.NewGuid(), "Carlos");

        log.Reason.Should().BeNull();
        log.OldValue.Should().BeNull();
        log.NewValue.Should().BeNull();
    }
}
