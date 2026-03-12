using MediatR;
using UpTask.Application.Features.Tasks.DTOs;
using UpTask.Application.Features.Tasks.Mapper; // Adicionado para o TaskMapper
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Tasks.Queries
{
    public record GetTaskByIdQuery(Guid Id) : IRequest<TaskDto?>;

    public class GetTaskByIdHandler(ITaskRepository taskRepository)
        : IRequestHandler<GetTaskByIdQuery, TaskDto?>
    {
        public async Task<TaskDto?> Handle(GetTaskByIdQuery request, CancellationToken ct)
        {
            var task = await taskRepository.GetByIdAsync(request.Id, ct);

            if (task is null) return null;

            // Correção: Agora o TaskMapper é reconhecido pelo compilador
            return TaskMapper.MapToDto(task);
        }
    }
}