using MediatR;
using UpTask.Application.Features.Tasks.DTOs;
using UpTask.Application.Features.Tasks.Mapper; // Adicionado
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;
using UpTask.Domain.Enums;
using UpTask.Domain.ValueObjects; // Adicionado para o TaskTitle

namespace UpTask.Application.Features.Tasks.Commands
{
    // ── Update Task ───────────────────────────────────────────────────────────────
    public record UpdateTaskCommand(Guid TaskId, Guid UserId, Guid RequesterId, string Title, string? Description,
        Priority Priority, DateTime? StartDate, DateTime? DueDate, int? StoryPoints, Guid? CategoryId) : IRequest<TaskDto>;

    public class UpdateTaskHandler(ITaskRepository repo, IUnitOfWork uow)
        : IRequestHandler<UpdateTaskCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(UpdateTaskCommand cmd, CancellationToken ct)
        {
            var task = await repo.GetByIdAsync(cmd.TaskId, ct)
                ?? throw new NotFoundException("Task", cmd.TaskId);

            if (task.CreatedBy != cmd.RequesterId && task.AssigneeId != cmd.RequesterId)
                throw new UnauthorizedException("You cannot edit this task.");

            // Correções: 
            // 1. Convertendo string para TaskTitle
            // 2. Adicionando o parâmetro estimatedHours: null no final
            task.Update(
                new TaskTitle(cmd.Title),
                cmd.Description,
                cmd.Priority,
                cmd.StartDate,
                cmd.DueDate,
                cmd.StoryPoints,
                cmd.CategoryId,
                null); // estimatedHours

            await uow.SaveChangesAsync(ct);
            return TaskMapper.ToDto(task);
        }
    }
}