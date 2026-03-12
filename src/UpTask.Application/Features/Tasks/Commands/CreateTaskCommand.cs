using FluentValidation;
using MediatR;
using UpTask.Application.Features.Tasks.DTOs;
using UpTask.Application.Features.Tasks.Mapper; // Adicionado para reconhecer o TaskMapper
using UpTask.Domain.Entities;
using UpTask.Domain.Enums;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;
using UpTask.Domain.ValueObjects; // Adicionado para o TaskTitle

namespace UpTask.Application.Features.Tasks.Commands
{
    public record CreateTaskCommand(Guid CreatedBy, string Title, string? Description, Priority Priority,
        DateTime? DueDate, Guid? ProjectId, Guid? ParentTaskId, Guid? CategoryId,
        int? StoryPoints, List<Guid>? TagIds) : IRequest<TaskDto>;

    public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
            RuleFor(x => x.Priority).IsInEnum();
            RuleFor(x => x.StoryPoints).GreaterThanOrEqualTo(0).When(x => x.StoryPoints.HasValue);
        }
    }

    public class CreateTaskHandler(ITaskRepository repo, IProjectRepository projectRepo, 
        IUnitOfWork uow)
        : IRequestHandler<CreateTaskCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(CreateTaskCommand cmd, CancellationToken ct)
        {
            if (cmd.ProjectId.HasValue)
            {
                var project = await projectRepo.GetWithMembersAsync(cmd.ProjectId.Value, ct)
                    ?? throw new NotFoundException("Project", cmd.ProjectId.Value);

                if (!project.IsMember(cmd.CreatedBy))
                    throw new UnauthorizedException("You are not a member of this project.");
            }

            // Correção: Criando o Value Object TaskTitle a partir da string do comando
            var task = TaskItem.Create(
                cmd.CreatedBy,
                new TaskTitle(cmd.Title), // Ajustado aqui
                cmd.Description,
                cmd.Priority,
                cmd.DueDate,
                cmd.ProjectId,
                cmd.ParentTaskId,
                cmd.CategoryId,
                cmd.StoryPoints);

            await repo.AddAsync(task, ct);
            await uow.SaveChangesAsync(ct);

            return TaskMapper.MapToDto(task);
        }
    }
}