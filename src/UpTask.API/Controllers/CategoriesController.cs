using MediatR;
using Microsoft.AspNetCore.Mvc;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Common.Models;
using UpTask.Application.Features.Categories.Commands;
using UpTask.Application.Features.Categories.Queries;
using UpTask.Application.Features.Categories.DTOs;
namespace UpTask.API.Controllers
{
    // ── CATEGORIES ────────────────────────────────────────────────────────────────
    [Route("api/v1/categories")]
    public sealed class CategoriesController(ISender mediator, ICurrentUserService currentUser)
        : ApiController(mediator, currentUser)
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetCategoriesQuery(CurrentUserId), ct);
            return Ok(ApiResponse<IEnumerable<CategoryDto>>.Ok(result));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand cmd, CancellationToken ct)
        {
            var result = await _mediator.Send(cmd with { UserId = CurrentUserId }, ct);
            return Ok(ApiResponse<CategoryDto>.Ok(result));
        }
    }
}
