using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Application.Services;

/// <summary>
/// Recalculates derived totals for a budget period (income/spent/allocated).
/// This provides a single source of truth so totals don't drift across operations.
/// </summary>
public sealed class BudgetPeriodRecalculationService
{
    private readonly IUnitOfWork _unitOfWork;

    public BudgetPeriodRecalculationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task RecalculateAsync(int year, int month, CancellationToken ct = default)
    {
        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);
        var dateRange = DateRange.ForMonth(year, month);

        var (income, spent) = await _unitOfWork.Transactions.GetTotalsForDateRangeAsync(dateRange, ct);
        var allocated = await _unitOfWork.EnvelopeAllocations.GetTotalAllocatedForPeriodAsync(period.Id, ct);

        // Persist per-envelope spent for deterministic reporting/rollover.
        // Note: Period.TotalSpent includes all outflows in the period (including unassigned),
        // while allocation spent only includes envelope-assigned outflows.
        var allocations = await _unitOfWork.EnvelopeAllocations.GetByPeriodAsync(period.Id, ct);
        foreach (var allocation in allocations)
        {
            var envelopeSpent = await _unitOfWork.Transactions.GetEnvelopeSpentAsync(allocation.EnvelopeId, dateRange, ct);
            allocation.UpdateSpent(envelopeSpent);
            await _unitOfWork.EnvelopeAllocations.UpdateAsync(allocation, ct);
        }

        period.UpdateIncome(income);
        period.UpdateSpent(spent);
        period.UpdateAllocated(allocated);

        await _unitOfWork.BudgetPeriods.UpdateAsync(period, ct);
    }
}
