using BudgetWise.Application.DTOs;
using BudgetWise.Application.Services;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.ValueObjects;
using BudgetWise.Infrastructure.Database;
using BudgetWise.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace BudgetWise.Infrastructure.Tests.Services;

public class ReconciliationIntegrationTests : IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly UnitOfWork _unitOfWork;
    private readonly BudgetEngine _engine;
    private readonly TransactionService _transactions;

    public ReconciliationIntegrationTests()
    {
        _connectionFactory = SqliteConnectionFactory.CreateInMemory();
        _connectionFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
        _unitOfWork = new UnitOfWork(_connectionFactory);
        _engine = new BudgetEngine(_unitOfWork);
        _transactions = new TransactionService(_unitOfWork);
    }

    [Fact]
    public async Task ReconcileAccountAsync_WhenDifferenceZero_ClearsAndReconcilesSelectedTransactions()
    {
        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var inflow = await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(2026, 2, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        var outflow = await _transactions.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(2026, 2, 2),
            Amount = new Money(30m),
            Payee = "Grocery",
            EnvelopeId = null
        });

        var result = await _engine.ReconcileAccountAsync(new ReconcileAccountRequest
        {
            AccountId = account.Id,
            StatementDate = new DateOnly(2026, 2, 28),
            StatementEndingBalance = new Money(70m),
            TransactionIdsToReconcile = new List<Guid> { inflow.Id, outflow.Id },
            CreateAdjustmentIfNeeded = false
        });

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Value.Should().NotBeNull();

        result.Value!.Difference.Should().Be(Money.Zero);
        result.Value.ClearedBalance.Should().Be(new Money(70m));
        result.Value.ReconciledTransactionCount.Should().Be(2);
        result.Value.AdjustmentTransaction.Should().BeNull();

        var inflowReloaded = await _unitOfWork.Transactions.GetByIdAsync(inflow.Id);
        inflowReloaded!.IsCleared.Should().BeTrue();
        inflowReloaded.IsReconciled.Should().BeTrue();

        var outflowReloaded = await _unitOfWork.Transactions.GetByIdAsync(outflow.Id);
        outflowReloaded!.IsCleared.Should().BeTrue();
        outflowReloaded.IsReconciled.Should().BeTrue();

        var accountReloaded = await _unitOfWork.Accounts.GetByIdAsync(account.Id);
        accountReloaded!.ClearedBalance.Should().Be(new Money(70m));
        accountReloaded.UnclearedBalance.Should().Be(Money.Zero);
        accountReloaded.Balance.Should().Be(new Money(70m));
        accountReloaded.LastReconciledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ReconcileAccountAsync_WhenDifferenceNonZero_WithoutAdjustment_FailsAndRollsBack()
    {
        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var inflow = await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(2026, 2, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        var result = await _engine.ReconcileAccountAsync(new ReconcileAccountRequest
        {
            AccountId = account.Id,
            StatementDate = new DateOnly(2026, 2, 28),
            StatementEndingBalance = new Money(90m),
            TransactionIdsToReconcile = new List<Guid> { inflow.Id },
            CreateAdjustmentIfNeeded = false
        });

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be("INVALID_OPERATION");
        result.Errors[0].Message.Should().Contain("difference must be zero");

        // The engine should not persist partial updates if it fails.
        var inflowReloaded = await _unitOfWork.Transactions.GetByIdAsync(inflow.Id);
        inflowReloaded!.IsCleared.Should().BeFalse();
        inflowReloaded.IsReconciled.Should().BeFalse();

        var accountReloaded = await _unitOfWork.Accounts.GetByIdAsync(account.Id);
        accountReloaded!.LastReconciledAt.Should().BeNull();
    }

    [Fact]
    public async Task ReconcileAccountAsync_WhenDifferenceNonZero_WithAdjustment_CreatesReconciledAdjustment()
    {
        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var inflow = await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(2026, 2, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        var outflow = await _transactions.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(2026, 2, 2),
            Amount = new Money(30m),
            Payee = "Grocery",
            EnvelopeId = null
        });

        var result = await _engine.ReconcileAccountAsync(new ReconcileAccountRequest
        {
            AccountId = account.Id,
            StatementDate = new DateOnly(2026, 2, 28),
            StatementEndingBalance = new Money(75m),
            TransactionIdsToReconcile = new List<Guid> { inflow.Id, outflow.Id },
            CreateAdjustmentIfNeeded = true
        });

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Value.Should().NotBeNull();

        result.Value!.Difference.Should().Be(Money.Zero);
        result.Value.ClearedBalance.Should().Be(new Money(75m));
        result.Value.ReconciledTransactionCount.Should().Be(2);
        result.Value.AdjustmentTransaction.Should().NotBeNull();

        var adjustmentDto = result.Value.AdjustmentTransaction!;
        adjustmentDto.Payee.Should().Be("Reconciliation Adjustment");
        adjustmentDto.Amount.Should().Be(new Money(5m));
        adjustmentDto.IsCleared.Should().BeTrue();
        adjustmentDto.IsReconciled.Should().BeTrue();

        var adjustmentReloaded = await _unitOfWork.Transactions.GetByIdAsync(adjustmentDto.Id);
        adjustmentReloaded!.IsCleared.Should().BeTrue();
        adjustmentReloaded.IsReconciled.Should().BeTrue();
    }

    [Fact]
    public async Task ReconcileAccountAsync_WhenDeletedTransactionIncluded_Fails()
    {
        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var inflow = await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(2026, 2, 1),
            Amount = new Money(50m),
            Payee = "Employer"
        });

        var delete = await _engine.DeleteTransactionAsync(inflow.Id);
        delete.Success.Should().BeTrue();

        var result = await _engine.ReconcileAccountAsync(new ReconcileAccountRequest
        {
            AccountId = account.Id,
            StatementDate = new DateOnly(2026, 2, 28),
            StatementEndingBalance = Money.Zero,
            TransactionIdsToReconcile = new List<Guid> { inflow.Id },
            CreateAdjustmentIfNeeded = false
        });

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().Be("INVALID_OPERATION");
        result.Errors[0].Message.Should().Contain("not found");

        var accountReloaded = await _unitOfWork.Accounts.GetByIdAsync(account.Id);
        accountReloaded!.LastReconciledAt.Should().BeNull();
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _connectionFactory.Dispose();
    }
}
