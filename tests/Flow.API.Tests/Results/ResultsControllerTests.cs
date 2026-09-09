using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flow.API.Tests.Results;

public class ResultsControllerTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly FlowWebApplicationFactory _factory;

    public ResultsControllerTests(FlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, Guid ProjectId)> CreateProjectAsManagerAsync()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Manager");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ownerId = await GetManagerIdAsync();
        var resp = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            title = "ROI Test Project",
            description = "For result tests",
            priority = "Medium",
            ownerId,
            estimatedCost = (decimal?)null,
            deadline = (DateTimeOffset?)null
        });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var id = Guid.Parse(body.GetProperty("id").GetString()!);
        return (client, id);
    }

    [Fact]
    public async Task GetResult_NoResultExists_Returns404()
    {
        var (client, projectId) = await CreateProjectAsManagerAsync();

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/result");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutResult_WithEstimated_Returns200WithComputedROI()
    {
        var (client, projectId) = await CreateProjectAsManagerAsync();

        var response = await client.PutAsJsonAsync($"/api/v1/projects/{projectId}/result", new
        {
            estimatedRevenue = 100_000m,
            estimatedSavings = 20_000m,
            estimatedCost = 40_000m,
            actualRevenue = (decimal?)null,
            actualSavings = (decimal?)null,
            actualCost = (decimal?)null,
            paybackPeriodMonths = (int?)null,
            notes = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // ROI = (100000 + 20000 - 40000) / 40000 * 100 = 200
        body.GetProperty("estimatedROI").GetDecimal().Should().Be(200m);
        body.GetProperty("actualROI").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task PutResult_UpdateActual_DoesNotClearEstimated()
    {
        var (client, projectId) = await CreateProjectAsManagerAsync();

        // First: set estimated
        await client.PutAsJsonAsync($"/api/v1/projects/{projectId}/result", new
        {
            estimatedRevenue = 100_000m,
            estimatedSavings = 0m,
            estimatedCost = 50_000m,
            actualRevenue = (decimal?)null,
            actualSavings = (decimal?)null,
            actualCost = (decimal?)null,
            paybackPeriodMonths = (int?)null,
            notes = (string?)null
        });

        // Then: set actual only
        var response = await client.PutAsJsonAsync($"/api/v1/projects/{projectId}/result", new
        {
            estimatedRevenue = (decimal?)null,
            estimatedSavings = (decimal?)null,
            estimatedCost = (decimal?)null,
            actualRevenue = 80_000m,
            actualSavings = 5_000m,
            actualCost = 40_000m,
            paybackPeriodMonths = 12,
            notes = "On track"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Estimated group must still be present: (100000+0-50000)/50000*100 = 100
        body.GetProperty("estimatedROI").GetDecimal().Should().Be(100m);
        body.GetProperty("actualROI").GetDecimal().Should().NotBe(0m);
        body.GetProperty("notes").GetString().Should().Be("On track");
    }

    [Fact]
    public async Task GetResult_AfterPut_ReturnsStoredValues()
    {
        var (client, projectId) = await CreateProjectAsManagerAsync();

        await client.PutAsJsonAsync($"/api/v1/projects/{projectId}/result", new
        {
            estimatedRevenue = 50_000m,
            estimatedSavings = 10_000m,
            estimatedCost = 20_000m,
            actualRevenue = (decimal?)null,
            actualSavings = (decimal?)null,
            actualCost = (decimal?)null,
            paybackPeriodMonths = (int?)null,
            notes = "Initial estimate"
        });

        var getResponse = await client.GetAsync($"/api/v1/projects/{projectId}/result");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("estimatedRevenue").GetDecimal().Should().Be(50_000m);
        body.GetProperty("notes").GetString().Should().Be("Initial estimate");
    }

    private async Task<string> GetManagerIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Flow.Domain.Entities.User>>();
        var email = $"result-owner-{Guid.NewGuid():N}@flow.test";
        var user = Flow.Domain.Entities.User.Create("Result Owner", email, Flow.Domain.Enums.UserRole.Manager);
        await userManager.CreateAsync(user, "Test123!");
        await userManager.AddToRoleAsync(user, "Manager");
        return user.Id.ToString();
    }
}
