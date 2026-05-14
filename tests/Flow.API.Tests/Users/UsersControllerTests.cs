using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
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
        body.GetProperty("points").GetInt32().Should().Be(0);
    }
}
