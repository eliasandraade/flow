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
