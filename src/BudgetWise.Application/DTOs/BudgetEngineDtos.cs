using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Application.DTOs;

/// <summary>
/// Aggregate budget state for a single period.
/// This is the primary DTO for UI/reporting in Phase 3+.
/// </summary>
public sealed record BudgetSnapshotDto
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public bool IsClosed { get; init; }

    public required Money CarriedOver { get; init; }
    public required Money TotalIncome { get; init; }
    public required Money TotalAllocated { get; init; }
    public required Money TotalSpent { get; init; }

    /// <summary>
    /// Money available to allocate in the period.
    /// </summary>
    public required Money ReadyToAssign { get; init; }
}

/// <summary>
/// Records an allocation change for an envelope.
/// </summary>
public sealed record AllocationChangeDto
{
    public required Guid EnvelopeId { get; init; }
    public string? EnvelopeName { get; init; }

    public required Money BeforeAllocated { get; init; }
    public required Money AfterAllocated { get; init; }

    public Money Delta => AfterAllocated - BeforeAllocated;
}

/// <summary>
/// Structured error for budget operations.
/// Useful for UI mapping and stable automation later.
/// </summary>
public sealed record BudgetOperationError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? Target { get; init; }

    public static BudgetOperationError Create(string code, string message, string? target = null)
        => new() { Code = code, Message = message, Target = target };
}

/// <summary>
/// Standard result wrapper for budget operations.
/// </summary>
public record BudgetOperationResult
{
    public required bool Success { get; init; }
    public required IReadOnlyList<BudgetOperationError> Errors { get; init; }

    public BudgetSnapshotDto? Snapshot { get; init; }
    public IReadOnlyList<AllocationChangeDto> AllocationChanges { get; init; } = Array.Empty<AllocationChangeDto>();

    public static BudgetOperationResult Ok(BudgetSnapshotDto? snapshot = null, IReadOnlyList<AllocationChangeDto>? changes = null)
        => new()
        {
            Success = true,
            Errors = Array.Empty<BudgetOperationError>(),
            Snapshot = snapshot,
            AllocationChanges = changes ?? Array.Empty<AllocationChangeDto>()
        };

    public static BudgetOperationResult Fail(params BudgetOperationError[] errors)
        => new()
        {
            Success = false,
            Errors = errors ?? Array.Empty<BudgetOperationError>(),
            Snapshot = null,
            AllocationChanges = Array.Empty<AllocationChangeDto>()
        };
}

/// <summary>
/// Budget operation result with a typed payload (e.g., created entity or computed model).
/// </summary>
public sealed record BudgetOperationResult<T> : BudgetOperationResult
{
    public T? Value { get; init; }

    public static BudgetOperationResult<T> Ok(T value, BudgetSnapshotDto? snapshot = null, IReadOnlyList<AllocationChangeDto>? changes = null)
        => new()
        {
            Success = true,
            Errors = Array.Empty<BudgetOperationError>(),
            Snapshot = snapshot,
            AllocationChanges = changes ?? Array.Empty<AllocationChangeDto>(),
            Value = value
        };

    public static new BudgetOperationResult<T> Fail(params BudgetOperationError[] errors)
        => new()
        {
            Success = false,
            Errors = errors ?? Array.Empty<BudgetOperationError>(),
            Snapshot = null,
            AllocationChanges = Array.Empty<AllocationChangeDto>(),
            Value = default
        };
}

public enum AutoAssignMode
{
    EarliestGoalDateFirst = 0,
    SmallestGoalFirst = 1
}

/// <summary>
/// Request to auto-assign available funds toward envelope goals.
/// Engine feature in Phase 2+.
/// </summary>
public sealed record AutoAssignToGoalsRequest
{
    public AutoAssignMode Mode { get; init; } = AutoAssignMode.EarliestGoalDateFirst;
}
