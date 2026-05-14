using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Flow.API.Tests.Helpers;
using Xunit;

namespace Flow.API.Tests.Guidelines;

public class GuidelinesControllerTests : IClassFixture<FlowWebApplicationFactory>
{
    private readonly FlowWebApplicationFactory _factory;

    public GuidelinesControllerTests(FlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetGuidelines_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/guidelines");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateGuideline_AsLeadership_Returns201WithGuideline()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Leadership");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/guidelines", new
        {
            title = "Operational Excellence",
            description = "Focus on reducing waste in core processes."
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("title").GetString().Should().Be("Operational Excellence");
    }

    [Fact]
    public async Task CreateGuideline_AsOperator_Returns403()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/guidelines", new
        {
            title = "Some Guideline",
            description = "Desc"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetGuidelines_AsOperator_Returns200WithList()
    {
        var client = _factory.CreateClient();

        // Create a guideline as Leadership first
        var leaderToken = await _factory.GetTokenForRoleAsync("Leadership");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leaderToken);
        await client.PostAsJsonAsync("/api/v1/guidelines", new
        {
            title = "Digital Transformation",
            description = "Accelerate digital adoption."
        });

        // Read as Operator
        var operatorToken = await _factory.GetTokenForRoleAsync("Operator");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);
        var response = await client.GetAsync("/api/v1/guidelines");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateGuideline_AsLeadership_Returns204()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Leadership");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/v1/guidelines", new
        {
            title = "Original Title",
            description = "Original description."
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/guidelines/{id}", new
        {
            title = "Updated Title",
            description = "Updated description."
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteGuideline_AsLeadership_Returns204()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetTokenForRoleAsync("Leadership");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/v1/guidelines", new
        {
            title = "To Be Deleted",
            description = "This will be removed."
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        var deleteResponse = await client.DeleteAsync($"/api/v1/guidelines/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
