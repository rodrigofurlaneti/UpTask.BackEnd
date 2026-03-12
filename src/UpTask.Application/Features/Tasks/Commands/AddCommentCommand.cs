using MediatR;
using UpTask.Domain.Entities;
using UpTask.Domain.Exceptions;
using UpTask.Domain.Interfaces;
using UpTask.Application.Features.Tasks.DTOs;

namespace UpTask.Application.Features.Tasks.Commands
{
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
}
