using NextLedger.Domain.ValueObjects;

namespace NextLedger.Application.DTOs;

/// <summary>
/// Spending breakdown by envelope for a period.
/// </summary>
public sealed record SpendingByEnvelopeDto
{
    public required Guid EnvelopeId { get; init; }
    public required string EnvelopeName { get; init; }
    public required string? GroupName { get; init; }
    public required string Color { get; init; }
    public required Money Budgeted { get; init; }
    public required Money Spent { get; init; }
    public required Money Remaining { get; init; }
    public decimal PercentSpent => Budgeted.IsZero ? 0 : Math.Round(Spent.Amount / Budgeted.Amount * 100, 1);
}

/// <summary>
/// Spending breakdown by group.
/// </summary>
public sealed record SpendingByGroupDto
{
    public required string GroupName { get; init; }
    public required Money TotalBudgeted { get; init; }
    public required Money TotalSpent { get; init; }
    public required Money TotalRemaining { get; init; }
    public required IReadOnlyList<SpendingByEnvelopeDto> Envelopes { get; init; }
    public decimal PercentSpent => TotalBudgeted.IsZero ? 0 : Math.Round(TotalSpent.Amount / TotalBudgeted.Amount * 100, 1);
}

/// <summary>
/// Income vs expenses summary.
/// </summary>
public sealed record IncomeVsExpenseDto
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required Money TotalIncome { get; init; }
    public required Money TotalExpenses { get; init; }
    public Money NetIncome => TotalIncome - TotalExpenses;
    public bool IsPositive => NetIncome.IsPositive;
}

/// <summary>
/// Monthly trend data point.
/// </summary>
public sealed record MonthlyTrendDto
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required Money Income { get; init; }
    public required Money Expenses { get; init; }
    public required Money NetWorth { get; init; }
    public string MonthLabel => $"{Year}-{Month:D2}";
}

/// <summary>
/// Age of money calculation result.
/// </summary>
public sealed record AgeOfMoneyDto
{
    public required int Days { get; init; }
    public required string Description { get; init; }
    public bool IsHealthy => Days >= 30;
}
