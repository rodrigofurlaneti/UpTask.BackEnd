using UpTask.Domain.Common;
using UpTask.Domain.Enums;
using UpTask.Domain.Exceptions;

namespace UpTask.Domain.Entities
{
    // ── TaskDependency ────────────────────────────────────────────────────────────
    public sealed class TaskDependency : BaseEntity
    {
        public Guid TaskId { get; private set; }
        public Guid DependsOnId { get; private set; }
        public DependencyType Type { get; private set; }

        private TaskDependency() { }

        public static TaskDependency Create(Guid taskId, Guid dependsOnId, DependencyType type)
        {
            if (taskId == dependsOnId) throw new DomainException("A task cannot depend on itself.");
            return new TaskDependency
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                DependsOnId = dependsOnId,
                Type = type,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
    }
}
