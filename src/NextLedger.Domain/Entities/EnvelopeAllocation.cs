using NextLedger.Domain.Common;
using NextLedger.Domain.ValueObjects;

namespace NextLedger.Domain.Entities;

/// <summary>
/// Represents money allocated to an envelope for a specific budget period.
/// This is the core of envelope budgeting - allocating income to categories.
/// </summary>
public class EnvelopeAllocation : Entity
{
    public Guid EnvelopeId { get; private set; }
    public Guid BudgetPeriodId { get; private set; }
    public Money Allocated { get; private set; }
    public Money RolloverFromPrevious { get; private set; }
    public Money Spent { get; private set; }

    private EnvelopeAllocation() : base()
    {
        Allocated = Money.Zero;
        RolloverFromPrevious = Money.Zero;
        Spent = Money.Zero;
    }

    public static EnvelopeAllocation Create(
        Guid envelopeId,
        Guid budgetPeriodId,
        Money? initialAllocation = null,
        Money? rollover = null)
    {
        return new EnvelopeAllocation
        {
            EnvelopeId = envelopeId,
            BudgetPeriodId = budgetPeriodId,
            Allocated = initialAllocation ?? Money.Zero,
            RolloverFromPrevious = rollover ?? Money.Zero,
            Spent = Money.Zero
        };
    }

    /// <summary>
    /// Available balance in this envelope for this period.
    /// = Allocated + Rollover - Spent
    /// </summary>
    public Money Available => Allocated + RolloverFromPrevious - Spent;

    /// <summary>
    /// Total budgeted for this period (allocation + rollover).
    /// </summary>
    public Money TotalBudgeted => Allocated + RolloverFromPrevious;

    /// <summary>
    /// Is this envelope overspent?
    /// </summary>
    public bool IsOverspent => Available.IsNegative;

    /// <summary>
    /// Is this envelope fully funded (goal met)?
    /// </summary>
    public bool IsFunded(Envelope envelope)
    {
        if (!envelope.HasGoal)
            return true;

        return Available >= envelope.GoalAmount!.Value;
    }

    /// <summary>
    /// Percentage of goal funded (0-100+).
    /// </summary>
    public decimal FundedPercentage(Envelope envelope)
    {
        if (!envelope.HasGoal || envelope.GoalAmount!.Value.IsZero)
            return 100m;

        return Math.Round(Available.Amount / envelope.GoalAmount.Value.Amount * 100, 1);
    }

    public void SetAllocation(Money amount)
    {
        if (amount.IsNegative)
            throw new ArgumentException("Allocation cannot be negative.", nameof(amount));

        Allocated = amount;
        Touch();
    }

    public void AddToAllocation(Money amount)
    {
        var newAllocation = Allocated + amount;
        if (newAllocation.IsNegative)
            throw new InvalidOperationException("Cannot reduce allocation below zero.");

        Allocated = newAllocation;
        Touch();
    }

    public void SetRollover(Money amount)
    {
        RolloverFromPrevious = amount;
        Touch();
    }

    public void UpdateSpent(Money spent)
    {
        Spent = spent;
        Touch();
    }

    /// <summary>
    /// Move money from this envelope to another.
    /// </summary>
    public void MoveTo(EnvelopeAllocation target, Money amount)
    {
        if (amount.IsNegative)
            throw new ArgumentException("Amount to move must be positive.", nameof(amount));

        if (amount > Available)
            throw new InvalidOperationException("Cannot move more than available balance.");

        // Reduce from this envelope's allocation
        Allocated = Allocated - amount;
        Touch();

        // Add to target envelope's allocation
        target.Allocated = target.Allocated + amount;
        target.Touch();
    }

    /// <summary>
    /// Calculate rollover amount for next period.
    /// Positive = carry forward, Negative = overspending debt
    /// </summary>
    public Money CalculateRollover() => Available;
}
