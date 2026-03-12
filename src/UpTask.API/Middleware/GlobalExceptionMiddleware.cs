using System.Text.Json;
using FluentValidation;

namespace UpTask.API.Middleware;

/// <summary>
/// Last-resort exception handler.
/// Catches unhandled exceptions and returns structured ProblemDetails JSON.
/// FluentValidation exceptions (from fallback paths) are translated to 400.
/// All other exceptions return 500 with a generic message in production.
/// </summary>
public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment env)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "FluentValidation exception (unhandled path)");
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Validation.Failed",
                "One or more validation errors occurred.",
                new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            var detail = env.IsDevelopment()
                ? ex.ToString()
                : "An unexpected error occurred. Please try again later.";

            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "Server.Error", detail);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        object? extensions = null)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var body = new
        {
            type = $"https://httpstatuses.io/{statusCode}",
            title,
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier,
            extensions
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
