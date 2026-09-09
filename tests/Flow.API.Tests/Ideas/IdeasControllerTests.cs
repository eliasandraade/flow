using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Xunit;

namespace Flow.API.Tests.Ideas;

public class IdeasControllerTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly FlowWebApplicationFactory _factory;

    public IdeasControllerTests(FlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateIdea_AsOperator_Returns201()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "Automate expense reports",
            description = "Use AI to auto-fill expense forms.",
            problem = "Finance team spends 3 hours/week on manual expense processing.",
            linkedGuidelineId = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("status").GetString().Should().Be("Draft");
    }

    [Fact]
    public async Task GetIdeas_AsOperator_ReturnsOnlyOwnIdeas()
    {
        var client = _factory.CreateClient();
        var operatorToken = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);

        await client.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "My Idea",
            description = "Desc",
            problem = "Problem",
            linkedGuidelineId = (string?)null
        });

        var response = await client.GetAsync("/api/v1/ideas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SubmitIdea_AsOwner_Returns204()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "Submit Test",
            description = "Desc",
            problem = "Problem",
            linkedGuidelineId = (string?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var response = await client.PostAsync($"/api/v1/ideas/{id}/submit", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ApproveIdea_AsManager_Returns204AndIdeasShowApproved()
    {
        var operatorClient = _factory.CreateClient();
        var operatorToken = await _factory.GetTokenForRoleAsync("Operator");
        operatorClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", operatorToken);

        var createResponse = await operatorClient.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "Approvable Idea",
            description = "Desc",
            problem = "Problem",
            linkedGuidelineId = (string?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;
        await operatorClient.PostAsync($"/api/v1/ideas/{id}/submit", null);

        var managerClient = _factory.CreateClient();
        var managerToken = await _factory.GetTokenForRoleAsync("Manager");
        managerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", managerToken);

        var response = await managerClient.PostAsJsonAsync(
            $"/api/v1/ideas/{id}/approve",
            new { managerComment = "Great idea!" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await managerClient.GetAsync($"/api/v1/ideas/{id}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
        detail.GetProperty("status").GetString().Should().Be("Approved");
    }

    [Fact]
    public async Task RejectIdea_AsManager_Returns204()
    {
        var operatorClient = _factory.CreateClient();
        var operatorToken = await _factory.GetTokenForRoleAsync("Operator");
        operatorClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", operatorToken);

        var createResponse = await operatorClient.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "Rejectable Idea",
            description = "Desc",
            problem = "Problem",
            linkedGuidelineId = (string?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;
        await operatorClient.PostAsync($"/api/v1/ideas/{id}/submit", null);

        var managerClient = _factory.CreateClient();
        var managerToken = await _factory.GetTokenForRoleAsync("Manager");
        managerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", managerToken);

        var response = await managerClient.PostAsJsonAsync(
            $"/api/v1/ideas/{id}/reject",
            new { managerComment = "Not aligned with strategy." });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
