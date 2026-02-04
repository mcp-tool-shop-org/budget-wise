using NextLedger.Domain.Entities;
using NextLedger.Domain.ValueObjects;

namespace NextLedger.Application.Interfaces;

/// <summary>
/// Repository for EnvelopeAllocation entities with specialized queries.
/// </summary>
public interface IEnvelopeAllocationRepository : IRepository<EnvelopeAllocation>
{
    Task<EnvelopeAllocation?> GetByEnvelopeAndPeriodAsync(Guid envelopeId, Guid budgetPeriodId, CancellationToken ct = default);
    Task<IReadOnlyList<EnvelopeAllocation>> GetByPeriodAsync(Guid budgetPeriodId, CancellationToken ct = default);
    Task<IReadOnlyList<EnvelopeAllocation>> GetByEnvelopeAsync(Guid envelopeId, CancellationToken ct = default);
    Task<EnvelopeAllocation> GetOrCreateAsync(Guid envelopeId, Guid budgetPeriodId, CancellationToken ct = default);
    Task<Money> GetTotalAllocatedForPeriodAsync(Guid budgetPeriodId, CancellationToken ct = default);
}
