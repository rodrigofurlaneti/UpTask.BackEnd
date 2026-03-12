using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Interfaces;
using UpTask.Infrastructure.Authentication;
using UpTask.Infrastructure.Persistence;
using UpTask.Infrastructure.Persistence.Repositories;

namespace UpTask.Infrastructure;

/// <summary>
/// Registers all Infrastructure layer services into the DI container.
/// Called from UpTask.API's Program.cs.
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql =>
                {
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sql.EnableRetryOnFailure(maxRetryCount: 5);
                }));

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Authentication Services ───────────────────────────────────────────
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordService, PasswordService>();

        // ── JWT Bearer Authentication ─────────────────────────────────────────
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var secretKey = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"] ?? "UpTask.API",
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"] ?? "UpTask.Client",
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }
}
