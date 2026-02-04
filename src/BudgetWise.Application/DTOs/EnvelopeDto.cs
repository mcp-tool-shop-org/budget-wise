using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Application.DTOs;

/// <summary>
/// Envelope with calculated balances for display.
/// </summary>
public sealed record EnvelopeDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? GroupName { get; init; }
    public required string Color { get; init; }
    public required Money Allocated { get; init; }
    public required Money Spent { get; init; }
    public required Money Available { get; init; }
    public Money? GoalAmount { get; init; }
    public DateOnly? GoalDate { get; init; }
    public bool IsOverspent => Available.IsNegative;
    public bool HasGoal => GoalAmount.HasValue && !GoalAmount.Value.IsZero;

    public decimal GoalProgress
    {
        get
        {
            if (!HasGoal || GoalAmount!.Value.IsZero)
                return 100m;
            return Math.Min(100m, Math.Max(0m, Available.Amount / GoalAmount.Value.Amount * 100m));
        }
    }
}

/// <summary>
/// Request to allocate money to an envelope.
/// </summary>
public sealed record AllocateToEnvelopeRequest
{
    public required Guid EnvelopeId { get; init; }
    public required Money Amount { get; init; }
}

/// <summary>
/// Request to adjust an envelope allocation by a delta (positive or negative).
/// </summary>
public sealed record AdjustEnvelopeAllocationRequest
{
    public required Guid EnvelopeId { get; init; }
    public required Money Delta { get; init; }
}

/// <summary>
/// Request to move money between envelopes.
/// </summary>
public sealed record MoveMoneyRequest
{
    public required Guid FromEnvelopeId { get; init; }
    public required Guid ToEnvelopeId { get; init; }
    public required Money Amount { get; init; }
}

/// <summary>
/// Request to set a goal on an envelope.
/// </summary>
public sealed record SetGoalRequest
{
    public required Guid EnvelopeId { get; init; }
    public required Money Amount { get; init; }
    public DateOnly? TargetDate { get; init; }
}

/// <summary>
/// Summary of budget state for a period.
/// </summary>
public sealed record BudgetSummaryDto
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required bool IsClosed { get; init; }
    public required Money CarriedOver { get; init; }
    public required Money TotalIncome { get; init; }
    public required Money TotalAllocated { get; init; }
    public required Money TotalSpent { get; init; }
    public required Money ReadyToAssign { get; init; }
    public required IReadOnlyList<EnvelopeDto> Envelopes { get; init; }
    public bool IsOverbudgeted => ReadyToAssign.IsNegative;
}
