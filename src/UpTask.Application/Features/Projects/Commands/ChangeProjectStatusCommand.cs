using MediatR;
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Projects.Commands
{
    // ── Change Project Status ─────────────────────────────────────────────────────
    public sealed record ChangeProjectStatusCommand(
        Guid ProjectId,
        ProjectStatus NewStatus) : IRequest<Result>;

    public sealed class ChangeProjectStatusCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
        : IRequestHandler<ChangeProjectStatusCommand, Result>
    {
        public async Task<Result> Handle(ChangeProjectStatusCommand command, CancellationToken ct)
        {
            var project = await projectRepository.GetWithMembersAsync(command.ProjectId, ct);

            if (project is null)
                return Result.Failure(Error.NotFound("Project", command.ProjectId));

            if (!project.IsAdmin(currentUser.UserId))
                return Result.Failure(Error.Unauthorized("Only admins can change project status."));

            try
            {
                project.ChangeStatus(command.NewStatus);
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.BusinessRule("Project.ChangeStatus", ex.Message));
            }

            await unitOfWork.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
