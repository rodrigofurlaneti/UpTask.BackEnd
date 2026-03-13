using FluentValidation;
using MediatR;
using UpTask.Application.Common.Interfaces;
using UpTask.Application.Features.Projects.DTOs;
using UpTask.Application.Features.Projects.Mapper;
using UpTask.Domain.Common;
using UpTask.Domain.Entities;
using UpTask.Domain.Enums;
using UpTask.Domain.Interfaces;
using UpTask.Domain.ValueObjects;

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

    // 1. Adicionando Validação para evitar que cheguem dados nulos ao Handler
    public class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
    {
        public CreateProjectValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Color).Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
                .When(x => !string.IsNullOrEmpty(x.Color))
                .WithMessage("Cor inválida");
        }
    }

    public class CreateProjectHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
        : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
    {
        public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken ct)
        {
            // Verificação de segurança: Se o UserId for nulo ou vazio, o Token está inválido/antigo
            if (currentUser.UserId == Guid.Empty)
            {
                return Result.Failure<ProjectDto>(new Error("Auth.UserNotFound", "Usuário não identificado. Faça login novamente."));
            }

            try
            {
                // 2. Criando a Entidade de Domínio
                // Certifique-se que dentro do Project.Create, você adiciona o Owner à lista de Members
                var project = Project.Create(
                    currentUser.UserId,
                    new ProjectName(request.Name),
                    request.Description,
                    request.Priority,
                    request.StartDate,
                    request.PlannedEndDate,
                    request.CategoryId,
                    new HexColor(request.Color ?? "#1976D2"));

                // 3. Persistência
                await projectRepository.AddAsync(project, ct);

                // O SaveChangesAsync aqui vai disparar os eventos de domínio (se houver)
                await unitOfWork.SaveChangesAsync(ct);

                // 4. Retorno mapeado
                // Passamos 0 para tarefas pois o projeto é novo
                return Result.Success(ProjectMapper.ToDto(project, 0, 0, currentUser.UserId));
            }
            catch (Exception ex)
            {
                // Captura erros de banco (como a FK que você recebeu) e retorna como falha
                return Result.Failure<ProjectDto>(new Error("Project.CreateError", ex.InnerException?.Message ?? ex.Message));
            }
        }
    }
}