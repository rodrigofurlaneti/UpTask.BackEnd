using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UpTask.Application.Features.Tasks.DTOs;
using UpTask.Application.Features.Tasks.Mapper; // Adicionado para o TaskMapper
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Tasks.Queries
{
    public record GetProjectTasksQuery(Guid ProjectId, Guid RequesterId) : IRequest<IEnumerable<TaskDto>>;

    public class GetProjectTasksHandler(ITaskRepository repo, IProjectRepository projectRepo)
        : IRequestHandler<GetProjectTasksQuery, IEnumerable<TaskDto>>
    {
        public async Task<IEnumerable<TaskDto>> Handle(GetProjectTasksQuery q, CancellationToken ct)
        {
            var project = await projectRepo.GetWithMembersAsync(q.ProjectId, ct)
                ?? throw new NotFoundException("Project", q.ProjectId);

            if (!project.IsMember(q.RequesterId))
                throw new UnauthorizedException("Access denied.");

            var tasks = await repo.GetByProjectAsync(q.ProjectId, ct);

            // Agora o TaskMapper é reconhecido
            return tasks.Select(TaskMapper.ToDto);
        }
    }

    public record GetMyTasksQuery(Guid UserId) : IRequest<IEnumerable<TaskDto>>;

    public class GetMyTasksHandler(ITaskRepository repo) : IRequestHandler<GetMyTasksQuery, IEnumerable<TaskDto>>
    {
        public async Task<IEnumerable<TaskDto>> Handle(GetMyTasksQuery q, CancellationToken ct)
        {
            var tasks = await repo.GetByAssigneeAsync(q.UserId, ct);

            // Agora o TaskMapper é reconhecido
            return tasks.Select(TaskMapper.ToDto);
        }
    }
}