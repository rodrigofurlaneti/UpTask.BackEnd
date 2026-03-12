using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpTask.Domain.Common;

namespace UpTask.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiController(ISender sender) : ControllerBase
{
    protected readonly ISender Sender = sender;
    protected IActionResult Ok<TValue>(Result<TValue> result) =>
        result.IsSuccess ? base.Ok(result.Value) : Problem(result.Error);

    protected IActionResult NoContent(Result result) =>
        result.IsSuccess ? base.NoContent() : Problem(result.Error);

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
