using BudgetWise.Domain.Common;
using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Domain.Entities;

/// <summary>
/// Represents a split line allocating part of a transaction amount to an envelope.
/// Amount is stored as a positive value (absolute amount).
/// </summary>
public sealed class TransactionSplitLine : Entity
{
    public Guid TransactionId { get; private set; }
    public Guid EnvelopeId { get; private set; }
    public Money Amount { get; private set; }
    public int SortOrder { get; private set; }

    private TransactionSplitLine() : base()
    {
        Amount = Money.Zero;
    }

    public static TransactionSplitLine Create(
        Guid transactionId,
        Guid envelopeId,
        Money amount,
        int sortOrder)
    {
        if (transactionId == Guid.Empty)
            throw new ArgumentException("Transaction id is required.", nameof(transactionId));

        if (envelopeId == Guid.Empty)
            throw new ArgumentException("Envelope id is required.", nameof(envelopeId));

        if (amount.IsNegative || amount.Amount <= 0)
            throw new ArgumentException("Split line amount must be positive.", nameof(amount));

        if (sortOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be >= 0.");

        return new TransactionSplitLine
        {
            TransactionId = transactionId,
            EnvelopeId = envelopeId,
            Amount = amount.Abs(),
            SortOrder = sortOrder
        };
    }
}
