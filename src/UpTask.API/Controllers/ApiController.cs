using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpTask.Domain.Common;

namespace UpTask.API.Controllers;

/// <summary>
/// Base controller providing:
///   - [ApiController] attribute (automatic model binding and validation).
///   - [Authorize] by default — individual endpoints opt-out with [AllowAnonymous].
///   - Helper methods to translate Result&lt;T&gt; into correct HTTP responses.
///   - Injects ISender (MediatR) for dispatching commands and queries.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiController(ISender sender) : ControllerBase
{
    protected readonly ISender Sender = sender;

    /// <summary>
    /// Translates a Result&lt;T&gt; to the correct HTTP action result.
    /// Success → 200 OK with the value.
    /// Failure → mapped HTTP status code with a problem detail response.
    /// </summary>
    protected IActionResult Ok<TValue>(Result<TValue> result) =>
        result.IsSuccess ? base.Ok(result.Value) : Problem(result.Error);

    /// <summary>Translates a Result (no value) to 204 No Content or problem.</summary>
    protected IActionResult NoContent(Result result) =>
        result.IsSuccess ? base.NoContent() : Problem(result.Error);

    /// <summary>Translates a Result to 201 Created or problem.</summary>
    protected IActionResult Created<TValue>(string? routeName, object? routeValues, Result<TValue> result) =>
        result.IsSuccess
            ? base.CreatedAtRoute(routeName, routeValues, result.Value)
            : Problem(result.Error);

    private IActionResult Problem(Error error)
    {
        var statusCode = error.Code switch
        {
            var c when c.EndsWith(".NotFound") => StatusCodes.Status404NotFound,
            var c when c.StartsWith("Auth.") => StatusCodes.Status401Unauthorized,
            var c when c.StartsWith("Validation.") => StatusCodes.Status400BadRequest,
            var c when c.EndsWith(".Conflict") => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity
        };

        return base.Problem(
            detail: error.Description,
            statusCode: statusCode,
            title: error.Code);
    }
}
