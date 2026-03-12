using FluentValidation;
using MediatR;
using UpTask.Application.Features.Categories.DTOs;
using UpTask.Domain.Entities;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Categories.Commands
{
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
                throw new UnauthorizedException($"Tag '{cmd.Name}' already exists.");

            var tag = Tag.Create(cmd.UserId, cmd.Name, cmd.Color);
            await repo.AddAsync(tag, ct);
            await uow.SaveChangesAsync(ct);

            return new TagDto(tag.Id, tag.Name, tag.Color);
        }
    }
}