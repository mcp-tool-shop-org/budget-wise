namespace BudgetWise.Application.Interfaces;

/// <summary>
/// Unit of work pattern for transaction management.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IAccountRepository Accounts { get; }
    IEnvelopeRepository Envelopes { get; }
    ITransactionRepository Transactions { get; }
    ITransactionSplitRepository TransactionSplits { get; }
    IBudgetPeriodRepository BudgetPeriods { get; }
    IEnvelopeAllocationRepository EnvelopeAllocations { get; }
    IPayeeRepository Payees { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
