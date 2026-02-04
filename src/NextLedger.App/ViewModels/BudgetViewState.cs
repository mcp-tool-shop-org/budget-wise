using NextLedger.Application.DTOs;

namespace NextLedger.App.ViewModels;

/// <summary>
/// Single source of truth for the Budget screen.
/// Phase 3 guardrail: UI state updates only from <see cref="NextLedger.Application.Interfaces.IBudgetEngine"/> responses.
/// </summary>
public sealed record BudgetViewState
{
    public required int Year { get; init; }
    public required int Month { get; init; }

    public BudgetSnapshotDto? Snapshot { get; init; }

    public BudgetSummaryDto? Summary { get; init; }

    public bool IsLoading { get; init; }

    public IReadOnlyList<BudgetOperationError> Errors { get; init; } = Array.Empty<BudgetOperationError>();

    public static BudgetViewState Empty(int year, int month)
        => new()
        {
            Year = year,
            Month = month,
            Snapshot = null,
            Summary = null,
            IsLoading = false,
            Errors = Array.Empty<BudgetOperationError>()
        };
}
