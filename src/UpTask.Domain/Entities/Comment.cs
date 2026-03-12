using UpTask.Domain.Common;
using UpTask.Domain.Exceptions;

namespace UpTask.Domain.Entities
{
    public sealed class Comment : Entity
    {
        public Guid TaskId { get; private set; }
        public Guid UserId { get; private set; }
        public string Content { get; private set; } = string.Empty;
        public bool IsEdited { get; private set; } = false;
        public DateTime? EditedAt { get; private set; }
        public bool IsDeleted { get; private set; } = false;
        public DateTime? DeletedAt { get; private set; }
        public User? Author { get; private set; }

        private Comment() { }

        public static Comment Create(Guid taskId, Guid userId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Comment content is required.");

            return new Comment
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow, // Padronizando para UtcNow
                UpdatedAt = DateTime.UtcNow
            };
        }

        public void Edit(string newContent)
        {
            if (IsDeleted) throw new DomainException("Cannot edit a deleted comment.");

            Content = newContent.Trim();
            IsEdited = true;
            EditedAt = DateTime.UtcNow;

            // ALTERADO: De SetUpdatedAt() para Touch() da nova Entity
            Touch();
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;

            // ALTERADO: De SetUpdatedAt() para Touch()
            Touch();
        }
    }
}