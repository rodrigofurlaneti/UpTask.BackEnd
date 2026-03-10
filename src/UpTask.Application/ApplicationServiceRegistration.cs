using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using UpTask.Application.Common.Behaviors;
using System.Reflection;

namespace UpTask.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
