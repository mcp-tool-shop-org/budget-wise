using NextLedger.Domain.Entities;

namespace NextLedger.Application.Interfaces;

/// <summary>
/// Repository for Envelope entities with specialized queries.
/// </summary>
public interface IEnvelopeRepository : IRepository<Envelope>
{
    Task<IReadOnlyList<Envelope>> GetActiveEnvelopesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Envelope>> GetByGroupAsync(string groupName, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetGroupNamesAsync(CancellationToken ct = default);
    Task<Envelope?> GetByNameAsync(string name, CancellationToken ct = default);
}
