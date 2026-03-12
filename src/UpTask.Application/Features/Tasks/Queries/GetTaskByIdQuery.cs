using MediatR;
using UpTask.Application.Features.Tasks.DTOs;
using UpTask.Application.Features.Tasks.Mapper;
using UpTask.Domain.Common;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Tasks.Queries
{
    public record GetTaskByIdQuery(Guid Id, Guid UserId) : IRequest<Result<TaskDetailDto>>;
    public class GetTaskByIdHandler(ITaskRepository taskRepository)
        : IRequestHandler<GetTaskByIdQuery, Result<TaskDetailDto>>
    {
        public async Task<Result<TaskDetailDto>> Handle(GetTaskByIdQuery request, CancellationToken ct)
        {
            var task = await taskRepository.GetWithDetailsAsync(request.Id, ct);

            if (task is null)
                return Result<TaskDetailDto>.Failure(Error.NotFound("Tasks.NotFound", "Task not found."));

            return Result<TaskDetailDto>.Success(TaskMapper.ToDetailDto(task));
        }
    }
}