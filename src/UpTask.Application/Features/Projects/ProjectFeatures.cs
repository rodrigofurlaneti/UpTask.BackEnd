using FluentValidation;
using MediatR;
using UpTask.Application.Common.Models;
using UpTask.Domain.Entities;
using UpTask.Domain.Enums;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;

namespace UpTask.Application.Features.Projects;

// ── DTOs ─────────────────────────────────────────────────────────────────────
public record ProjectDto(Guid Id, string Name, string? Description, string Color, ProjectStatus Status,
    Priority Priority, DateOnly? StartDate, DateOnly? PlannedEndDate, int Progress,
    int TotalTasks, int CompletedTasks, DateTime CreatedAt);

public record ProjectMemberDto(Guid UserId, string UserName, string Email, MemberRole Role, DateTime? AcceptedAt);

// ── Create Project ────────────────────────────────────────────────────────────
public record CreateProjectCommand(string Name, string? Description, Priority Priority,
    DateOnly? StartDate, DateOnly? PlannedEndDate, Guid? CategoryId, Guid OwnerId) : IRequest<ProjectDto>;

public class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x).Must(x => x.StartDate == null || x.PlannedEndDate == null || x.PlannedEndDate >= x.StartDate)
            .WithMessage("End date must be >= start date.");
    }
}

public class CreateProjectHandler(IProjectRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand cmd, CancellationToken ct)
    {
        var project = Project.Create(cmd.OwnerId, cmd.Name, cmd.Description,
            cmd.Priority, cmd.StartDate, cmd.PlannedEndDate, cmd.CategoryId);
        await repo.AddAsync(project, ct);
        await uow.SaveChangesAsync(ct);
        return ProjectMapper.MapToDto(project, 0, 0);
    }
}

// ── Update Project ────────────────────────────────────────────────────────────
public record UpdateProjectCommand(Guid ProjectId, Guid RequesterId, string Name, string? Description,
    Priority Priority, DateOnly? StartDate, DateOnly? PlannedEndDate, string Color, string? Icon) : IRequest<ProjectDto>;

public class UpdateProjectHandler(IProjectRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(UpdateProjectCommand cmd, CancellationToken ct)
    {
        var project = await repo.GetWithMembersAsync(cmd.ProjectId, ct)
            ?? throw new NotFoundException("Project", cmd.ProjectId);

        if (!project.IsAdmin(cmd.RequesterId))
            throw new UnauthorizedException("Only project admins can update the project.");

        project.Update(cmd.Name, cmd.Description, cmd.Priority, cmd.StartDate, cmd.PlannedEndDate, cmd.Color, cmd.Icon);

        await uow.SaveChangesAsync(ct);
        return ProjectMapper.MapToDto(project, 0, 0);
    }
}

// ── Delete Project ────────────────────────────────────────────────────────────
public record DeleteProjectCommand(Guid ProjectId, Guid RequesterId) : IRequest<Unit>;

public class DeleteProjectHandler(IProjectRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteProjectCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProjectCommand cmd, CancellationToken ct)
    {
        var project = await repo.GetWithMembersAsync(cmd.ProjectId, ct)
            ?? throw new NotFoundException("Project", cmd.ProjectId);

        if (project.OwnerId != cmd.RequesterId)
            throw new UnauthorizedException("Only the project owner can delete it.");

        repo.Remove(project);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── Add Member ────────────────────────────────────────────────────────────────
public record AddProjectMemberCommand(Guid ProjectId, Guid RequesterId, Guid UserId, MemberRole Role) : IRequest<Unit>;

public class AddProjectMemberHandler(IProjectRepository repo, IUserRepository userRepo, IUnitOfWork uow)
    : IRequestHandler<AddProjectMemberCommand, Unit>
{
    public async Task<Unit> Handle(AddProjectMemberCommand cmd, CancellationToken ct)
    {
        var project = await repo.GetWithMembersAsync(cmd.ProjectId, ct)
            ?? throw new NotFoundException("Project", cmd.ProjectId);

        if (!project.IsAdmin(cmd.RequesterId))
            throw new UnauthorizedException("Only admins can add members.");

        var user = await userRepo.GetByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User", cmd.UserId);

        project.AddMember(user.Id, cmd.Role, cmd.RequesterId);

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── Change Project Status ─────────────────────────────────────────────────────
public record ChangeProjectStatusCommand(Guid ProjectId, Guid RequesterId, ProjectStatus NewStatus) : IRequest<Unit>;

public class ChangeProjectStatusHandler(IProjectRepository repo, IUnitOfWork uow)
    : IRequestHandler<ChangeProjectStatusCommand, Unit>
{
    public async Task<Unit> Handle(ChangeProjectStatusCommand cmd, CancellationToken ct)
    {
        var project = await repo.GetWithMembersAsync(cmd.ProjectId, ct)
            ?? throw new NotFoundException("Project", cmd.ProjectId);

        if (!project.IsAdmin(cmd.RequesterId))
            throw new UnauthorizedException("Only admins can change project status.");

        project.ChangeStatus(cmd.NewStatus);

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── Queries ───────────────────────────────────────────────────────────────────
public record GetProjectByIdQuery(Guid ProjectId, Guid RequesterId) : IRequest<ProjectDto>;

public class GetProjectByIdHandler(IProjectRepository repo, ITaskRepository taskRepo)
    : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    public async Task<ProjectDto> Handle(GetProjectByIdQuery q, CancellationToken ct)
    {
        var project = await repo.GetWithMembersAsync(q.ProjectId, ct)
            ?? throw new NotFoundException("Project", q.ProjectId);

        if (!project.IsMember(q.RequesterId) && project.OwnerId != q.RequesterId)
            throw new UnauthorizedException("Access denied.");

        var (total, completed) = await taskRepo.GetProjectProgressAsync(project.Id, ct);
        return ProjectMapper.MapToDto(project, total, completed);
    }
}

public record GetMyProjectsQuery(Guid UserId) : IRequest<IEnumerable<ProjectDto>>;

public class GetMyProjectsHandler(IProjectRepository repo, ITaskRepository taskRepo)
    : IRequestHandler<GetMyProjectsQuery, IEnumerable<ProjectDto>>
{
    public async Task<IEnumerable<ProjectDto>> Handle(GetMyProjectsQuery q, CancellationToken ct)
    {
        var projects = await repo.GetByMemberAsync(q.UserId, ct);
        var result = new List<ProjectDto>();
        foreach (var p in projects)
        {
            var (total, completed) = await taskRepo.GetProjectProgressAsync(p.Id, ct);
            result.Add(ProjectMapper.MapToDto(p, total, completed));
        }
        return result;
    }
}

// ── Mapper ────────────────────────────────────────────────────────────────────
internal static class ProjectMapper
{
    internal static ProjectDto MapToDto(Project p, int total, int completed) =>
        new(p.Id, p.Name, p.Description, p.Color, p.Status, p.Priority,
            p.StartDate, p.PlannedEndDate, p.Progress, total, completed, p.CreatedAt);
}