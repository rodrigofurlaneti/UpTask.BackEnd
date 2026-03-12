using MediatR;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Features.Projects.DTOs;
using UpTask.Application.Features.Projects.Mapper; // Adicionado para o ProjectMapper
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Projects.Queries
{
    public record GetProjectByIdQuery(Guid Id) : IRequest<ProjectDto?>;

    public class GetProjectByIdHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUser) // Injetado para o IsOwner
        : IRequestHandler<GetProjectByIdQuery, ProjectDto?>
    {
        public async Task<ProjectDto?> Handle(GetProjectByIdQuery request, CancellationToken ct)
        {
            var project = await projectRepository.GetWithMembersAsync(request.Id, ct);

            if (project is null) return null;

            // Buscamos as estatísticas ou passamos 0 se não estiverem disponíveis aqui
            // O importante é passar o currentUser.UserId para o Mapper
            return ProjectMapper.ToDto(project, 0, 0, currentUser.UserId);
        }
    }
}