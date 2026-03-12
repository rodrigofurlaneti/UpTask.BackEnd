using UpTask.Domain.Entities;

namespace UpTask.Domain.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<IEnumerable<Category>> GetGlobalAsync(CancellationToken ct = default);
        Task<IEnumerable<Category>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    }
}
