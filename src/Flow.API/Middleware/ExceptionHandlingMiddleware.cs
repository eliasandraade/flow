using System.Net;
using System.Text.Json;
using Flow.Application.Common.Exceptions;

namespace Flow.API.Middleware;

public class ExceptionHandlingMiddleware
{
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
            _ =>
                (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (object?)null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception.");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var body = JsonSerializer.Serialize(
            new { title, status = (int)statusCode, errors },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(body);
    }
}
