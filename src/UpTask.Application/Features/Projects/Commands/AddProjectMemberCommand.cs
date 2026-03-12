using MediatR;
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Projects.Commands
{
    // ── Add Project Member ────────────────────────────────────────────────────────
    public sealed record AddProjectMemberCommand(
        Guid ProjectId,
        Guid UserId,
        MemberRole Role) : IRequest<Result>;

    public sealed class AddProjectMemberCommandHandler(
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
        : IRequestHandler<AddProjectMemberCommand, Result>
    {
        public async Task<Result> Handle(AddProjectMemberCommand command, CancellationToken ct)
        {
            var project = await projectRepository.GetWithMembersAsync(command.ProjectId, ct);

            if (project is null)
                return Result.Failure(Error.NotFound("Project", command.ProjectId));

            if (!project.IsAdmin(currentUser.UserId))
                return Result.Failure(Error.Unauthorized("Only admins can add members."));

            var user = await userRepository.GetByIdAsync(command.UserId, ct);

            if (user is null)
                return Result.Failure(Error.NotFound("User", command.UserId));

            try
            {
                project.AddMember(user.Id, command.Role, currentUser.UserId);
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.BusinessRule("Project.AddMember", ex.Message));
            }

            await unitOfWork.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
