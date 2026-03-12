using FluentValidation;
using MediatR;
using UpTask.Application.Features.Categories.DTOs;
using UpTask.Domain.Entities;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Categories.Commands
{
    // ── Create Category ───────────────────────────────────────────────────────────
    public record CreateCategoryCommand(string Name, string? Description, string Color,
        string? Icon, Guid? UserId, Guid? ParentId) : IRequest<CategoryDto>;

    public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Color).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex (#RRGGBB).");
        }
    }

    public class CreateCategoryHandler(ICategoryRepository repo, IUnitOfWork uow)
        : IRequestHandler<CreateCategoryCommand, CategoryDto>
    {
        public async Task<CategoryDto> Handle(CreateCategoryCommand cmd, CancellationToken ct)
        {
            var category = Category.Create(cmd.Name, cmd.Description, cmd.Color, cmd.Icon, cmd.UserId, cmd.ParentId);
            await repo.AddAsync(category, ct);
            await uow.SaveChangesAsync(ct);
            return new CategoryDto(category.Id, category.Name, category.Description, category.Color, category.Icon, category.IsGlobal);
        }
    }

}
