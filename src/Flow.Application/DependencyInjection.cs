using Flow.Application.Auth;
using Flow.Application.Common.Behaviors;
using Flow.Application.Common.Services;
using Flow.Application.Projects;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<AuthTokenIssuer>();
        services.AddScoped<AuditTrail>();
        services.AddScoped<NotificationPublisher>();
        services.AddScoped<ProjectTransitionRecorder>();

        return services;
    }
}
