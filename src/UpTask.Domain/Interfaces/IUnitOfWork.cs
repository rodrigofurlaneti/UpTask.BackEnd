using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpTask.Domain.Interfaces
{
    // ── Unit of Work ──────────────────────────────────────────────────────────────
    /// <summary>Abstracts the transaction boundary and dispatches domain events.</summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
