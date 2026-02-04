using BudgetWise.Application.Interfaces;
using BudgetWise.Infrastructure.Database;
using Microsoft.Data.Sqlite;

namespace BudgetWise.Infrastructure.Repositories;

/// <summary>
/// Unit of work implementation for SQLite.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private SqliteTransaction? _transaction;
    private bool _disposed;

    private AccountRepository? _accounts;
    private EnvelopeRepository? _envelopes;
    private TransactionRepository? _transactions;
    private TransactionSplitRepository? _transactionSplits;
    private BudgetPeriodRepository? _budgetPeriods;
    private EnvelopeAllocationRepository? _envelopeAllocations;
    private PayeeRepository? _payees;

    public UnitOfWork(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public IAccountRepository Accounts => _accounts ??= new AccountRepository(_connectionFactory);
    public IEnvelopeRepository Envelopes => _envelopes ??= new EnvelopeRepository(_connectionFactory);
    public ITransactionRepository Transactions => _transactions ??= new TransactionRepository(_connectionFactory);
    public ITransactionSplitRepository TransactionSplits => _transactionSplits ??= new TransactionSplitRepository(_connectionFactory);
    public IBudgetPeriodRepository BudgetPeriods => _budgetPeriods ??= new BudgetPeriodRepository(_connectionFactory);
    public IEnvelopeAllocationRepository EnvelopeAllocations => _envelopeAllocations ??= new EnvelopeAllocationRepository(_connectionFactory);
    public IPayeeRepository Payees => _payees ??= new PayeeRepository(_connectionFactory);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // SQLite with Dapper executes immediately; this is a no-op
        // but maintains the interface for potential future ORMs
        return Task.FromResult(0);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("Transaction already started.");

        var connection = await _connectionFactory.GetConnectionAsync(ct);
        _transaction = connection.BeginTransaction();
    }

    public Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No transaction to commit.");

        _transaction.Commit();
        _transaction.Dispose();
        _transaction = null;
        return Task.CompletedTask;
    }

    public Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No transaction to rollback.");

        _transaction.Rollback();
        _transaction.Dispose();
        _transaction = null;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _transaction?.Dispose();
        _transaction = null;
        _disposed = true;
    }
}
