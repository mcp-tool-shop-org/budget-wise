using BudgetWise.Domain.Entities;
using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Application.Interfaces;

/// <summary>
/// Repository for Transaction entities with specialized queries.
/// </summary>
public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IReadOnlyList<Transaction>> GetByAccountAsync(Guid accountId, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetByEnvelopeAsync(Guid envelopeId, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateRange range, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetByAccountAndDateRangeAsync(Guid accountId, DateRange range, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetUnclearedAsync(Guid accountId, CancellationToken ct = default);
    Task<IReadOnlyList<Transaction>> GetUnassignedAsync(CancellationToken ct = default);
    Task<Money> GetAccountBalanceAsync(Guid accountId, CancellationToken ct = default);
    Task<Money> GetAccountClearedBalanceAsync(Guid accountId, CancellationToken ct = default);
    Task<Money> GetEnvelopeSpentAsync(Guid envelopeId, DateRange range, CancellationToken ct = default);

    /// <summary>
    /// Returns income and spent totals for the given date range.
    /// Income includes inflows only (excludes transfers).
    /// Spent includes outflows only and is returned as a positive amount.
    /// </summary>
    Task<(Money Income, Money Spent)> GetTotalsForDateRangeAsync(DateRange range, CancellationToken ct = default);
}
