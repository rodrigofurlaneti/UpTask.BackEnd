using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UpTask.Infrastructure.Persistence;

namespace UpTask.Integration.Tests;

/// <summary>
/// Custom WebApplicationFactory that replaces SQL Server with EF Core InMemory.
/// Provides a clean, isolated database for each test run.
/// </summary>
public sealed class UpTaskWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove real DbContext
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // Add InMemory DbContext
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase($"UpTask_Test_{Guid.NewGuid()}"));

            // Ensure DB is created
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }
}

/// <summary>
/// Base integration test class providing HTTP client helpers,
/// auth token management, and serialization utilities.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<UpTaskWebApplicationFactory>
{
    protected readonly HttpClient Client;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected IntegrationTestBase(UpTaskWebApplicationFactory factory)
    {
        Client = factory.CreateClient();
    }

    protected StringContent Json(object payload) =>
        new(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");

    protected async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    protected void SetBearerToken(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    protected async Task<string> RegisterAndLoginAsync(
        string name = "Test User",
        string email = "test@uptask.com",
        string password = "Password1!")
    {
        var registerResp = await Client.PostAsync("/api/auth/register",
            Json(new { name, email, password, confirmPassword = password }));

        if (!registerResp.IsSuccessStatusCode)
        {
            // User might already exist — try login
            var loginResp = await Client.PostAsync("/api/auth/login",
                Json(new { email, password }));
            loginResp.EnsureSuccessStatusCode();
            var loginDto = await DeserializeAsync<AuthResponse>(loginResp);
            return loginDto!.AccessToken;
        }

        var dto = await DeserializeAsync<AuthResponse>(registerResp);
        return dto!.AccessToken;
    }

    private sealed record AuthResponse(string AccessToken);
}
