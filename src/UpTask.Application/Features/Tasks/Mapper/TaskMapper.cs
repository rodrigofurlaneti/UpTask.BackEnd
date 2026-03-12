using UpTask.Application.Features.Tasks.DTOs;
using UpTask.Application.Features.Categories.DTOs; // Para TagDto
using UpTask.Domain.Entities;
using System.Linq;

namespace UpTask.Application.Features.Tasks.Mapper;

internal static class TaskMapper
{
    // Mudamos o nome para ToDto para ser mais curto e padrão
    internal static TaskDto ToDto(TaskItem t) => new(
        t.Id,
        t.Title.Value,
        t.Description,
        t.Status,
        t.Priority,
        t.DueDate,
        t.CompletedAt,
        t.EstimatedHours,
        t.HoursWorked,
        t.StoryPoints,
        t.IsOverdue(),
        t.ProjectId,
        t.Project?.Name.Value,
        t.ParentTaskId,
        t.AssigneeId,
        t.Assignee?.Name,
        t.CreatedAt);

    internal static TaskDetailDto ToDetailDto(TaskItem t) => new(
        ToDto(t),
        t.SubTasks?.Select(ToDto).ToList() ?? [],
        t.Comments?.Select(c => new CommentDto(
            c.Id,
            c.Content,
            c.UserId,
            c.Author?.Name ?? "Sistema",
            c.IsEdited,
            c.CreatedAt)).ToList() ?? [],
        t.Checklists?.Select(cl => new ChecklistDto(
            cl.Id,
            cl.Title,
            cl.Items.Count, 
            cl.Items.Select(i => new ChecklistItemDto(
                i.Id,
                i.Description,
                i.IsCompleted,
                null
            )).ToList()
        )).ToList() ?? []
    );
}