using BudgetWise.Application.Services;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.ValueObjects;
using BudgetWise.Infrastructure.Database;
using BudgetWise.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace BudgetWise.Infrastructure.Tests.Services;

public class BudgetPeriodRecalculationServiceTests : IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly UnitOfWork _unitOfWork;

    public BudgetPeriodRecalculationServiceTests()
    {
        _connectionFactory = SqliteConnectionFactory.CreateInMemory();
        _connectionFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
        _unitOfWork = new UnitOfWork(_connectionFactory);
    }

    [Fact]
    public async Task RecalculateAsync_SetsIncomeSpentAllocated_AndExcludesTransfers()
    {
        var year = 2026;
        var month = 2;
        var dateInPeriod = new DateOnly(year, month, 10);

        // Accounts
        var checking = Account.Create("Checking", AccountType.Checking);
        var savings = Account.Create("Savings", AccountType.Savings);
        await _unitOfWork.Accounts.AddAsync(checking);
        await _unitOfWork.Accounts.AddAsync(savings);

        // Envelope + allocation
        var groceries = Envelope.Create("Groceries");
        await _unitOfWork.Envelopes.AddAsync(groceries);

        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month);
        var allocation = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(groceries.Id, period.Id);
        allocation.SetAllocation(new Money(60m));
        await _unitOfWork.EnvelopeAllocations.UpdateAsync(allocation);

        // Inflow + Outflow
        await _unitOfWork.Transactions.AddAsync(Transaction.CreateInflow(checking.Id, dateInPeriod, new Money(100m), "Paycheck"));
        await _unitOfWork.Transactions.AddAsync(Transaction.CreateOutflow(checking.Id, dateInPeriod, new Money(25m), "Store", groceries.Id));

        // Transfer (should not count toward income/spent totals)
        var txService = new TransactionService(_unitOfWork);
        await txService.CreateTransferAsync(new BudgetWise.Application.DTOs.CreateTransferRequest
        {
            FromAccountId = checking.Id,
            ToAccountId = savings.Id,
            Date = dateInPeriod,
            Amount = new Money(999m),
            Memo = "Move money"
        });

        // Out-of-period inflow (should not count)
        await _unitOfWork.Transactions.AddAsync(Transaction.CreateInflow(checking.Id, dateInPeriod.AddMonths(1), new Money(777m), "Next month"));

        var service = new BudgetPeriodRecalculationService(_unitOfWork);
        await service.RecalculateAsync(year, month);

        var reloaded = await _unitOfWork.BudgetPeriods.GetByYearMonthAsync(year, month);
        reloaded.Should().NotBeNull();

        reloaded!.TotalIncome.Should().Be(new Money(100m));
        reloaded.TotalSpent.Should().Be(new Money(25m));
        reloaded.TotalAllocated.Should().Be(new Money(60m));
    }

    [Fact]
    public async Task RecalculateAsync_PersistsAllocationSpent_PerEnvelope()
    {
        var year = 2026;
        var month = 2;
        var dateInPeriod = new DateOnly(year, month, 10);

        var checking = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(checking);

        var groceries = Envelope.Create("Groceries");
        var dining = Envelope.Create("Dining");
        await _unitOfWork.Envelopes.AddAsync(groceries);
        await _unitOfWork.Envelopes.AddAsync(dining);

        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month);

        var groceriesAlloc = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(groceries.Id, period.Id);
        groceriesAlloc.SetAllocation(new Money(60m));
        await _unitOfWork.EnvelopeAllocations.UpdateAsync(groceriesAlloc);

        var diningAlloc = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(dining.Id, period.Id);
        diningAlloc.SetAllocation(new Money(40m));
        await _unitOfWork.EnvelopeAllocations.UpdateAsync(diningAlloc);

        // Two assigned outflows + one unassigned outflow
        await _unitOfWork.Transactions.AddAsync(Transaction.CreateOutflow(checking.Id, dateInPeriod, new Money(10m), "Store", groceries.Id));
        await _unitOfWork.Transactions.AddAsync(Transaction.CreateOutflow(checking.Id, dateInPeriod, new Money(30m), "Cafe", dining.Id));
        await _unitOfWork.Transactions.AddAsync(Transaction.CreateOutflow(checking.Id, dateInPeriod, new Money(5m), "Unassigned"));

        var service = new BudgetPeriodRecalculationService(_unitOfWork);
        await service.RecalculateAsync(year, month);

        var allocations = await _unitOfWork.EnvelopeAllocations.GetByPeriodAsync(period.Id);
        allocations.Should().HaveCount(2);
        allocations.Single(a => a.EnvelopeId == groceries.Id).Spent.Should().Be(new Money(10m));
        allocations.Single(a => a.EnvelopeId == dining.Id).Spent.Should().Be(new Money(30m));

        var reloaded = await _unitOfWork.BudgetPeriods.GetByYearMonthAsync(year, month);
        reloaded!.TotalSpent.Should().Be(new Money(45m));
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _connectionFactory.Dispose();
    }
}
