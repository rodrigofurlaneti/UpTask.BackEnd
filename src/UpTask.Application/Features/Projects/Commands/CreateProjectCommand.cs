using FluentValidation;
using MediatR;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Features.Projects.DTOs;
using UpTask.Application.Features.Projects.Mapper; // Adicionado para o ProjectMapper
using UpTask.Domain.Common;
using UpTask.Domain.Entities;
using UpTask.Domain.Enums;
using UpTask.Domain.Interfaces;
using UpTask.Domain.ValueObjects; // Adicionado para ProjectName e HexColor

namespace UpTask.Application.Features.Projects.Commands
{
    public record CreateProjectCommand(
        string Name,
        string? Description,
        Priority Priority,
        DateOnly? StartDate,
        DateOnly? PlannedEndDate,
        Guid? CategoryId,
        string? Color) : IRequest<Result<ProjectDto>>;

    public class CreateProjectHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
        : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
    {
        public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken ct)
        {
            // Correção: Envolvendo strings nos Value Objects do Domínio
            var project = Project.Create(
                currentUser.UserId,
                new ProjectName(request.Name),
                request.Description,
                request.Priority,
                request.StartDate,
                request.PlannedEndDate,
                request.CategoryId,
                new HexColor(request.Color ?? "#1976D2"));

            await projectRepository.AddAsync(project, ct);
            await unitOfWork.SaveChangesAsync(ct);

            // Correção: Passando os argumentos necessários para o Mapper (incluindo o ID para IsOwner)
            return Result.Success(ProjectMapper.ToDto(project, 0, 0, currentUser.UserId));
        }
    }
}