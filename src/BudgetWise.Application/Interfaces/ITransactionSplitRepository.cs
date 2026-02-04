using BudgetWise.Domain.Entities;

namespace BudgetWise.Application.Interfaces;

/// <summary>
/// Repository for transaction split lines.
/// </summary>
public interface ITransactionSplitRepository
{
    Task<IReadOnlyList<TransactionSplitLine>> GetByTransactionIdAsync(Guid transactionId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<TransactionSplitLine>>> GetByTransactionIdsAsync(
        IReadOnlyList<Guid> transactionIds,
        CancellationToken ct = default);

    Task ReplaceAsync(Guid transactionId, IReadOnlyList<TransactionSplitLine> lines, CancellationToken ct = default);

    Task DeleteForTransactionAsync(Guid transactionId, CancellationToken ct = default);
}
