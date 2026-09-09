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
