using FluentValidation;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using UpTask.Application.Features.Projects.DTOs;
using UpTask.Application.Features.Projects.Mapper; // Adicionado para o ProjectMapper
using UpTask.Application.Common.Interfaces;
using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;
using UpTask.Domain.ValueObjects;

namespace UpTask.Application.Features.Projects.Commands
{
    // ── Update Project ────────────────────────────────────────────────────────────
    public sealed record UpdateProjectCommand(
        Guid ProjectId,
        string Name,
        string? Description,
        Priority Priority,
        DateOnly? StartDate,
        DateOnly? PlannedEndDate,
        string Color,
        string? Icon) : IRequest<Result<ProjectDto>>;

    public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
    {
        public UpdateProjectCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(ProjectName.MaxLength);
            RuleFor(x => x.Priority).IsInEnum();
            RuleFor(x => x)
                .Must(x => x.StartDate is null || x.PlannedEndDate is null || x.PlannedEndDate >= x.StartDate)
                .WithMessage("End date must be >= start date.");
        }
    }

    public sealed class UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
        : IRequestHandler<UpdateProjectCommand, Result<ProjectDto>>
    {
        public async Task<Result<ProjectDto>> Handle(UpdateProjectCommand command, CancellationToken ct)
        {
            var project = await projectRepository.GetWithMembersAsync(command.ProjectId, ct);

            if (project is null)
                return Result.Failure<ProjectDto>(Error.NotFound("Project", command.ProjectId));

            if (!project.IsAdmin(currentUser.UserId))
                return Result.Failure<ProjectDto>(
                    Error.Unauthorized("Only project admins can update the project."));

            try
            {
                project.Update(
                    new ProjectName(command.Name),
                    command.Description,
                    command.Priority,
                    command.StartDate,
                    command.PlannedEndDate,
                    new HexColor(command.Color),
                    command.Icon);
            }
            catch (DomainException ex)
            {
                return Result.Failure<ProjectDto>(Error.BusinessRule("Project.Update", ex.Message));
            }

            await unitOfWork.SaveChangesAsync(ct);

            // CORREÇÃO: Passando currentUser.UserId para o ToDto (Erro CS0103 / CS7036)
            return Result.Success(ProjectMapper.ToDto(project, 0, 0, currentUser.UserId));
        }
    }
}