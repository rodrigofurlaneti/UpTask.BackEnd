using MediatR;
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Common;
using UpTask.Domain.Interfaces;
using UpTask.Domain.Exceptions; // Adicionado para capturar exceções de domínio se necessário

namespace UpTask.Application.Features.Projects.Commands
{
    public sealed record RemoveProjectMemberCommand(Guid ProjectId, Guid UserId) : IRequest<Result>;

    public sealed class RemoveProjectMemberCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
        : IRequestHandler<RemoveProjectMemberCommand, Result>
    {
        public async Task<Result> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
        {
            var project = await projectRepository.GetWithMembersAsync(request.ProjectId, cancellationToken);

            if (project is null)
                return Result.Failure(Error.NotFound("Project", request.ProjectId));

            if (!project.IsAdmin(currentUser.UserId))
                return Result.Failure(Error.Unauthorized("Apenas administradores podem remover membros."));

            try
            {
                // CORREÇÃO: Removido o segundo argumento 'currentUser.UserId' 
                // para bater com a assinatura da entidade no Domain (Erro CS1501)
                project.RemoveMember(request.UserId);

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(Error.BusinessRule("Project.RemoveMember", ex.Message));
            }
        }
    }
}