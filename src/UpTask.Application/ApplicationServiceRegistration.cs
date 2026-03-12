using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using UpTask.Application.Common.Behaviors;

namespace UpTask.Application;

/// <summary>
/// Registers all Application layer services into the DI container.
/// Called from UpTask.API's Program.cs.
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceRegistration).Assembly;

        // MediatR — scans all handlers, commands, and queries in this assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // FluentValidation — scans all AbstractValidator<T> in this assembly
        services.AddValidatorsFromAssembly(assembly);

        // MediatR pipeline behaviors — order matters (logging → validation → performance)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }
}
