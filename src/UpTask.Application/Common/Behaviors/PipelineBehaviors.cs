using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UpTask.Domain.Common;

namespace UpTask.Application.Common.Behaviors;

// Valida os dados antes de chegarem ao Handler
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
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

        if (failures.Count == 0) return await next();

        logger.LogWarning("Validation failed for {RequestType}", typeof(TRequest).Name);

        var responseType = typeof(TResponse);

        // Se o retorno for Result<T>, mapeia o erro automaticamente
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var firstFailure = failures.First();
            var error = Error.Validation(firstFailure.PropertyName, firstFailure.ErrorMessage);
            var failureMethod = responseType.GetMethod("Failure", [typeof(Error)])!;
            return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        // Se o retorno for Result simples
        if (responseType == typeof(Result))
        {
            var firstFailure = failures.First();
            var error = Error.Validation(firstFailure.PropertyName, firstFailure.ErrorMessage);
            return (TResponse)(object)Result.Failure(error);
        }

        throw new ValidationException(failures);
    }
}

// Loga o início e o fim de cada ação
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

        var response = await next();

        sw.Stop();
        logger.LogInformation("Handled {Request} in {Elapsed}ms", name, sw.ElapsedMilliseconds);
        return response;
    }
}

// Avisa se alguma requisição demorar mais de 500ms
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
            logger.LogWarning("Slow request: {Request} took {Elapsed}ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);

        return response;
    }
}