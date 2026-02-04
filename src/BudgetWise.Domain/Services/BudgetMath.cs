using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Domain.Services;

/// <summary>
/// Canonical, deterministic budgeting math.
/// Keep rules here so Application services remain orchestration only.
/// </summary>
public static class BudgetMath
{
    /// <summary>
    /// Ready to assign cash in a period.
    /// = TotalIncome + CarriedOver - TotalAllocated
    /// </summary>
    public static Money ComputeReadyToAssign(Money totalIncome, Money carriedOver, Money totalAllocated)
        => totalIncome + carriedOver - totalAllocated;

    /// <summary>
    /// Envelope available balance.
    /// = Allocated + RolloverFromPrevious - Spent
    /// </summary>
    public static Money ComputeEnvelopeAvailable(Money allocated, Money rolloverFromPrevious, Money spent)
        => allocated + rolloverFromPrevious - spent;

    /// <summary>
    /// Rollover amount for the next period.
    /// Positive carries forward; negative represents overspending debt.
    /// </summary>
    public static Money ComputeRollover(Money available)
        => available;
}
