using UpTask.Domain.Common;
using UpTask.Domain.Exceptions;

namespace UpTask.Domain.Entities
{
    // ── Checklist ─────────────────────────────────────────────────────────────────
    public sealed class Checklist : BaseEntity
    {
        public Guid TaskId { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public int Order { get; private set; } = 0;
        private readonly List<ChecklistItem> _items = [];
        public IReadOnlyCollection<ChecklistItem> Items => _items.AsReadOnly();

        private Checklist() { }

        public static Checklist Create(Guid taskId, string title)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Checklist title is required.");
            return new Checklist
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                Title = title.Trim(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public ChecklistItem AddItem(string description)
        {
            var item = ChecklistItem.Create(Id, description, _items.Count);
            _items.Add(item);
            SetUpdatedAt();
            return item;
        }

        public int CompletionPercentage()
        {
            if (!_items.Any()) return 0;
            return (int)Math.Floor((double)_items.Count(i => i.IsCompleted) / _items.Count * 100);
        }
    }
}
