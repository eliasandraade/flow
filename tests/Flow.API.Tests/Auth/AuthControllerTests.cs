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
