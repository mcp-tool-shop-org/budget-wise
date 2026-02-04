using BudgetWise.Domain.Enums;
using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Application.DTOs;

/// <summary>
/// Transaction for display with related names.
/// </summary>
public sealed record TransactionDto
{
    public required Guid Id { get; init; }
    public required Guid AccountId { get; init; }
    public required string AccountName { get; init; }
    public Guid? EnvelopeId { get; init; }
    public string? EnvelopeName { get; init; }
    public required DateOnly Date { get; init; }
    public required Money Amount { get; init; }
    public required string Payee { get; init; }
    public string? Memo { get; init; }
    public required TransactionType Type { get; init; }
    public required bool IsCleared { get; init; }
    public required bool IsReconciled { get; init; }
    public IReadOnlyList<TransactionSplitLineDto> SplitLines { get; init; } = Array.Empty<TransactionSplitLineDto>();
    public bool HasSplits => SplitLines.Count > 0;
    public bool IsTransfer => Type == TransactionType.Transfer;
    public bool IsInflow => Amount.IsPositive;
    public bool IsOutflow => Amount.IsNegative;
    public Money AbsoluteAmount => Amount.Abs();
}

public sealed record TransactionSplitLineDto
{
    public required Guid EnvelopeId { get; init; }
    public required string EnvelopeName { get; init; }
    public required Money Amount { get; init; }
    public required int SortOrder { get; init; }
}

/// <summary>
/// Result payload for a transfer operation (outflow + inflow pair).
/// </summary>
public sealed record TransferResultDto
{
    public required TransactionDto From { get; init; }
    public required TransactionDto To { get; init; }
}

/// <summary>
/// Request to create an outflow (expense) transaction.
/// </summary>
public sealed record CreateOutflowRequest
{
    public required Guid AccountId { get; init; }
    public required DateOnly Date { get; init; }
    public required Money Amount { get; init; }
    public required string Payee { get; init; }
    public Guid? EnvelopeId { get; init; }
    /// <summary>
    /// Optional split allocations. If provided and non-empty, EnvelopeId must be null.
    /// Amount must equal the sum of split line amounts.
    /// </summary>
    public IReadOnlyList<TransactionSplitLineRequest>? SplitLines { get; init; }
    public string? Memo { get; init; }
}

/// <summary>
/// Request to create an inflow (income) transaction.
/// </summary>
public sealed record CreateInflowRequest
{
    public required Guid AccountId { get; init; }
    public required DateOnly Date { get; init; }
    public required Money Amount { get; init; }
    public required string Payee { get; init; }
    public string? Memo { get; init; }
}

/// <summary>
/// Request to create a transfer between accounts.
/// </summary>
public sealed record CreateTransferRequest
{
    public required Guid FromAccountId { get; init; }
    public required Guid ToAccountId { get; init; }
    public required DateOnly Date { get; init; }
    public required Money Amount { get; init; }
    public string? Memo { get; init; }
}

/// <summary>
/// Request to update a transaction.
/// </summary>
public sealed record UpdateTransactionRequest
{
    public required Guid Id { get; init; }
    public DateOnly? Date { get; init; }
    public Money? Amount { get; init; }
    public string? Payee { get; init; }
    public Guid? EnvelopeId { get; init; }
    /// <summary>
    /// Optional split allocations. If provided, replaces existing split lines.
    /// If non-empty, EnvelopeId must be null.
    /// </summary>
    public IReadOnlyList<TransactionSplitLineRequest>? SplitLines { get; init; }
    public string? Memo { get; init; }
}

public sealed record TransactionSplitLineRequest
{
    public required Guid EnvelopeId { get; init; }
    public required Money Amount { get; init; }
}

/// <summary>
/// Filters for querying transactions.
/// </summary>
public sealed record TransactionFilterDto
{
    public Guid? AccountId { get; init; }
    public Guid? EnvelopeId { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? PayeeSearch { get; init; }
    public bool? IsCleared { get; init; }
    public bool? IsUnassigned { get; init; }
    public int PageSize { get; init; } = 50;
    public int PageNumber { get; init; } = 1;
}

/// <summary>
/// Request to reconcile an account to a statement ending balance.
/// The engine will compute the difference between the cleared balance and statement.
/// If the difference is zero, selected transactions are marked reconciled.
/// Optionally, an adjustment transaction can be created to force a match.
/// </summary>
public sealed record ReconcileAccountRequest
{
    public required Guid AccountId { get; init; }
    public required DateOnly StatementDate { get; init; }
    public required Money StatementEndingBalance { get; init; }

    /// <summary>
    /// Transactions the user intends to reconcile as part of this statement.
    /// </summary>
    public required IReadOnlyList<Guid> TransactionIdsToReconcile { get; init; }

    /// <summary>
    /// If true, the engine will create an explicit adjustment transaction when there is a difference.
    /// </summary>
    public bool CreateAdjustmentIfNeeded { get; init; } = false;
}

public sealed record ReconcileAccountResultDto
{
    public required Guid AccountId { get; init; }
    public required DateOnly StatementDate { get; init; }

    public required Money StatementEndingBalance { get; init; }
    public required Money ClearedBalance { get; init; }

    /// <summary>
    /// StatementEndingBalance - ClearedBalance.
    /// </summary>
    public required Money Difference { get; init; }

    public int ReconciledTransactionCount { get; init; }

    /// <summary>
    /// Present only if an adjustment was created.
    /// </summary>
    public TransactionDto? AdjustmentTransaction { get; init; }
}
