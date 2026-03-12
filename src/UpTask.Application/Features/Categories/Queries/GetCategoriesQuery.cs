using MediatR;
using UpTask.Application.Features.Categories.DTOs;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Categories.Queries
{
    // ── Get Categories ────────────────────────────────────────────────────────────
    public record GetCategoriesQuery(Guid? UserId) : IRequest<IEnumerable<CategoryDto>>;

    public class GetCategoriesHandler(ICategoryRepository repo)
        : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
    {
        public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery q, CancellationToken ct)
        {
            var global = await repo.GetGlobalAsync(ct);
            var user = q.UserId.HasValue ? await repo.GetByUserAsync(q.UserId.Value, ct) : [];
            return global.Concat(user)
                .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.Color, c.Icon, c.IsGlobal));
        }
    }
}
