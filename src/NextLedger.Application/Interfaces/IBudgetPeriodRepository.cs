using NextLedger.Domain.Entities;

namespace NextLedger.Application.Interfaces;

/// <summary>
/// Repository for BudgetPeriod entities with specialized queries.
/// </summary>
public interface IBudgetPeriodRepository : IRepository<BudgetPeriod>
{
    Task<BudgetPeriod?> GetByYearMonthAsync(int year, int month, CancellationToken ct = default);
    Task<BudgetPeriod?> GetCurrentPeriodAsync(CancellationToken ct = default);
    Task<BudgetPeriod?> GetPreviousPeriodAsync(int year, int month, CancellationToken ct = default);
    Task<IReadOnlyList<BudgetPeriod>> GetByYearAsync(int year, CancellationToken ct = default);
    Task<BudgetPeriod> GetOrCreateAsync(int year, int month, CancellationToken ct = default);
}
