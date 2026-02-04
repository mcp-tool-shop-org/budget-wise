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

// ============================================================================
// XRPL Interpretation Layer DTOs
// ============================================================================

/// <summary>
/// Represents a single XRPL transaction from the on-chain history.
/// Read-only, no editing, no categorization into envelopes.
/// </summary>
public sealed record XrplTransactionDto
{
    /// <summary>
    /// The transaction hash (unique identifier on-chain).
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    /// When this transaction was validated on-chain.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// The type of transaction (Payment, TrustSet, OfferCreate, etc.).
    /// </summary>
    public required string TransactionType { get; init; }

    /// <summary>
    /// Human-readable classification for the account's perspective.
    /// </summary>
    public required XrplTransactionCategory Category { get; init; }

    /// <summary>
    /// The counterparty address (sender or recipient).
    /// </summary>
    public string? Counterparty { get; init; }

    /// <summary>
    /// Amount in drops (positive for inflows, negative for outflows).
    /// </summary>
    public required long AmountDrops { get; init; }

    /// <summary>
    /// Amount in XRP.
    /// </summary>
    public decimal AmountXrp => AmountDrops / 1_000_000m;

    /// <summary>
    /// Fee paid in drops (always negative).
    /// </summary>
    public required long FeeDrops { get; init; }

    /// <summary>
    /// Fee paid in XRP.
    /// </summary>
    public decimal FeeXrp => FeeDrops / 1_000_000m;

    /// <summary>
    /// Ledger index where this transaction was included.
    /// </summary>
    public required long LedgerIndex { get; init; }

    /// <summary>
    /// Whether the transaction succeeded.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Result code from XRPL (tesSUCCESS, etc.).
    /// </summary>
    public required string ResultCode { get; init; }

    /// <summary>
    /// Human-readable summary of what this transaction did.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Link to XRPL explorer (read-only, for user reference).
    /// </summary>
    public string ExplorerUrl => $"https://livenet.xrpl.org/transactions/{Hash}";

    /// <summary>
    /// Whether this transaction was initiated by the tracked account.
    /// </summary>
    public bool IsOutgoing => AmountDrops < 0 || (AmountDrops == 0 && FeeDrops < 0);

    /// <summary>
    /// Net impact on the account (amount + fee).
    /// </summary>
    public long NetImpactDrops => AmountDrops - Math.Abs(FeeDrops);

    /// <summary>
    /// Net impact in XRP.
    /// </summary>
    public decimal NetImpactXrp => NetImpactDrops / 1_000_000m;
}

/// <summary>
/// Classification of XRPL transactions from the account holder's perspective.
/// </summary>
public enum XrplTransactionCategory
{
    /// <summary>
    /// Received XRP from another address.
    /// </summary>
    IncomingPayment,

    /// <summary>
    /// Sent XRP to another address.
    /// </summary>
    OutgoingPayment,

    /// <summary>
    /// Transaction that only consumed fees (no transfer).
    /// </summary>
    FeeOnly,

    /// <summary>
    /// Reserve changed due to trust lines, offers, or other objects.
    /// </summary>
    ReserveChange,

    /// <summary>
    /// Account was activated with initial funding.
    /// </summary>
    AccountActivation,

    /// <summary>
    /// Other transaction type (not a simple payment).
    /// </summary>
    Other
}

/// <summary>
/// Represents a balance change between two sync points.
/// "My XRP balance changed — explain it to me."
/// </summary>
public sealed record XrplBalanceChangeDto
{
    /// <summary>
    /// Previous balance in drops.
    /// </summary>
    public required long PreviousBalanceDrops { get; init; }

    /// <summary>
    /// Current balance in drops.
    /// </summary>
    public required long CurrentBalanceDrops { get; init; }

    /// <summary>
    /// When the previous balance was recorded.
    /// </summary>
    public required DateTime PreviousSyncAt { get; init; }

    /// <summary>
    /// When the current balance was recorded.
    /// </summary>
    public required DateTime CurrentSyncAt { get; init; }

    /// <summary>
    /// Balance change in drops (positive = increase, negative = decrease).
    /// </summary>
    public long ChangeDrops => CurrentBalanceDrops - PreviousBalanceDrops;

    /// <summary>
    /// Balance change in XRP.
    /// </summary>
    public decimal ChangeXrp => ChangeDrops / 1_000_000m;

    /// <summary>
    /// Previous balance in XRP.
    /// </summary>
    public decimal PreviousBalanceXrp => PreviousBalanceDrops / 1_000_000m;

    /// <summary>
    /// Current balance in XRP.
    /// </summary>
    public decimal CurrentBalanceXrp => CurrentBalanceDrops / 1_000_000m;

    /// <summary>
    /// Whether balance increased.
    /// </summary>
    public bool IsIncrease => ChangeDrops > 0;

    /// <summary>
    /// Whether balance decreased.
    /// </summary>
    public bool IsDecrease => ChangeDrops < 0;

    /// <summary>
    /// Whether balance is unchanged.
    /// </summary>
    public bool IsUnchanged => ChangeDrops == 0;

    /// <summary>
    /// Transactions that explain this change (from on-chain data).
    /// </summary>
    public IReadOnlyList<XrplTransactionDto> ExplainingTransactions { get; init; } = Array.Empty<XrplTransactionDto>();

    /// <summary>
    /// Human-readable explanation of the change.
    /// </summary>
    public string Explanation
    {
        get
        {
            if (IsUnchanged)
                return "Balance unchanged since last sync.";

            var direction = IsIncrease ? "increased" : "decreased";
            var amount = Math.Abs(ChangeXrp);
            var txCount = ExplainingTransactions.Count;

            if (txCount == 0)
                return $"Balance {direction} by {amount:N6} XRP. No transactions found in this period.";

            if (txCount == 1)
            {
                var tx = ExplainingTransactions[0];
                return $"Balance {direction} by {amount:N6} XRP. {tx.Summary}";
            }

            return $"Balance {direction} by {amount:N6} XRP across {txCount} transactions.";
        }
    }
}

/// <summary>
/// Reconciliation status between NextLedger's cached value and XRPL's authoritative state.
/// "Does NextLedger agree with XRPL?"
/// </summary>
public sealed record XrplReconciliationDto
{
    /// <summary>
    /// Balance as reported by XRPL (authoritative source of truth).
    /// </summary>
    public required long XrplBalanceDrops { get; init; }

    /// <summary>
    /// Balance cached in NextLedger.
    /// </summary>
    public required long NextLedgerBalanceDrops { get; init; }

    /// <summary>
    /// When XRPL balance was fetched.
    /// </summary>
    public required DateTime XrplFetchedAt { get; init; }

    /// <summary>
    /// When NextLedger's cache was last updated.
    /// </summary>
    public required DateTime? NextLedgerSyncedAt { get; init; }

    /// <summary>
    /// Difference in drops (XRPL - NextLedger). Should be 0 after sync.
    /// </summary>
    public long DifferenceDrops => XrplBalanceDrops - NextLedgerBalanceDrops;

    /// <summary>
    /// Difference in XRP.
    /// </summary>
    public decimal DifferenceXrp => DifferenceDrops / 1_000_000m;

    /// <summary>
    /// XRPL balance in XRP.
    /// </summary>
    public decimal XrplBalanceXrp => XrplBalanceDrops / 1_000_000m;

    /// <summary>
    /// NextLedger balance in XRP.
    /// </summary>
    public decimal NextLedgerBalanceXrp => NextLedgerBalanceDrops / 1_000_000m;

    /// <summary>
    /// Whether the balances match (within tolerance of 1 drop for rounding).
    /// </summary>
    public bool IsReconciled => Math.Abs(DifferenceDrops) <= 1;

    /// <summary>
    /// Ledger index from which XRPL balance was fetched.
    /// </summary>
    public long? LedgerIndex { get; init; }

    /// <summary>
    /// Human-readable explanation of the reconciliation status.
    /// Emphasizes observation, not correction.
    /// </summary>
    public string Explanation
    {
        get
        {
            if (IsReconciled)
                return "✓ NextLedger matches the on-chain balance.";

            var timeDiff = NextLedgerSyncedAt.HasValue
                ? (XrplFetchedAt - NextLedgerSyncedAt.Value).TotalMinutes
                : (double?)null;

            if (timeDiff.HasValue && timeDiff > 1)
            {
                return $"Balances differ by {Math.Abs(DifferenceXrp):N6} XRP. " +
                       $"This is likely due to timing — NextLedger's cache is {timeDiff:N0} minutes old. " +
                       $"Sync again to update.";
            }

            return $"Balances differ by {Math.Abs(DifferenceXrp):N6} XRP. " +
                   $"XRPL reports {XrplBalanceXrp:N6} XRP; NextLedger shows {NextLedgerBalanceXrp:N6} XRP. " +
                   $"This may be due to recent on-chain activity.";
        }
    }
}

/// <summary>
/// Request to fetch XRPL transaction history. Read-only, no editing.
/// </summary>
public sealed record XrplTransactionHistoryRequest
{
    /// <summary>
    /// The NextLedger account ID for the XRPL account.
    /// </summary>
    public required Guid AccountId { get; init; }

    /// <summary>
    /// Maximum number of transactions to fetch.
    /// </summary>
    public int Limit { get; init; } = 20;

    /// <summary>
    /// Marker for pagination (from previous response).
    /// </summary>
    public string? Marker { get; init; }

    /// <summary>
    /// Only include transactions after this ledger index.
    /// </summary>
    public long? MinLedgerIndex { get; init; }

    /// <summary>
    /// Only include transactions before this ledger index.
    /// </summary>
    public long? MaxLedgerIndex { get; init; }
}

/// <summary>
/// Response containing XRPL transaction history. Read-only.
/// </summary>
public sealed record XrplTransactionHistoryResult
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
    /// The XRPL address.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Transactions in reverse chronological order.
    /// </summary>
    public IReadOnlyList<XrplTransactionDto> Transactions { get; init; } = Array.Empty<XrplTransactionDto>();

    /// <summary>
    /// Marker for fetching the next page.
    /// </summary>
    public string? NextMarker { get; init; }

    /// <summary>
    /// Whether there are more transactions available.
    /// </summary>
    public bool HasMore => !string.IsNullOrEmpty(NextMarker);

    /// <summary>
    /// When this data was fetched.
    /// </summary>
    public DateTime FetchedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static XrplTransactionHistoryResult Ok(
        string address,
        IReadOnlyList<XrplTransactionDto> transactions,
        string? nextMarker = null) => new()
        {
            Success = true,
            Address = address,
            Transactions = transactions,
            NextMarker = nextMarker,
            FetchedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static XrplTransactionHistoryResult Fail(string address, string errorMessage) => new()
    {
        Success = false,
        Address = address,
        ErrorMessage = errorMessage,
        FetchedAt = DateTime.UtcNow
    };
}

/// <summary>
/// XRPL balance snapshot for history tracking.
/// Stores previous sync values so we can explain changes.
/// </summary>
public sealed record XrplBalanceSnapshot
{
    /// <summary>
    /// The NextLedger account ID.
    /// </summary>
    public required Guid AccountId { get; init; }

    /// <summary>
    /// Balance in drops at this snapshot.
    /// </summary>
    public required long BalanceDrops { get; init; }

    /// <summary>
    /// Reserve in drops at this snapshot.
    /// </summary>
    public required long ReserveDrops { get; init; }

    /// <summary>
    /// Owner count at this snapshot.
    /// </summary>
    public required int OwnerCount { get; init; }

    /// <summary>
    /// Ledger index at this snapshot.
    /// </summary>
    public required long LedgerIndex { get; init; }

    /// <summary>
    /// When this snapshot was taken.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Balance in XRP.
    /// </summary>
    public decimal BalanceXrp => BalanceDrops / 1_000_000m;

    /// <summary>
    /// Reserve in XRP.
    /// </summary>
    public decimal ReserveXrp => ReserveDrops / 1_000_000m;

    /// <summary>
    /// Spendable balance in drops.
    /// </summary>
    public long SpendableDrops => Math.Max(0, BalanceDrops - ReserveDrops);

    /// <summary>
    /// Spendable balance in XRP.
    /// </summary>
    public decimal SpendableXrp => SpendableDrops / 1_000_000m;
}
