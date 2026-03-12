using UpTask.Domain.Entities;
namespace UpTask.Domain.Interfaces
{
    public interface ICommentRepository : IRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetByTaskAsync(Guid taskId, CancellationToken ct = default);
    }
}
