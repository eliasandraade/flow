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
        user.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AddPoints_AccumulatesAcrossMultipleCalls()
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Operator);

        user.AddPoints(30);
        user.AddPoints(20);

        user.Points.Should().Be(50);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void AddPoints_ZeroOrNegative_ThrowsArgumentException(int invalidPoints)
    {
        var user = User.Create("Ana Lima", "ana@example.com", UserRole.Operator);

        var act = () => user.AddPoints(invalidPoints);

        act.Should().Throw<ArgumentException>();
    }
}
