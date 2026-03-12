namespace UpTask.Domain.Entities
{
    // ── TaskTag (join) ────────────────────────────────────────────────────────────
    public sealed class TaskTag
    {
        public Guid TaskId { get; private set; }
        public Guid TagId { get; private set; }
        public Tag? Tag { get; private set; }

        private TaskTag() { }
        public static TaskTag Create(Guid taskId, Guid tagId) => new() { TaskId = taskId, TagId = tagId };
    }
}
