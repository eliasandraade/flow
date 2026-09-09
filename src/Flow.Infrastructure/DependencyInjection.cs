using Flow.Application.Common.Persistence;
using Flow.Application.Common.Interfaces;
using Flow.Domain.Entities;
using Flow.Infrastructure.Auth;
using Flow.Infrastructure.Identity;
using Flow.Infrastructure.Observability;
using Flow.Infrastructure.Persistence.Mongo;
using Flow.Infrastructure.Persistence.Mongo.Repositories;
using Flow.Infrastructure.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace Flow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMongoPersistence(configuration);
        services.AddIdentityStores();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ICorrelationIdAccessor, ActivityCorrelationIdAccessor>();

        services.AddSingleton(sp =>
        {
            var options = configuration.GetSection(DemoSeedOptions.SectionName).Get<DemoSeedOptions>()
                ?? new DemoSeedOptions();

            // Environment variable wins so that a demo environment can set the password
            // without it ever living in a committed file.
            var fromEnvironment = Environment.GetEnvironmentVariable("SEED_DEMO_PASSWORD");
            if (!string.IsNullOrWhiteSpace(fromEnvironment)) options.Password = fromEnvironment;

            return options;
        });

        services.AddScoped<DemoDataSeeder>();

        return services;
    }

    private static IServiceCollection AddMongoPersistence(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Serializer registration is global in the driver and must happen before the first
        // serialisation, so it runs here rather than lazily on first use.
        MongoMapping.Register();

        services.Configure<MongoOptions>(configuration.GetSection(MongoOptions.SectionName));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = configuration.GetSection(MongoOptions.SectionName).Get<MongoOptions>()
                ?? new MongoOptions();

            var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);

            // Emits ActivitySource spans that OpenTelemetry picks up, so database calls
            // appear inside the request trace instead of as unexplained latency.
            settings.ClusterConfigurator = cb =>
                cb.Subscribe(new DiagnosticsActivityEventSubscriber(
                    new InstrumentationOptions { CaptureCommandText = false }));

            return new MongoClient(settings);
        });

        services.AddSingleton(sp =>
        {
            var options = configuration.GetSection(MongoOptions.SectionName).Get<MongoOptions>()
                ?? new MongoOptions();

            return new FlowMongoContext(sp.GetRequiredService<IMongoClient>(), options.Database);
        });

        services.AddSingleton<MongoIndexInitializer>();

        // The transaction session is per request scope; the client and context are not.
        services.AddScoped<MongoSessionAccessor>();
        services.AddScoped<IUnitOfWork, MongoUnitOfWork>();

        services.AddScoped<IUserRepository, MongoUserRepository>();
        services.AddScoped<IRefreshTokenRepository, MongoRefreshTokenRepository>();
        services.AddScoped<IGuidelineRepository, MongoGuidelineRepository>();
        services.AddScoped<IGuidelineHistoryRepository, MongoGuidelineHistoryRepository>();
        services.AddScoped<IIdeaRepository, MongoIdeaRepository>();
        services.AddScoped<IIdeaCommentRepository, MongoIdeaCommentRepository>();
        services.AddScoped<IProjectRepository, MongoProjectRepository>();
        services.AddScoped<IProjectSnapshotRepository, MongoProjectSnapshotRepository>();
        services.AddScoped<IResultRepository, MongoResultRepository>();
        services.AddScoped<IPointLedgerRepository, MongoPointLedgerRepository>();
        services.AddScoped<IAuditLogRepository, MongoAuditLogRepository>();
        services.AddScoped<INotificationRepository, MongoNotificationRepository>();
        services.AddScoped<IOutboxRepository, MongoOutboxRepository>();
        services.AddScoped<IAssistantRunRepository, MongoAssistantRunRepository>();
        services.AddScoped<IDashboardReadRepository, MongoDashboardReadRepository>();

        return services;
    }

    private static IServiceCollection AddIdentityStores(this IServiceCollection services)
    {
        services.AddScoped<IUserStore<User>, MongoUserStore>();
        services.AddScoped<IRoleStore<Role>, MongoRoleStore>();

        services
            .AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<Role>();

        return services;
    }
}
