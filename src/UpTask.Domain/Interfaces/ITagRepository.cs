using UpTask.Domain.Entities;
namespace UpTask.Domain.Interfaces
{
    public interface ITagRepository : IRepository<Tag>
    {
        Task<IEnumerable<Tag>> GetByUserAsync(Guid userId, CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid userId, string name, CancellationToken ct = default);
    }
}
