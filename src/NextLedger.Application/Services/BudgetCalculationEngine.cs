using NextLedger.Application.DTOs;
using NextLedger.Application.Interfaces;
using NextLedger.Domain.Entities;
using NextLedger.Domain.ValueObjects;

namespace NextLedger.Application.Services;

/// <summary>
/// Engine for budget calculations, reports, and analytics.
/// </summary>
public sealed class BudgetCalculationEngine
{
    private readonly IUnitOfWork _unitOfWork;

    public BudgetCalculationEngine(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Calculate "Ready to Assign" - money not yet allocated to envelopes.
    /// </summary>
    public async Task<Money> CalculateReadyToAssignAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);
        var totalAllocated = await _unitOfWork.EnvelopeAllocations.GetTotalAllocatedForPeriodAsync(period.Id, ct);

        // Ready to Assign = Total Income + Carryover - Total Allocated
        return period.TotalIncome + period.CarriedOver - totalAllocated;
    }

    /// <summary>
    /// Calculate total available across all envelopes.
    /// </summary>
    public async Task<Money> CalculateTotalAvailableAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        var period = await _unitOfWork.BudgetPeriods.GetByYearMonthAsync(year, month, ct);
        if (period is null) return Money.Zero;

        var allocations = await _unitOfWork.EnvelopeAllocations.GetByPeriodAsync(period.Id, ct);
        var dateRange = DateRange.ForMonth(year, month);

        var totalAvailable = Money.Zero;
        foreach (var alloc in allocations)
        {
            var spent = await _unitOfWork.Transactions.GetEnvelopeSpentAsync(alloc.EnvelopeId, dateRange, ct);
            var available = alloc.Allocated + alloc.RolloverFromPrevious - spent;
            totalAvailable = totalAvailable + available;
        }

        return totalAvailable;
    }

    /// <summary>
    /// Get spending breakdown by envelope for a period.
    /// </summary>
    public async Task<IReadOnlyList<SpendingByEnvelopeDto>> GetSpendingByEnvelopeAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);
        var envelopes = await _unitOfWork.Envelopes.GetActiveEnvelopesAsync(ct);
        var allocations = await _unitOfWork.EnvelopeAllocations.GetByPeriodAsync(period.Id, ct);
        var dateRange = DateRange.ForMonth(year, month);

        var result = new List<SpendingByEnvelopeDto>();

        foreach (var envelope in envelopes)
        {
            var allocation = allocations.FirstOrDefault(a => a.EnvelopeId == envelope.Id);
            var budgeted = (allocation?.Allocated ?? Money.Zero) + (allocation?.RolloverFromPrevious ?? Money.Zero);
            var spent = await _unitOfWork.Transactions.GetEnvelopeSpentAsync(envelope.Id, dateRange, ct);

            result.Add(new SpendingByEnvelopeDto
            {
                EnvelopeId = envelope.Id,
                EnvelopeName = envelope.Name,
                GroupName = envelope.GroupName,
                Color = envelope.Color,
                Budgeted = budgeted,
                Spent = spent,
                Remaining = budgeted - spent
            });
        }

        return result.OrderBy(e => e.GroupName ?? "").ThenBy(e => e.EnvelopeName).ToList();
    }

    /// <summary>
    /// Get spending breakdown grouped by envelope group.
    /// </summary>
    public async Task<IReadOnlyList<SpendingByGroupDto>> GetSpendingByGroupAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        var byEnvelope = await GetSpendingByEnvelopeAsync(year, month, ct);

        return byEnvelope
            .GroupBy(e => e.GroupName ?? "Uncategorized")
            .Select(g => new SpendingByGroupDto
            {
                GroupName = g.Key,
                TotalBudgeted = g.Aggregate(Money.Zero, (sum, e) => sum + e.Budgeted),
                TotalSpent = g.Aggregate(Money.Zero, (sum, e) => sum + e.Spent),
                TotalRemaining = g.Aggregate(Money.Zero, (sum, e) => sum + e.Remaining),
                Envelopes = g.ToList()
            })
            .OrderBy(g => g.GroupName)
            .ToList();
    }

    /// <summary>
    /// Get income vs expenses for a period.
    /// </summary>
    public async Task<IncomeVsExpenseDto> GetIncomeVsExpenseAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        var dateRange = DateRange.ForMonth(year, month);
        var transactions = await _unitOfWork.Transactions.GetByDateRangeAsync(dateRange, ct);

        var income = transactions
            .Where(t => t.IsInflow && !t.IsTransfer)
            .Aggregate(Money.Zero, (sum, t) => sum + t.Amount);

        var expenses = transactions
            .Where(t => t.IsOutflow && !t.IsTransfer)
            .Aggregate(Money.Zero, (sum, t) => sum + t.Amount.Abs());

        return new IncomeVsExpenseDto
        {
            Year = year,
            Month = month,
            TotalIncome = income,
            TotalExpenses = expenses
        };
    }

    /// <summary>
    /// Get monthly trend data for the last N months.
    /// </summary>
    public async Task<IReadOnlyList<MonthlyTrendDto>> GetMonthlyTrendAsync(
        int months = 12,
        CancellationToken ct = default)
    {
        var result = new List<MonthlyTrendDto>();
        var today = DateTime.Today;

        for (var i = months - 1; i >= 0; i--)
        {
            var date = today.AddMonths(-i);
            var year = date.Year;
            var month = date.Month;

            var incomeVsExpense = await GetIncomeVsExpenseAsync(year, month, ct);

            // Calculate net worth at end of month
            var accounts = await _unitOfWork.Accounts.GetActiveAccountsAsync(ct);
            var netWorth = accounts.Aggregate(Money.Zero, (sum, a) => sum + a.Balance);

            result.Add(new MonthlyTrendDto
            {
                Year = year,
                Month = month,
                Income = incomeVsExpense.TotalIncome,
                Expenses = incomeVsExpense.TotalExpenses,
                NetWorth = netWorth
            });
        }

        return result;
    }

    /// <summary>
    /// Calculate "Age of Money" - average days between earning and spending.
    /// </summary>
    public async Task<AgeOfMoneyDto> CalculateAgeOfMoneyAsync(CancellationToken ct = default)
    {
        // Get on-budget accounts total balance
        var accounts = await _unitOfWork.Accounts.GetOnBudgetAccountsAsync(ct);
        var totalBalance = accounts.Aggregate(Money.Zero, (sum, a) => sum + a.Balance);

        if (totalBalance.IsZero || totalBalance.IsNegative)
        {
            return new AgeOfMoneyDto
            {
                Days = 0,
                Description = "Your age of money is 0 days. Build up a buffer to increase it."
            };
        }

        // Calculate average daily spending over last 30 days
        var dateRange = DateRange.LastNDays(30);
        var transactions = await _unitOfWork.Transactions.GetByDateRangeAsync(dateRange, ct);

        var totalSpent = transactions
            .Where(t => t.IsOutflow && !t.IsTransfer)
            .Aggregate(Money.Zero, (sum, t) => sum + t.Amount.Abs());

        if (totalSpent.IsZero)
        {
            return new AgeOfMoneyDto
            {
                Days = 999,
                Description = "No spending in the last 30 days - your money is very old!"
            };
        }

        var avgDailySpending = totalSpent.Amount / 30m;
        var ageInDays = (int)(totalBalance.Amount / avgDailySpending);

        var description = ageInDays switch
        {
            < 7 => "Your money is very young. Focus on budgeting ahead.",
            < 14 => "Your money is young. Keep building that buffer.",
            < 30 => "Getting better! Keep growing your buffer.",
            < 60 => "Good job! Your money is aging well.",
            < 90 => "Excellent! You're living on last month's money.",
            _ => "Amazing! You have a very healthy financial buffer."
        };

        return new AgeOfMoneyDto
        {
            Days = ageInDays,
            Description = description
        };
    }

    /// <summary>
    /// Check if any envelopes are overspent.
    /// </summary>
    public async Task<IReadOnlyList<SpendingByEnvelopeDto>> GetOverspentEnvelopesAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        var spending = await GetSpendingByEnvelopeAsync(year, month, ct);
        return spending.Where(e => e.Remaining.IsNegative).ToList();
    }

    /// <summary>
    /// Calculate envelope funding progress for goals.
    /// </summary>
    public async Task<IReadOnlyList<EnvelopeDto>> GetGoalProgressAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var envelopes = await _unitOfWork.Envelopes.GetActiveEnvelopesAsync(ct);
        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(today.Year, today.Month, ct);
        var allocations = await _unitOfWork.EnvelopeAllocations.GetByPeriodAsync(period.Id, ct);
        var dateRange = DateRange.ForMonth(today.Year, today.Month);

        var result = new List<EnvelopeDto>();

        foreach (var envelope in envelopes.Where(e => e.HasGoal))
        {
            var allocation = allocations.FirstOrDefault(a => a.EnvelopeId == envelope.Id);
            var budgeted = (allocation?.Allocated ?? Money.Zero) + (allocation?.RolloverFromPrevious ?? Money.Zero);
            var spent = await _unitOfWork.Transactions.GetEnvelopeSpentAsync(envelope.Id, dateRange, ct);

            result.Add(new EnvelopeDto
            {
                Id = envelope.Id,
                Name = envelope.Name,
                GroupName = envelope.GroupName,
                Color = envelope.Color,
                Allocated = budgeted,
                Spent = spent,
                Available = budgeted - spent,
                GoalAmount = envelope.GoalAmount,
                GoalDate = envelope.GoalDate
            });
        }

        return result.OrderBy(e => e.GoalProgress).ToList();
    }

    /// <summary>
    /// Recalculate all balances for a period.
    /// </summary>
    public async Task RecalculatePeriodAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);
        var dateRange = DateRange.ForMonth(year, month);
        var allocations = await _unitOfWork.EnvelopeAllocations.GetByPeriodAsync(period.Id, ct);

        // Recalculate income
        var transactions = await _unitOfWork.Transactions.GetByDateRangeAsync(dateRange, ct);
        var totalIncome = transactions
            .Where(t => t.IsInflow && !t.IsTransfer)
            .Aggregate(Money.Zero, (sum, t) => sum + t.Amount);

        // Recalculate total spent per envelope
        foreach (var alloc in allocations)
        {
            var spent = await _unitOfWork.Transactions.GetEnvelopeSpentAsync(alloc.EnvelopeId, dateRange, ct);
            alloc.UpdateSpent(spent);
            await _unitOfWork.EnvelopeAllocations.UpdateAsync(alloc, ct);
        }

        // Recalculate period totals
        var totalAllocated = allocations.Aggregate(Money.Zero, (sum, a) => sum + a.Allocated);
        var totalSpent = allocations.Aggregate(Money.Zero, (sum, a) => sum + a.Spent);

        period.UpdateIncome(totalIncome);
        period.UpdateAllocated(totalAllocated);
        period.UpdateSpent(totalSpent);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period, ct);
    }
}
