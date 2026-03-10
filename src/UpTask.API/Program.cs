using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using UpTask.Application;
using UpTask.API.Middleware;
using UpTask.Infrastructure;
using UpTask.Application.Common.Interfaces;
using UpTask.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/uptask-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ── Application + Infrastructure ──────────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ── HttpContext / Current User ────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT:Secret not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opt.AddPolicy("ManagerOrAdmin", p => p.RequireRole("Admin", "Manager"));
});

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UpTask API",
        Version = "v1",
        Description = "Task Organizer API — DDD + CQRS + Clean Architecture",
        Contact = new OpenApiContact { Name = "UpTask Team" }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter: **Bearer {token}**",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opt => opt.AddPolicy("AllowAll", p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "UpTask v1"));
}

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { } // needed for integration tests
