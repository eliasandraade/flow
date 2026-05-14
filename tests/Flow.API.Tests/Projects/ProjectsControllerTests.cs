using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flow.API.Tests.Projects;

public class ProjectsControllerTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly FlowWebApplicationFactory _factory;

    public ProjectsControllerTests(FlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, string ManagerToken)> CreateManagerClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Manager");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token);
    }

    [Fact]
    public async Task CreateProject_AsManager_Returns201WithPlannedStatus()
    {
        var (client, _) = await CreateManagerClientAsync();
        var ownerId = await GetManagerIdAsync();

        var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "Automate Onboarding",
            description = "Reduce onboarding time by 50%.",
            priority = "High",
            ownerId,
            estimatedCost = 15000.00m,
            deadline = DateTimeOffset.UtcNow.AddMonths(3)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Planned");
        body.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task StartProject_AsManager_Returns204AndStatusIsInProgress()
    {
        var (client, _) = await CreateManagerClientAsync();
        var ownerId = await GetManagerIdAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "Project To Start",
            description = "Desc",
            priority = "Medium",
            ownerId,
            estimatedCost = (decimal?)null,
            deadline = (DateTimeOffset?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var startResponse = await client.PostAsync($"/api/v1/projects/{id}/start", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await client.GetAsync($"/api/v1/projects/{id}");
        var body = await detail.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("InProgress");
    }

    [Fact]
    public async Task BlockProject_AsManager_Returns204WithBlockedReason()
    {
        var (client, _) = await CreateManagerClientAsync();
        var ownerId = await GetManagerIdAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "Project To Block",
            description = "Desc",
            priority = "Low",
            ownerId,
            estimatedCost = (decimal?)null,
            deadline = (DateTimeOffset?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var blockResponse = await client.PostAsJsonAsync(
            $"/api/v1/projects/{id}/block",
            new { reason = "Awaiting board approval." });

        blockResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await client.GetAsync($"/api/v1/projects/{id}");
        var body = await detail.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Blocked");
        body.GetProperty("blockedReason").GetString().Should().Be("Awaiting board approval.");
    }

    [Fact]
    public async Task GetProjectTimeline_AfterTransitions_ReturnsAuditEntries()
    {
        var (client, _) = await CreateManagerClientAsync();
        var ownerId = await GetManagerIdAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "Timeline Project",
            description = "Desc",
            priority = "Medium",
            ownerId,
            estimatedCost = (decimal?)null,
            deadline = (DateTimeOffset?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        await client.PostAsync($"/api/v1/projects/{id}/start", null);

        var timelineResponse = await client.GetAsync($"/api/v1/projects/{id}/timeline");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var timeline = await timelineResponse.Content.ReadFromJsonAsync<JsonElement>();
        timeline.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetProjectSnapshots_AfterTransitions_ReturnsSnapshots()
    {
        var (client, _) = await CreateManagerClientAsync();
        var ownerId = await GetManagerIdAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "Snapshot Project",
            description = "Desc",
            priority = "High",
            ownerId,
            estimatedCost = (decimal?)null,
            deadline = (DateTimeOffset?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        await client.PostAsync($"/api/v1/projects/{id}/start", null);

        var snapshotsResponse = await client.GetAsync($"/api/v1/projects/{id}/snapshots");
        snapshotsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var snapshots = await snapshotsResponse.Content.ReadFromJsonAsync<JsonElement>();
        snapshots.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ConvertIdeaToProject_ApprovedIdea_Returns201()
    {
        var operatorClient = _factory.CreateClient();
        var operatorToken = await _factory.GetTokenForRoleAsync("Operator");
        operatorClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", operatorToken);

        var ideaResponse = await operatorClient.PostAsJsonAsync("/api/v1/ideas", new
        {
            title = "Convertible Idea",
            description = "To be converted",
            problem = "Problem",
            linkedGuidelineId = (string?)null
        });
        var idea = await ideaResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ideaId = idea.GetProperty("id").GetString()!;
        await operatorClient.PostAsync($"/api/v1/ideas/{ideaId}/submit", null);

        var (managerClient, _) = await CreateManagerClientAsync();
        await managerClient.PostAsJsonAsync($"/api/v1/ideas/{ideaId}/approve",
            new { managerComment = (string?)null });

        var ownerId = await GetManagerIdAsync();
        var convertResponse = await managerClient.PostAsJsonAsync(
            $"/api/v1/ideas/{ideaId}/convert",
            new
            {
                title = "Project from Idea",
                description = "Desc",
                priority = "Medium",
                ownerId,
                estimatedCost = (decimal?)null,
                deadline = (DateTimeOffset?)null
            });

        convertResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = await convertResponse.Content.ReadFromJsonAsync<JsonElement>();
        project.GetProperty("status").GetString().Should().Be("Planned");
    }

    private async Task<string> GetManagerIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Flow.Domain.Entities.User>>();
        var email = $"owner-{Guid.NewGuid():N}@flow.test";
        var user = Flow.Domain.Entities.User.Create("Project Owner", email, Flow.Domain.Enums.UserRole.Manager);
        await userManager.CreateAsync(user, "Test123!");
        await userManager.AddToRoleAsync(user, "Manager");
        return user.Id.ToString();
    }
}
