using System.Threading.RateLimiting;
using Flow.API;
using Flow.API.Middleware;
using Flow.API.Services;
using Flow.Application;
using Flow.Application.Auth;
using Flow.Application.Common.Interfaces;
using Flow.Infrastructure;
using Flow.Infrastructure.Observability;
using Flow.Infrastructure.Persistence.Mongo;
using Flow.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logging — structured from the first line, so startup failures are readable too.
// ---------------------------------------------------------------------------
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("service", FlowTelemetry.ServiceName)
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

// ---------------------------------------------------------------------------
// Application composition
// ---------------------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("Auth"));

// ---------------------------------------------------------------------------
// Authentication
// ---------------------------------------------------------------------------
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey is missing from configuration.");

if (!builder.Environment.IsDevelopment())
{
    if (jwtSecret.StartsWith("CHANGE-THIS", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException(
            "JwtSettings:SecretKey must be replaced with a secure value before running outside of Development.");

    // HMAC-SHA256 keys shorter than the hash output weaken the signature, and a short
    // secret is the single easiest production mistake to make here.
    if (System.Text.Encoding.UTF8.GetByteCount(jwtSecret) < 32)
        throw new InvalidOperationException(
            "JwtSettings:SecretKey must be at least 32 bytes long.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer();

builder.Services.ConfigureOptions<JwtBearerOptionsSetup>();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ---------------------------------------------------------------------------
// CORS — explicit origins only. A wildcard would be a silent invitation.
// ---------------------------------------------------------------------------
const string CorsPolicy = "flow-clients";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    else
        // Development convenience only; production configuration must list its origins.
        policy.SetIsOriginAllowed(_ => builder.Environment.IsDevelopment())
              .AllowAnyHeader().AllowAnyMethod();
}));

// ---------------------------------------------------------------------------
// Rate limiting — authentication and the AI endpoints are the two surfaces where
// abuse is cheap for the attacker and expensive for us.
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.Auth, http =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy(RateLimitPolicies.Ai, http =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.User.FindFirst("sub")?.Value
                ?? http.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------------------------------------------------------------------------
// Observability
// ---------------------------------------------------------------------------
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

builder.Services.AddSingleton<FlowMetrics>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: FlowTelemetry.ServiceName,
        serviceVersion: FlowTelemetry.ServiceVersion))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(o => o.Filter = ctx =>
                !ctx.Request.Path.StartsWithSegments("/health"))
            .AddHttpClientInstrumentation()
            .AddSource(FlowTelemetry.ActivitySourceName)
            // Emitted by the MongoDB driver's diagnostics subscriber.
            .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources");

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(FlowTelemetry.MeterName);

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    });

// ---------------------------------------------------------------------------
// Health checks
// ---------------------------------------------------------------------------
var mongoConnectionString = builder.Configuration["Mongo:ConnectionString"]
    ?? "mongodb://localhost:27017/?replicaSet=rs0";

builder.Services.AddHealthChecks()
    .AddMongoDb(
        clientFactory: sp => sp.GetRequiredService<MongoDB.Driver.IMongoClient>(),
        databaseNameFactory: _ => builder.Configuration["Mongo:Database"] ?? "flow",
        name: "mongodb",
        tags: ["ready"]);

// ---------------------------------------------------------------------------
// HTTP API
// ---------------------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(SwaggerConfiguration.Configure);

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (http, _, ex) =>
        ex is not null ? Serilog.Events.LogEventLevel.Error
        : http.Response.StatusCode >= 500 ? Serilog.Events.LogEventLevel.Error
        : http.Request.Path.StartsWithSegments("/health") ? Serilog.Events.LogEventLevel.Verbose
        : Serilog.Events.LogEventLevel.Information;
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Flow API v1"));
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Liveness answers "is the process up"; readiness answers "can it actually serve".
// Keeping them apart stops an orchestrator from killing a healthy pod during a brief
// database blip.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteAsync
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync
});

await app.InitialiseDatabaseAsync();

app.Run();

/// <summary>Exposed so the integration test host can reference the entry point assembly.</summary>
public partial class Program { }
