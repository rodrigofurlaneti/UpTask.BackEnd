using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Exceptions;

namespace UpTask.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiController(ISender mediator, ICurrentUserService currentUser) : ControllerBase
{
    protected readonly ISender _mediator = mediator;

    protected Guid CurrentUserId => currentUser.UserId
        ?? throw new UnauthorizedException("User not authenticated.");
}
