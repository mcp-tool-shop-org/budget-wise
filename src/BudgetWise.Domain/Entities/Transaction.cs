using BudgetWise.Domain.Common;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Domain.Entities;

/// <summary>
/// Represents a financial transaction in an account.
/// Transactions can be inflows (income), outflows (expenses), or transfers.
/// </summary>
public class Transaction : Entity
{
    public Guid AccountId { get; private set; }
    public Guid? EnvelopeId { get; private set; }
    public Guid? TransferAccountId { get; private set; }
    public Guid? LinkedTransactionId { get; private set; }
    public DateOnly Date { get; private set; }
    public Money Amount { get; private set; }
    public string Payee { get; private set; }
    public string? Memo { get; private set; }
    public TransactionType Type { get; private set; }
    public bool IsCleared { get; private set; }
    public bool IsReconciled { get; private set; }
    public bool IsApproved { get; private set; }
    public bool IsDeleted { get; private set; }

    private Transaction() : base()
    {
        Payee = string.Empty;
        Amount = Money.Zero;
        Date = DateOnly.FromDateTime(DateTime.Today);
    }

    public static Transaction CreateOutflow(
        Guid accountId,
        DateOnly date,
        Money amount,
        string payee,
        Guid? envelopeId = null,
        string? memo = null)
    {
        if (amount.IsNegative)
            throw new ArgumentException("Amount should be positive; it will be stored as negative for outflows.", nameof(amount));

        return new Transaction
        {
            AccountId = accountId,
            Date = date,
            Amount = amount.Negate(), // Outflows are negative
            Payee = payee?.Trim() ?? string.Empty,
            EnvelopeId = envelopeId,
            Memo = memo?.Trim(),
            Type = TransactionType.Outflow,
            IsCleared = false,
            IsReconciled = false,
            IsApproved = true
        };
    }

    public static Transaction CreateInflow(
        Guid accountId,
        DateOnly date,
        Money amount,
        string payee,
        Guid? envelopeId = null,
        string? memo = null)
    {
        if (amount.IsNegative)
            throw new ArgumentException("Amount should be positive for inflows.", nameof(amount));

        return new Transaction
        {
            AccountId = accountId,
            Date = date,
            Amount = amount, // Inflows are positive
            Payee = payee?.Trim() ?? string.Empty,
            EnvelopeId = envelopeId,
            Memo = memo?.Trim(),
            Type = TransactionType.Inflow,
            IsCleared = false,
            IsReconciled = false,
            IsApproved = true
        };
    }

    public static (Transaction from, Transaction to) CreateTransfer(
        Guid fromAccountId,
        Guid toAccountId,
        DateOnly date,
        Money amount,
        string? memo = null)
    {
        if (amount.IsNegative)
            throw new ArgumentException("Transfer amount should be positive.", nameof(amount));

        if (fromAccountId == toAccountId)
            throw new ArgumentException("Cannot transfer to the same account.", nameof(toAccountId));

        var fromTransaction = new Transaction
        {
            AccountId = fromAccountId,
            TransferAccountId = toAccountId,
            Date = date,
            Amount = amount.Negate(),
            Payee = "Transfer",
            Memo = memo?.Trim(),
            Type = TransactionType.Transfer,
            IsCleared = false,
            IsReconciled = false,
            IsApproved = true
        };

        var toTransaction = new Transaction
        {
            AccountId = toAccountId,
            TransferAccountId = fromAccountId,
            Date = date,
            Amount = amount,
            Payee = "Transfer",
            Memo = memo?.Trim(),
            Type = TransactionType.Transfer,
            IsCleared = false,
            IsReconciled = false,
            IsApproved = true
        };

        return (fromTransaction, toTransaction);
    }

    /// <summary>
    /// Links this transfer transaction to its counterpart.
    /// This is typically done after persistence to satisfy FK constraints.
    /// </summary>
    public void LinkTo(Guid linkedTransactionId)
    {
        if (Type != TransactionType.Transfer)
            throw new InvalidOperationException("Only transfers can be linked.");

        if (linkedTransactionId == Guid.Empty)
            throw new ArgumentException("Linked transaction id is required.", nameof(linkedTransactionId));

        if (linkedTransactionId == Id)
            throw new ArgumentException("Cannot link a transaction to itself.", nameof(linkedTransactionId));

        LinkedTransactionId = linkedTransactionId;
        Touch();
    }

    public void SetDate(DateOnly date)
    {
        if (IsReconciled)
            throw new InvalidOperationException("Cannot modify reconciled transaction.");

        Date = date;
        Touch();
    }

    public void SetAmount(Money amount)
    {
        if (IsReconciled)
            throw new InvalidOperationException("Cannot modify reconciled transaction.");

        // Preserve the sign based on transaction type
        Amount = Type == TransactionType.Outflow ? amount.Abs().Negate() : amount.Abs();
        Touch();
    }

    public void SetPayee(string payee)
    {
        if (IsReconciled)
            throw new InvalidOperationException("Cannot modify reconciled transaction.");

        Payee = payee?.Trim() ?? string.Empty;
        Touch();
    }

    public void SetMemo(string? memo)
    {
        Memo = memo?.Trim();
        Touch();
    }

    public void AssignToEnvelope(Guid? envelopeId)
    {
        if (IsReconciled)
            throw new InvalidOperationException("Cannot modify reconciled transaction.");

        if (Type == TransactionType.Transfer)
            throw new InvalidOperationException("Transfers cannot be assigned to envelopes.");

        EnvelopeId = envelopeId;
        Touch();
    }

    public void MarkCleared()
    {
        IsCleared = true;
        Touch();
    }

    public void MarkUncleared()
    {
        if (IsReconciled)
            throw new InvalidOperationException("Cannot uncleare a reconciled transaction.");

        IsCleared = false;
        Touch();
    }

    public void MarkReconciled()
    {
        if (!IsCleared)
            throw new InvalidOperationException("Transaction must be cleared before reconciling.");

        IsReconciled = true;
        Touch();
    }

    public void SoftDelete()
    {
        if (IsReconciled)
            throw new InvalidOperationException("Cannot delete reconciled transaction.");

        IsDeleted = true;
        Touch();
    }

    public void Approve()
    {
        IsApproved = true;
        Touch();
    }

    public void Unapprove()
    {
        if (IsReconciled)
            throw new InvalidOperationException("Cannot unapprove reconciled transaction.");

        IsApproved = false;
        Touch();
    }

    public bool IsTransfer => Type == TransactionType.Transfer;
    public bool IsInflow => Type == TransactionType.Inflow;
    public bool IsOutflow => Type == TransactionType.Outflow;
    public bool IsAssigned => EnvelopeId.HasValue || IsTransfer;
    public Money AbsoluteAmount => Amount.Abs();
}
