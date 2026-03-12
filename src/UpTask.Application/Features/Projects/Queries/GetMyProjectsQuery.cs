using MediatR;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Features.Projects.DTOs;
using UpTask.Application.Features.Projects.Mapper;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Projects.Queries
{
    public record GetMyProjectsQuery : IRequest<IEnumerable<ProjectDto>>;

    public class GetMyProjectsHandler(IProjectRepository projectRepository, ICurrentUserService currentUser)
        : IRequestHandler<GetMyProjectsQuery, IEnumerable<ProjectDto>>
    {
        public async Task<IEnumerable<ProjectDto>> Handle(GetMyProjectsQuery request, CancellationToken ct)
        {
            var projects = await projectRepository.GetByMemberAsync(currentUser.UserId, ct);
            return projects.Select(p => ProjectMapper.ToDto(p, 0, 0, currentUser.UserId));
        }
    }
}