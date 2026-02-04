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
}
