using NextLedger.Domain.Enums;
using NextLedger.Domain.ValueObjects;

namespace NextLedger.Application.DTOs;

/// <summary>
/// Account for display with balance information.
/// </summary>
public sealed record AccountDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required AccountType Type { get; init; }
    public required Money Balance { get; init; }
    public required Money ClearedBalance { get; init; }
    public required Money UnclearedBalance { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsOnBudget { get; init; }
    public DateTime? LastReconciledAt { get; init; }
    public string TypeDisplayName => Type switch
    {
        AccountType.Checking => "Checking",
        AccountType.Savings => "Savings",
        AccountType.CreditCard => "Credit Card",
        AccountType.Cash => "Cash",
        AccountType.LineOfCredit => "Line of Credit",
        AccountType.Investment => "Investment",
        _ => "Other"
    };
}

/// <summary>
/// Request to create a new account.
/// </summary>
public sealed record CreateAccountRequest
{
    public required string Name { get; init; }
    public required AccountType Type { get; init; }
    public Money? InitialBalance { get; init; }
    public bool IsOnBudget { get; init; } = true;
}

/// <summary>
/// Request to update an account.
/// </summary>
public sealed record UpdateAccountRequest
{
    public required Guid Id { get; init; }
    public string? Name { get; init; }
    public bool? IsOnBudget { get; init; }
    public string? Note { get; init; }
}

/// <summary>
/// Summary of reconciliation state.
/// </summary>
public sealed record ReconciliationDto
{
    public required Guid AccountId { get; init; }
    public required string AccountName { get; init; }
    public required Money StatementBalance { get; init; }
    public required Money ClearedBalance { get; init; }
    public required Money Difference { get; init; }
    public required int UnclearedCount { get; init; }
    public bool IsBalanced => Difference.IsZero;
}

/// <summary>
/// Summary of all account balances.
/// </summary>
public sealed record AccountsSummaryDto
{
    public required Money TotalOnBudget { get; init; }
    public required Money TotalOffBudget { get; init; }
    public required Money TotalAssets { get; init; }
    public required Money TotalLiabilities { get; init; }
    public required Money NetWorth { get; init; }
    public required IReadOnlyList<AccountDto> Accounts { get; init; }
}
