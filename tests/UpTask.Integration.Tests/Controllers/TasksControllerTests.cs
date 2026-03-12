using System.Net;
using FluentAssertions;
using UpTask.Domain.Enums;
using Xunit;

namespace UpTask.Integration.Tests.Controllers;

/// <summary>
/// End-to-end integration tests for the Tasks API endpoints.
/// Uses an in-memory database and a real HTTP stack via WebApplicationFactory.
/// </summary>
public sealed class TasksControllerTests(UpTaskWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    // ── POST /api/tasks ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTask_WithValidData_Returns201Created()
    {
        // Arrange
        var token = await RegisterAndLoginAsync(
            email: $"user_{Guid.NewGuid()}@test.com");
        SetBearerToken(token);

        // Act
        var response = await Client.PostAsync("/api/tasks", Json(new
        {
            title = "Integration test task",
            description = "Created in integration test",
            priority = (int)Priority.High,
            dueDate = DateTime.UtcNow.AddDays(7),
            storyPoints = 3
        }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await DeserializeAsync<dynamic>(response);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateTask_WithEmptyTitle_Returns400BadRequest()
    {
        var token = await RegisterAndLoginAsync(email: $"user_{Guid.NewGuid()}@test.com");
        SetBearerToken(token);

        var response = await Client.PostAsync("/api/tasks", Json(new
        {
            title = "",
            priority = (int)Priority.Medium
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTask_WithoutToken_Returns401Unauthorized()
    {
        // No SetBearerToken call
        var response = await Client.PostAsync("/api/tasks", Json(new
        {
            title = "Unauthorized task",
            priority = (int)Priority.Low
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/tasks/mine ───────────────────────────────────────────────────

    [Fact]
    public async Task GetMyTasks_WhenAuthenticated_Returns200WithList()
    {
        var token = await RegisterAndLoginAsync(email: $"user_{Guid.NewGuid()}@test.com");
        SetBearerToken(token);

        var response = await Client.GetAsync("/api/tasks/mine");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/tasks/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task GetTaskById_WithNonExistentId_Returns404NotFound()
    {
        var token = await RegisterAndLoginAsync(email: $"user_{Guid.NewGuid()}@test.com");
        SetBearerToken(token);

        var response = await Client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── AUTH FLOW ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409Conflict()
    {
        var email = $"dup_{Guid.NewGuid()}@test.com";

        // First registration
        await Client.PostAsync("/api/auth/register", Json(new
        {
            name = "User One",
            email,
            password = "Password1!",
            confirmPassword = "Password1!"
        }));

        // Duplicate registration
        var response = await Client.PostAsync("/api/auth/register", Json(new
        {
            name = "User Two",
            email,
            password = "Password1!",
            confirmPassword = "Password1!"
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Unauthorized()
    {
        var email = $"user_{Guid.NewGuid()}@test.com";

        await Client.PostAsync("/api/auth/register", Json(new
        {
            name = "Test User", email,
            password = "Password1!",
            confirmPassword = "Password1!"
        }));

        var loginResponse = await Client.PostAsync("/api/auth/login", Json(new
        {
            email,
            password = "WrongPassword99!"
        }));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
