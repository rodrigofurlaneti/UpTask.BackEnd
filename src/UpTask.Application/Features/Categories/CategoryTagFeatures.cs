using FluentValidation;
using MediatR;
using UpTask.Domain.Entities;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Categories;

public record CategoryDto(Guid Id, string Name, string? Description, string Color, string? Icon, bool IsGlobal);
public record TagDto(Guid Id, string Name, string Color);

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

// ── Create Tag ────────────────────────────────────────────────────────────────
public record CreateTagCommand(Guid UserId, string Name, string Color) : IRequest<TagDto>;

public class CreateTagValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Color).Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color must be hex.");
    }
}

public class CreateTagHandler(ITagRepository repo, IUnitOfWork uow) : IRequestHandler<CreateTagCommand, TagDto>
{
    public async Task<TagDto> Handle(CreateTagCommand cmd, CancellationToken ct)
    {
        if (await repo.ExistsAsync(cmd.UserId, cmd.Name, ct))
            throw new BusinessRuleException($"Tag '{cmd.Name}' already exists.");

        var tag = Tag.Create(cmd.UserId, cmd.Name, cmd.Color);
        await repo.AddAsync(tag, ct);
        await uow.SaveChangesAsync(ct);
        return new TagDto(tag.Id, tag.Name, tag.Color);
    }
}

// ── Get My Tags ───────────────────────────────────────────────────────────────
public record GetMyTagsQuery(Guid UserId) : IRequest<IEnumerable<TagDto>>;

public class GetMyTagsHandler(ITagRepository repo) : IRequestHandler<GetMyTagsQuery, IEnumerable<TagDto>>
{
    public async Task<IEnumerable<TagDto>> Handle(GetMyTagsQuery q, CancellationToken ct)
    {
        var tags = await repo.GetByUserAsync(q.UserId, ct);
        return tags.Select(t => new TagDto(t.Id, t.Name, t.Color));
    }
}

// ── Delete Tag ────────────────────────────────────────────────────────────────
public record DeleteTagCommand(Guid TagId, Guid UserId) : IRequest<Unit>;

public class DeleteTagHandler(ITagRepository repo, IUnitOfWork uow) : IRequestHandler<DeleteTagCommand, Unit>
{
    public async Task<Unit> Handle(DeleteTagCommand cmd, CancellationToken ct)
    {
        var tag = await repo.GetByIdAsync(cmd.TagId, ct)
            ?? throw new NotFoundException("Tag", cmd.TagId);

        if (tag.UserId != cmd.UserId) throw new UnauthorizedException("Cannot delete another user's tag.");
        repo.Remove(tag);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
