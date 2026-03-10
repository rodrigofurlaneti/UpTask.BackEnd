using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Interfaces;
using UpTask.Infrastructure.Authentication;
using UpTask.Infrastructure.Persistence;
using UpTask.Infrastructure.Persistence.Repositories;

namespace UpTask.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration config)
    {
        // Database
        var connString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found.");

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseMySql(connString, ServerVersion.AutoDetect(connString),
                mySql => mySql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Auth services
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordService, PasswordService>();

        return services;
    }
}
