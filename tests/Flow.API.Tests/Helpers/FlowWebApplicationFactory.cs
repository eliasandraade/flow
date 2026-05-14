using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Domain.Enums;
using Flow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.API.Tests.Helpers;

public class FlowWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb-" + Guid.NewGuid();
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-key-must-be-at-least-256-bits-long-for-hmac",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:ExpiryMinutes"] = "15"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName, _dbRoot));

            // Register the test-only controller from this assembly so the
            // ExceptionHandlingMiddleware in Program.cs can catch exceptions thrown by it.
            services.AddControllers()
                .AddApplicationPart(typeof(FlowWebApplicationFactory).Assembly);
        });
    }

    public async Task<string> GetTokenForRoleAsync(string role)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var email = $"test-{role.ToLower()}-{Guid.NewGuid():N}@flow.test";
        var userRole = Enum.Parse<UserRole>(role);
        var user = User.Create($"Test {role}", email, userRole);

        await userManager.CreateAsync(user, "Test123!");
        await userManager.AddToRoleAsync(user, role);

        var roles = await userManager.GetRolesAsync(user);
        return jwtService.GenerateAccessToken(user, roles);
    }
}
