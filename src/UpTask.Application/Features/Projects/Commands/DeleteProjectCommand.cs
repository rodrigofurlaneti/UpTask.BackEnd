using MediatR;
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Common;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Projects.Commands
{
    // ── Delete Project ────────────────────────────────────────────────────────────
    public sealed record DeleteProjectCommand(Guid ProjectId) : IRequest<Result>;

    public sealed class DeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
        : IRequestHandler<DeleteProjectCommand, Result>
    {
        public async Task<Result> Handle(DeleteProjectCommand command, CancellationToken ct)
        {
            var project = await projectRepository.GetWithMembersAsync(command.ProjectId, ct);

            if (project is null)
                return Result.Failure(Error.NotFound("Project", command.ProjectId));

            if (!project.IsOwner(currentUser.UserId))
                return Result.Failure(Error.Unauthorized("Only the project owner can delete it."));

            projectRepository.Remove(project);
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }
    }

}
