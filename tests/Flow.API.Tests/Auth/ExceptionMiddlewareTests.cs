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
