using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace UpTask.Application.Common.Behaviors;

// ── Validation Behavior ───────────────────────────────────────────────────────
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}

// ── Logging Behavior ──────────────────────────────────────────────────────────
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {Request}", name);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();
            logger.LogInformation("Handled {Request} in {Elapsed}ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Error handling {Request} after {Elapsed}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

// ── Performance Behavior ──────────────────────────────────────────────────────
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowThresholdMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowThresholdMs)
            logger.LogWarning("Slow request detected: {Request} took {Elapsed}ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);

        return response;
    }
}
