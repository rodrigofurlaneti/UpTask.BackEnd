using System.Net;
using System.Text.Json;
using FluentValidation;
using UpTask.Domain.Exceptions;

namespace UpTask.API.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(ctx, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext ctx, Exception ex)
    {
        ctx.Response.ContentType = "application/json";

        var (status, message, errors) = ex switch
        {
            ValidationException ve => (HttpStatusCode.UnprocessableEntity, "Validation failed",
                ve.Errors.Select(e => e.ErrorMessage).ToArray()),
            NotFoundException nfe => (HttpStatusCode.NotFound, nfe.Message, null),
            UnauthorizedException ue => (HttpStatusCode.Forbidden, ue.Message, null),
            BusinessRuleException bre => (HttpStatusCode.BadRequest, bre.Message, null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", null)
        };

        ctx.Response.StatusCode = (int)status;

        var response = new
        {
            success = false,
            message,
            errors,
            timestamp = DateTime.UtcNow
        };

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
