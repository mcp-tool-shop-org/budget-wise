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

    // XRPL-specific fields (only populated for ExternalXrpl accounts)
    /// <summary>
    /// The external ledger address (e.g., XRPL r-address). Read-only.
    /// </summary>
    public string? ExternalAddress { get; init; }

    /// <summary>
    /// The network (mainnet/testnet).
    /// </summary>
    public string? ExternalNetwork { get; init; }

    /// <summary>
    /// Last successful external sync timestamp.
    /// </summary>
    public DateTime? LastExternalSyncAt { get; init; }

    /// <summary>
    /// For XRPL: the reserve amount in drops.
    /// </summary>
    public long? ExternalReserveDrops { get; init; }

    /// <summary>
    /// Whether this is an externally reconciled account (read-only).
    /// </summary>
    public bool IsExternalLedger => Type == AccountType.ExternalXrpl;

    public string TypeDisplayName => Type switch
    {
        AccountType.Checking => "Checking",
        AccountType.Savings => "Savings",
        AccountType.CreditCard => "Credit Card",
        AccountType.Cash => "Cash",
        AccountType.LineOfCredit => "Line of Credit",
        AccountType.Investment => "Investment",
        AccountType.ExternalXrpl => "XRPL (External)",
        _ => "Other"
    };

    /// <summary>
    /// Formatted balance text for display.
    /// </summary>
    public string BalanceText => IsExternalLedger
        ? $"{Balance.Amount:N6} XRP"
        : Balance.ToString();

    /// <summary>
    /// For XRPL: calculates the spendable balance (total - reserve).
    /// </summary>
    public decimal? SpendableXrpBalance => IsExternalLedger && ExternalReserveDrops.HasValue
        ? Math.Max(0, Balance.Amount - (ExternalReserveDrops.Value / 1_000_000m))
        : null;

    /// <summary>
    /// For XRPL: the reserve amount in XRP.
    /// </summary>
    public decimal? ReserveXrp => ExternalReserveDrops.HasValue
        ? ExternalReserveDrops.Value / 1_000_000m
        : null;
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

// ============================================================================
// XRPL-Specific DTOs
// ============================================================================

/// <summary>
/// Request to track an XRPL address. Read-only, non-custodial.
/// This is NOT a wallet connection - NextLedger only observes, never controls.
/// </summary>
public sealed record TrackXrplAddressRequest
{
    /// <summary>
    /// Display name for the tracked address.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The XRPL r-address (public address only, no private keys!).
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Network: "mainnet" or "testnet".
    /// </summary>
    public string Network { get; init; } = "mainnet";
}

/// <summary>
/// Result of fetching XRPL account info.
/// </summary>
public sealed record XrplAccountInfoResult
{
    /// <summary>
    /// Whether the fetch was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if fetch failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The XRPL address queried.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Balance in drops (1 XRP = 1,000,000 drops).
    /// </summary>
    public long? BalanceDrops { get; init; }

    /// <summary>
    /// Balance in XRP.
    /// </summary>
    public decimal? BalanceXrp => BalanceDrops.HasValue ? BalanceDrops.Value / 1_000_000m : null;

    /// <summary>
    /// Owner count (affects reserve calculation).
    /// </summary>
    public int? OwnerCount { get; init; }

    /// <summary>
    /// Sequence number of last transaction.
    /// </summary>
    public long? Sequence { get; init; }

    /// <summary>
    /// Ledger index this data was fetched from.
    /// </summary>
    public long? LedgerIndex { get; init; }

    /// <summary>
    /// When this data was fetched.
    /// </summary>
    public DateTime FetchedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this account is activated (has XRP).
    /// </summary>
    public bool IsActivated => Success && BalanceDrops.HasValue;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static XrplAccountInfoResult Ok(
        string address,
        long balanceDrops,
        int ownerCount,
        long sequence,
        long ledgerIndex) => new()
        {
            Success = true,
            Address = address,
            BalanceDrops = balanceDrops,
            OwnerCount = ownerCount,
            Sequence = sequence,
            LedgerIndex = ledgerIndex,
            FetchedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static XrplAccountInfoResult Fail(string address, string errorMessage) => new()
    {
        Success = false,
        Address = address,
        ErrorMessage = errorMessage,
        FetchedAt = DateTime.UtcNow
    };
}

/// <summary>
/// XRPL reserve calculation result.
/// </summary>
public sealed record XrplReserveInfo
{
    /// <summary>
    /// Base reserve in drops (currently 10 XRP = 10,000,000 drops on mainnet).
    /// </summary>
    public required long BaseReserveDrops { get; init; }

    /// <summary>
    /// Owner reserve per object in drops (currently 2 XRP = 2,000,000 drops on mainnet).
    /// </summary>
    public required long OwnerReserveDrops { get; init; }

    /// <summary>
    /// Number of owned objects (trust lines, offers, etc.).
    /// </summary>
    public required int OwnerCount { get; init; }

    /// <summary>
    /// Total reserve in drops.
    /// </summary>
    public long TotalReserveDrops => BaseReserveDrops + (OwnerReserveDrops * OwnerCount);

    /// <summary>
    /// Total reserve in XRP.
    /// </summary>
    public decimal TotalReserveXrp => TotalReserveDrops / 1_000_000m;

    /// <summary>
    /// Base reserve in XRP.
    /// </summary>
    public decimal BaseReserveXrp => BaseReserveDrops / 1_000_000m;

    /// <summary>
    /// Owner reserve in XRP.
    /// </summary>
    public decimal OwnerReserveXrp => OwnerReserveDrops / 1_000_000m;

    /// <summary>
    /// Human-readable explanation of the reserve.
    /// </summary>
    public string Explanation => OwnerCount > 0
        ? $"Base reserve ({BaseReserveXrp:N0} XRP) + {OwnerCount} owned objects × {OwnerReserveXrp:N0} XRP = {TotalReserveXrp:N0} XRP locked"
        : $"Base reserve: {BaseReserveXrp:N0} XRP (required to activate account)";
}

/// <summary>
/// XRPL network status.
/// </summary>
public sealed record XrplNetworkStatus
{
    /// <summary>
    /// Whether the XRPL client is configured and reachable.
    /// </summary>
    public required bool IsConnected { get; init; }

    /// <summary>
    /// Error message if not connected.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The RPC endpoint being used.
    /// </summary>
    public string? Endpoint { get; init; }

    /// <summary>
    /// Network identifier from server.
    /// </summary>
    public string? NetworkId { get; init; }

    /// <summary>
    /// Server version.
    /// </summary>
    public string? ServerVersion { get; init; }

    /// <summary>
    /// Current validated ledger index.
    /// </summary>
    public long? ValidatedLedgerIndex { get; init; }

    /// <summary>
    /// When this status was checked.
    /// </summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Current base reserve from network.
    /// </summary>
    public long? BaseReserveDrops { get; init; }

    /// <summary>
    /// Current owner reserve from network.
    /// </summary>
    public long? OwnerReserveDrops { get; init; }

    /// <summary>
    /// Creates a connected status.
    /// </summary>
    public static XrplNetworkStatus Connected(
        string endpoint,
        string? networkId,
        string? serverVersion,
        long validatedLedgerIndex,
        long baseReserve,
        long ownerReserve) => new()
        {
            IsConnected = true,
            Endpoint = endpoint,
            NetworkId = networkId,
            ServerVersion = serverVersion,
            ValidatedLedgerIndex = validatedLedgerIndex,
            BaseReserveDrops = baseReserve,
            OwnerReserveDrops = ownerReserve,
            CheckedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Creates a disconnected status.
    /// </summary>
    public static XrplNetworkStatus Disconnected(string errorMessage) => new()
    {
        IsConnected = false,
        ErrorMessage = errorMessage,
        CheckedAt = DateTime.UtcNow
    };
}
