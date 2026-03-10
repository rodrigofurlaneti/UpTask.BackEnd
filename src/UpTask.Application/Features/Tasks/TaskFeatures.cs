using FluentValidation;
using MediatR;
using UpTask.Domain.Entities;
using UpTask.Domain.Enums;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;
using TaskStatus = UpTask.Domain.Enums.TaskStatus;

namespace UpTask.Application.Features.Tasks;

// ── DTOs ─────────────────────────────────────────────────────────────────────
public record TaskDto(Guid Id, string Title, string? Description, TaskStatus Status, Priority Priority,
    DateTime? DueDate, DateTime? CompletedAt, decimal? EstimatedHours, decimal HoursWorked,
    int? StoryPoints, bool IsOverdue, Guid? ProjectId, Guid? ParentTaskId,
    Guid? AssigneeId, string? AssigneeName, DateTime CreatedAt);

public record TaskDetailDto(TaskDto Task, IEnumerable<TaskDto> SubTasks,
    IEnumerable<CommentDto> Comments, IEnumerable<ChecklistDto> Checklists);

public record CommentDto(Guid Id, string Content, Guid AuthorId, string AuthorName,
    bool IsEdited, DateTime CreatedAt);

public record ChecklistDto(Guid Id, string Title, int CompletionPercentage,
    IEnumerable<ChecklistItemDto> Items);

public record ChecklistItemDto(Guid Id, string Description, bool IsCompleted, DateTime? CompletedAt);

// ── Create Task ───────────────────────────────────────────────────────────────
public record CreateTaskCommand(Guid CreatedBy, string Title, string? Description, Priority Priority,
    DateTime? DueDate, Guid? ProjectId, Guid? ParentTaskId, Guid? CategoryId,
    int? StoryPoints, List<Guid>? TagIds) : IRequest<TaskDto>;

public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.StoryPoints).GreaterThanOrEqualTo(0).When(x => x.StoryPoints.HasValue);
    }
}

public class CreateTaskHandler(ITaskRepository repo, IProjectRepository projectRepo, IUnitOfWork uow)
    : IRequestHandler<CreateTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(CreateTaskCommand cmd, CancellationToken ct)
    {
        if (cmd.ProjectId.HasValue)
        {
            var project = await projectRepo.GetWithMembersAsync(cmd.ProjectId.Value, ct)
                ?? throw new NotFoundException("Project", cmd.ProjectId.Value);
            if (!project.IsMember(cmd.CreatedBy))
                throw new UnauthorizedException("You are not a member of this project.");
        }

        var task = TaskItem.Create(cmd.CreatedBy, cmd.Title, cmd.Description,
            cmd.Priority, cmd.DueDate, cmd.ProjectId, cmd.ParentTaskId, cmd.CategoryId, cmd.StoryPoints);

        await repo.AddAsync(task, ct);
        await uow.SaveChangesAsync(ct);
        return TaskMapper.MapToDto(task);
    }
}

// ── Update Task ───────────────────────────────────────────────────────────────
public record UpdateTaskCommand(Guid TaskId, Guid RequesterId, string Title, string? Description,
    Priority Priority, DateTime? StartDate, DateTime? DueDate, int? StoryPoints, Guid? CategoryId) : IRequest<TaskDto>;

public class UpdateTaskHandler(ITaskRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(UpdateTaskCommand cmd, CancellationToken ct)
    {
        var task = await repo.GetByIdAsync(cmd.TaskId, ct)
            ?? throw new NotFoundException("Task", cmd.TaskId);

        if (task.CreatedBy != cmd.RequesterId && task.AssigneeId != cmd.RequesterId)
            throw new UnauthorizedException("You cannot edit this task.");

        task.Update(cmd.Title, cmd.Description, cmd.Priority, cmd.StartDate, cmd.DueDate, cmd.StoryPoints, cmd.CategoryId);

        await uow.SaveChangesAsync(ct);
        return TaskMapper.MapToDto(task);
    }
}

// ── Complete Task ─────────────────────────────────────────────────────────────
public record CompleteTaskCommand(Guid TaskId, Guid RequesterId) : IRequest<TaskDto>;

public class CompleteTaskHandler(ITaskRepository repo, IProjectRepository projectRepo, IUnitOfWork uow)
    : IRequestHandler<CompleteTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(CompleteTaskCommand cmd, CancellationToken ct)
    {
        var task = await repo.GetByIdAsync(cmd.TaskId, ct)
            ?? throw new NotFoundException("Task", cmd.TaskId);

        task.Complete(cmd.RequesterId);


        if (task.ProjectId.HasValue)
        {
            var project = await projectRepo.GetByIdAsync(task.ProjectId.Value, ct);
            if (project != null)
            {
                var (total, completed) = await repo.GetProjectProgressAsync(task.ProjectId.Value, ct);
                project.UpdateProgress(total, completed);

            }
        }

        await uow.SaveChangesAsync(ct);
        return TaskMapper.MapToDto(task);
    }
}

// ── Change Status ─────────────────────────────────────────────────────────────
public record ChangeTaskStatusCommand(Guid TaskId, Guid RequesterId, TaskStatus NewStatus) : IRequest<TaskDto>;

public class ChangeTaskStatusHandler(ITaskRepository repo, IUnitOfWork uow)
    : IRequestHandler<ChangeTaskStatusCommand, TaskDto>
{
    public async Task<TaskDto> Handle(ChangeTaskStatusCommand cmd, CancellationToken ct)
    {
        var task = await repo.GetByIdAsync(cmd.TaskId, ct)
            ?? throw new NotFoundException("Task", cmd.TaskId);

        if (cmd.NewStatus == TaskStatus.Completed)
            task.Complete(cmd.RequesterId);
        else
            task.ChangeStatus(cmd.NewStatus);


        await uow.SaveChangesAsync(ct);
        return TaskMapper.MapToDto(task);
    }
}

// ── Assign Task ───────────────────────────────────────────────────────────────
public record AssignTaskCommand(Guid TaskId, Guid RequesterId, Guid AssigneeId) : IRequest<TaskDto>;

public class AssignTaskHandler(ITaskRepository repo, IProjectRepository projectRepo,
    IUserRepository userRepo, IUnitOfWork uow) : IRequestHandler<AssignTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(AssignTaskCommand cmd, CancellationToken ct)
    {
        var task = await repo.GetByIdAsync(cmd.TaskId, ct)
            ?? throw new NotFoundException("Task", cmd.TaskId);

        _ = await userRepo.GetByIdAsync(cmd.AssigneeId, ct)
            ?? throw new NotFoundException("User", cmd.AssigneeId);

        if (task.ProjectId.HasValue)
        {
            var project = await projectRepo.GetWithMembersAsync(task.ProjectId.Value, ct);
            if (project != null && !project.IsMember(cmd.AssigneeId))
                throw new BusinessRuleException("Assignee must be a project member.");
        }

        task.Assign(cmd.AssigneeId);

        await uow.SaveChangesAsync(ct);
        return TaskMapper.MapToDto(task);
    }
}

// ── Delete Task ───────────────────────────────────────────────────────────────
public record DeleteTaskCommand(Guid TaskId, Guid RequesterId) : IRequest<Unit>;

public class DeleteTaskHandler(ITaskRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteTaskCommand, Unit>
{
    public async Task<Unit> Handle(DeleteTaskCommand cmd, CancellationToken ct)
    {
        var task = await repo.GetByIdAsync(cmd.TaskId, ct)
            ?? throw new NotFoundException("Task", cmd.TaskId);

        if (task.CreatedBy != cmd.RequesterId)
            throw new UnauthorizedException("Only the task creator can delete it.");

        repo.Remove(task);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── Add Comment ───────────────────────────────────────────────────────────────
public record AddCommentCommand(Guid TaskId, Guid UserId, string Content) : IRequest<CommentDto>;

public class AddCommentHandler(ITaskRepository taskRepo, ICommentRepository commentRepo, IUnitOfWork uow)
    : IRequestHandler<AddCommentCommand, CommentDto>
{
    public async Task<CommentDto> Handle(AddCommentCommand cmd, CancellationToken ct)
    {
        _ = await taskRepo.GetByIdAsync(cmd.TaskId, ct)
            ?? throw new NotFoundException("Task", cmd.TaskId);

        var comment = Comment.Create(cmd.TaskId, cmd.UserId, cmd.Content);
        await commentRepo.AddAsync(comment, ct);
        await uow.SaveChangesAsync(ct);
        return new CommentDto(comment.Id, comment.Content, comment.UserId, string.Empty, false, comment.CreatedAt);
    }
}

// ── Queries ───────────────────────────────────────────────────────────────────
public record GetTaskByIdQuery(Guid TaskId, Guid RequesterId) : IRequest<TaskDetailDto>;

public class GetTaskByIdHandler(ITaskRepository repo, ICommentRepository commentRepo)
    : IRequestHandler<GetTaskByIdQuery, TaskDetailDto>
{
    public async Task<TaskDetailDto> Handle(GetTaskByIdQuery q, CancellationToken ct)
    {
        var task = await repo.GetWithDetailsAsync(q.TaskId, ct)
            ?? throw new NotFoundException("Task", q.TaskId);

        var comments = await commentRepo.GetByTaskAsync(task.Id, ct);
        var subTasks = await repo.GetSubTasksAsync(task.Id, ct);

        var commentDtos = comments
            .Where(c => !c.IsDeleted)
            .Select(c => new CommentDto(c.Id, c.Content, c.UserId,
                c.Author?.Name ?? string.Empty, c.IsEdited, c.CreatedAt));

        var checklistDtos = task.Checklists.Select(cl => new ChecklistDto(
            cl.Id, cl.Title, cl.CompletionPercentage(),
            cl.Items.Select(i => new ChecklistItemDto(i.Id, i.Description, i.IsCompleted, i.CompletedAt))));

        return new TaskDetailDto(TaskMapper.MapToDto(task), subTasks.Select(TaskMapper.MapToDto), commentDtos, checklistDtos);
    }
}

public record GetProjectTasksQuery(Guid ProjectId, Guid RequesterId) : IRequest<IEnumerable<TaskDto>>;

public class GetProjectTasksHandler(ITaskRepository repo, IProjectRepository projectRepo)
    : IRequestHandler<GetProjectTasksQuery, IEnumerable<TaskDto>>
{
    public async Task<IEnumerable<TaskDto>> Handle(GetProjectTasksQuery q, CancellationToken ct)
    {
        var project = await projectRepo.GetWithMembersAsync(q.ProjectId, ct)
            ?? throw new NotFoundException("Project", q.ProjectId);

        if (!project.IsMember(q.RequesterId))
            throw new UnauthorizedException("Access denied.");

        var tasks = await repo.GetByProjectAsync(q.ProjectId, ct);
        return tasks.Select(TaskMapper.MapToDto);
    }
}

public record GetMyTasksQuery(Guid UserId) : IRequest<IEnumerable<TaskDto>>;

public class GetMyTasksHandler(ITaskRepository repo) : IRequestHandler<GetMyTasksQuery, IEnumerable<TaskDto>>
{
    public async Task<IEnumerable<TaskDto>> Handle(GetMyTasksQuery q, CancellationToken ct)
    {
        var tasks = await repo.GetByAssigneeAsync(q.UserId, ct);
        return tasks.Select(TaskMapper.MapToDto);
    }
}

// ── Mapper ────────────────────────────────────────────────────────────────────
internal static class TaskMapper
{
    internal static TaskDto MapToDto(TaskItem t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority,
        t.DueDate, t.CompletedAt, t.EstimatedHours, t.HoursWorked,
        t.StoryPoints, t.IsOverdue(), t.ProjectId, t.ParentTaskId,
        t.AssigneeId, t.Assignee?.Name, t.CreatedAt);
}