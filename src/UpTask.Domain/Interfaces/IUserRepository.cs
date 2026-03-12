using UpTask.Domain.Entities;

namespace UpTask.Domain.Interfaces
{
    // ── Specific Repositories ─────────────────────────────────────────────────────
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
        Task<User?> GetWithSettingsAsync(Guid id, CancellationToken ct = default);
    }
}
