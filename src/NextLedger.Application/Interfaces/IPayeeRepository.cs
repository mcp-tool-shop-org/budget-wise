using NextLedger.Domain.Entities;

namespace NextLedger.Application.Interfaces;

/// <summary>
/// Repository for Payee entities with specialized queries.
/// </summary>
public interface IPayeeRepository : IRepository<Payee>
{
    Task<Payee?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Payee> GetOrCreateAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Payee>> SearchAsync(string query, int limit = 10, CancellationToken ct = default);
    Task<IReadOnlyList<Payee>> GetRecentAsync(int limit = 10, CancellationToken ct = default);
    Task<IReadOnlyList<Payee>> GetVisibleAsync(CancellationToken ct = default);
}
