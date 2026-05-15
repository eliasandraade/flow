using System.Net;
using System.Text.Json;
using Flow.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Exception occurred after response had started; cannot write error response.");
            return;
        }

        var (statusCode, title, errors) = exception switch
        {
            Application.Common.Exceptions.ValidationException ve =>
                (HttpStatusCode.BadRequest, "Validation failed", (object?)ve.Errors),
            NotFoundException nfe =>
                (HttpStatusCode.NotFound, nfe.Message, (object?)null),
            ConflictException ce =>
                (HttpStatusCode.Conflict, ce.Message, (object?)null),
            ForbiddenException fe =>
                (HttpStatusCode.Forbidden, fe.Message, (object?)null),
            Flow.Domain.Exceptions.DomainException de =>
                (HttpStatusCode.BadRequest, de.Message, (object?)null),
            _ =>
                (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (object?)null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception.");

        var problem = new ProblemDetails
        {
            Title = title,
            Status = (int)statusCode,
        };

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
