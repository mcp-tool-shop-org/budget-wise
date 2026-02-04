using NextLedger.Domain.Common;
using NextLedger.Domain.Enums;
using NextLedger.Domain.ValueObjects;

namespace NextLedger.Domain.Entities;

/// <summary>
/// Represents a financial account (checking, savings, credit card, etc.)
/// </summary>
public class Account : Entity
{
    public string Name { get; private set; }
    public AccountType Type { get; private set; }
    public Money Balance { get; private set; }
    public Money ClearedBalance { get; private set; }
    public Money UnclearedBalance { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsOnBudget { get; private set; }
    public int SortOrder { get; private set; }
    public string? Note { get; private set; }
    public DateTime? LastReconciledAt { get; private set; }

    // XRPL-specific properties (only populated for ExternalXrpl accounts)
    /// <summary>
    /// The external ledger address (e.g., XRPL r-address). Read-only, public address only.
    /// </summary>
    public string? ExternalAddress { get; private set; }

    /// <summary>
    /// The network this external account belongs to (e.g., "mainnet", "testnet").
    /// </summary>
    public string? ExternalNetwork { get; private set; }

    /// <summary>
    /// Last time the external ledger balance was successfully synced.
    /// </summary>
    public DateTime? LastExternalSyncAt { get; private set; }

    /// <summary>
    /// For XRPL: the reserve amount locked by the ledger (base reserve + owner reserve).
    /// Stored in drops (1 XRP = 1,000,000 drops).
    /// </summary>
    public long? ExternalReserveDrops { get; private set; }

    private Account() : base()
    {
        Name = string.Empty;
        Balance = Money.Zero;
        ClearedBalance = Money.Zero;
        UnclearedBalance = Money.Zero;
    }

    public static Account Create(
        string name,
        AccountType type,
        Money? initialBalance = null,
        bool isOnBudget = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty.", nameof(name));

        var account = new Account
        {
            Name = name.Trim(),
            Type = type,
            Balance = initialBalance ?? Money.Zero,
            ClearedBalance = initialBalance ?? Money.Zero,
            UnclearedBalance = Money.Zero,
            IsActive = true,
            IsOnBudget = isOnBudget,
            SortOrder = 0
        };

        return account;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Account name cannot be empty.", nameof(newName));

        Name = newName.Trim();
        Touch();
    }

    public void UpdateBalance(Money clearedBalance, Money unclearedBalance)
    {
        ClearedBalance = clearedBalance;
        UnclearedBalance = unclearedBalance;
        Balance = clearedBalance + unclearedBalance;
        Touch();
    }

    public void SetOnBudget(bool onBudget)
    {
        IsOnBudget = onBudget;
        Touch();
    }

    public void SetSortOrder(int order)
    {
        SortOrder = order;
        Touch();
    }

    public void SetNote(string? note)
    {
        Note = note?.Trim();
        Touch();
    }

    public void Close()
    {
        if (!Balance.IsZero)
            throw new InvalidOperationException("Cannot close account with non-zero balance.");

        IsActive = false;
        Touch();
    }

    public void Reopen()
    {
        IsActive = true;
        Touch();
    }

    public void MarkReconciled(Money reconciledBalance, DateTime reconciledAt)
    {
        ClearedBalance = reconciledBalance;
        Balance = reconciledBalance + UnclearedBalance;
        LastReconciledAt = reconciledAt;
        Touch();
    }

    public bool IsCreditType => Type is AccountType.CreditCard or AccountType.LineOfCredit;

    /// <summary>
    /// Whether this account is an externally reconciled ledger account (e.g., XRPL).
    /// </summary>
    public bool IsExternalLedger => Type is AccountType.ExternalXrpl;

    /// <summary>
    /// Creates an XRPL external ledger account. Read-only, non-custodial.
    /// NextLedger observes this account but cannot execute transactions.
    /// </summary>
    /// <param name="name">Display name for the account.</param>
    /// <param name="xrplAddress">The public r-address on XRPL (no private keys!).</param>
    /// <param name="network">Network identifier (mainnet/testnet).</param>
    public static Account CreateXrplAccount(string name, string xrplAddress, string network = "mainnet")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(xrplAddress))
            throw new ArgumentException("XRPL address is required.", nameof(xrplAddress));

        // Basic validation: XRPL addresses start with 'r' and are 25-35 chars
        if (!xrplAddress.StartsWith('r') || xrplAddress.Length < 25 || xrplAddress.Length > 35)
            throw new ArgumentException("Invalid XRPL address format. Must be a valid r-address.", nameof(xrplAddress));

        var account = new Account
        {
            Name = name.Trim(),
            Type = AccountType.ExternalXrpl,
            Balance = Money.Zero, // Will be fetched from XRPL
            ClearedBalance = Money.Zero,
            UnclearedBalance = Money.Zero,
            IsActive = true,
            IsOnBudget = false, // External accounts are off-budget by default
            SortOrder = 0,
            ExternalAddress = xrplAddress.Trim(),
            ExternalNetwork = network.ToLowerInvariant(),
            Note = "Externally reconciled (XRPL). Read-only."
        };

        return account;
    }

    /// <summary>
    /// Updates the balance from an external ledger sync (XRPL only).
    /// This sets the balance based on the authoritative on-chain state.
    /// </summary>
    /// <param name="balanceDrops">Balance in drops (1 XRP = 1,000,000 drops).</param>
    /// <param name="reserveDrops">Reserve amount in drops.</param>
    /// <param name="syncedAt">When the sync occurred.</param>
    public void UpdateFromExternalLedger(long balanceDrops, long reserveDrops, DateTime syncedAt)
    {
        if (Type != AccountType.ExternalXrpl)
            throw new InvalidOperationException("Only XRPL accounts can be updated from external ledger.");

        // Convert drops to XRP (divide by 1,000,000)
        var xrpBalance = balanceDrops / 1_000_000m;
        Balance = Money.USD(xrpBalance); // Using USD as placeholder; ideally we'd have Money.XRP
        ClearedBalance = Balance;
        UnclearedBalance = Money.Zero;
        ExternalReserveDrops = reserveDrops;
        LastExternalSyncAt = syncedAt;
        Touch();
    }

    /// <summary>
    /// Marks that an external sync attempt failed without changing the balance.
    /// </summary>
    public void MarkExternalSyncFailed()
    {
        // We don't update the balance, but we do touch the entity
        // so the UI knows we tried recently
        Touch();
    }

    /// <summary>
    /// For XRPL accounts: calculates spendable balance (total - reserve).
    /// Returns null for non-XRPL accounts.
    /// </summary>
    public decimal? GetSpendableXrpBalance()
    {
        if (Type != AccountType.ExternalXrpl || ExternalReserveDrops is null)
            return null;

        var totalDrops = (long)(Balance.Amount * 1_000_000m);
        var spendableDrops = totalDrops - ExternalReserveDrops.Value;
        return Math.Max(0, spendableDrops) / 1_000_000m;
    }
}
