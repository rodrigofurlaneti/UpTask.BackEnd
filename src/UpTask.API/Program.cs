using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using UpTask.API.Middleware;
using UpTask.API.Services;
using UpTask.Application;
using UpTask.Application.Common.Interfaces;
using UpTask.Infrastructure;
using UpTask.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;

// 1. LIMPEZA DE MAPEAMENTO (Deve ser a primeira linha)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Adiciona suporte a Enums como String no JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UpTask API",
        Version = "v1",
        Description = "Task & project management API"
    });

    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT."
    });

    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
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
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Banco de Dados ────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ── Pipeline de Middleware (A ORDEM AQUI É CRÍTICA) ───────────────────────────

// 1. Tratamento de erro global primeiro
app.UseMiddleware<GlobalExceptionMiddleware>();

// 2. Swagger
app.UseSwagger();
app.UseSwaggerUI();

// 3. Segurança e Roteamento
app.UseHttpsRedirection();
app.UseRouting();

// 4. CORS DEVE VIR ANTES DA AUTENTICAÇÃO
app.UseCors("AllowFrontend");

// 5. Segurança de Acesso
app.UseAuthentication();
app.UseAuthorization();

// 6. Endpoints
app.MapControllers();

await app.RunAsync();

public partial class Program { }