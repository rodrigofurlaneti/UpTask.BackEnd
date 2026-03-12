using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using UpTask.Application.Features.Tasks.DTOs;
using UpTask.Application.Features.Tasks.Mapper; // Adicionado
using UpTask.Domain.Exceptions; // Adicionado para BusinessRuleException
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Tasks.Commands
{
    public record AssignTaskCommand(Guid TaskId, Guid RequesterId, Guid AssigneeId) : IRequest<TaskDto>;

    public class AssignTaskHandler(
        ITaskRepository repo,
        IProjectRepository projectRepo,
        IUserRepository userRepo,
        IUnitOfWork uow) : IRequestHandler<AssignTaskCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(AssignTaskCommand cmd, CancellationToken ct)
        {
            var task = await repo.GetByIdAsync(cmd.TaskId, ct)
                ?? throw new NotFoundException("Task", cmd.TaskId);

            _ = await userRepo.GetByIdAsync(cmd.AssigneeId, ct)
                ?? throw new NotFoundException("User", cmd.AssigneeId);

            if (task.ProjectId.HasValue)
            {
                var project = await projectRepo.GetWithMembersAsync(task.ProjectId.Value, ct);
                if (project != null && !project.IsMember(cmd.AssigneeId))
                    // BusinessRuleException agora será reconhecido pelo using
                    throw new UnauthorizedException("Assignee must be a project member.");
            }

            task.Assign(cmd.AssigneeId);

            await uow.SaveChangesAsync(ct);
            return TaskMapper.MapToDto(task);
        }
    }
}