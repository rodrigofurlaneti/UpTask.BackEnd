using UpTask.Domain.Common;
using UpTask.Domain.Exceptions;

namespace UpTask.Domain.Entities
{
    // ── ChecklistItem ─────────────────────────────────────────────────────────────
    public sealed class ChecklistItem : BaseEntity
    {
        public Guid ChecklistId { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public bool IsCompleted { get; private set; } = false;
        public Guid? CompletedBy { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public int Order { get; private set; }

        private ChecklistItem() { }

        public static ChecklistItem Create(Guid checklistId, string description, int order)
        {
            if (string.IsNullOrWhiteSpace(description)) throw new DomainException("Item description is required.");
            return new ChecklistItem
            {
                Id = Guid.NewGuid(),
                ChecklistId = checklistId,
                Description = description.Trim(),
                Order = order,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public void Complete(Guid userId)
        {
            IsCompleted = true; CompletedBy = userId; CompletedAt = DateTime.UtcNow;
            SetUpdatedAt();
        }

        public void Uncomplete()
        {
            IsCompleted = false; CompletedBy = null; CompletedAt = null;
            SetUpdatedAt();
        }
    }
}
