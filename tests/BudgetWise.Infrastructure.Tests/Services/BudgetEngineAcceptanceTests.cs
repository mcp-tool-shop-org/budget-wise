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

public class BudgetEngineAcceptanceTests : IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly UnitOfWork _unitOfWork;
    private readonly BudgetEngine _engine;
    private readonly TransactionService _transactions;

    public BudgetEngineAcceptanceTests()
    {
        _connectionFactory = SqliteConnectionFactory.CreateInMemory();
        _connectionFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
        _unitOfWork = new UnitOfWork(_connectionFactory);
        _engine = new BudgetEngine(_unitOfWork);
        _transactions = new TransactionService(_unitOfWork);
    }

    [Fact]
    public async Task EndToEnd_BudgetMonth_Recalc_ThenRollover_PreservesCashCarryoverAndEnvelopeRollover()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var food = Envelope.Create("Food");
        var gas = Envelope.Create("Gas");
        await _unitOfWork.Envelopes.AddAsync(food);
        await _unitOfWork.Envelopes.AddAsync(gas);

        await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        await _engine.SetEnvelopeAllocationAsync(
            new AllocateToEnvelopeRequest { EnvelopeId = food.Id, Amount = new Money(60m) },
            year,
            month);

        await _engine.SetEnvelopeAllocationAsync(
            new AllocateToEnvelopeRequest { EnvelopeId = gas.Id, Amount = new Money(20m) },
            year,
            month);

        await _engine.MoveAsync(
            new MoveMoneyRequest { FromEnvelopeId = food.Id, ToEnvelopeId = gas.Id, Amount = new Money(10m) },
            year,
            month);

        // Outflows: 30 from Food, 5 from Gas, plus 10 unassigned.
        await _transactions.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 5),
            Amount = new Money(30m),
            Payee = "Grocery Store",
            EnvelopeId = food.Id
        });

        await _transactions.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 7),
            Amount = new Money(5m),
            Payee = "Gas Station",
            EnvelopeId = gas.Id
        });

        await _transactions.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 9),
            Amount = new Money(10m),
            Payee = "Coffee",
            EnvelopeId = null
        });

        // Recalculate totals (spent includes unassigned)
        var recalc = await _engine.RecalculateAsync(year, month);
        recalc.Success.Should().BeTrue();

        var snapshot = await _engine.GetSnapshotAsync(year, month);
        snapshot.TotalIncome.Should().Be(new Money(100m));
        snapshot.TotalAllocated.Should().Be(new Money(80m));
        snapshot.TotalSpent.Should().Be(new Money(45m));
        snapshot.ReadyToAssign.Should().Be(new Money(20m));

        // Rollover closes the period and creates next period carryover + envelope rollovers.
        var rollover = await _engine.RolloverAsync(year, month);
        rollover.Success.Should().BeTrue();

        var next = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(2026, 3);
        next.CarriedOver.Should().Be(new Money(20m));

        var foodNext = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(food.Id, next.Id);
        var gasNext = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(gas.Id, next.Id);

        // Food: started 60, moved 10 out -> 50, spent 30 => +20 rollover.
        foodNext.RolloverFromPrevious.Should().Be(new Money(20m));
        // Gas: started 20, moved 10 in -> 30, spent 5 => +25 rollover.
        gasNext.RolloverFromPrevious.Should().Be(new Money(25m));

        var prev = await _unitOfWork.BudgetPeriods.GetByYearMonthAsync(year, month);
        prev!.IsClosed.Should().BeTrue();
    }

    [Fact]
    public async Task EndToEnd_Overspending_RollsForwardAsNegativeRollover_WithoutReducingCashCarryover()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var food = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(food);

        await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        await _engine.SetEnvelopeAllocationAsync(
            new AllocateToEnvelopeRequest { EnvelopeId = food.Id, Amount = new Money(50m) },
            year,
            month);

        // Spend 70 from a 50 budget => -20 available
        await _transactions.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 5),
            Amount = new Money(70m),
            Payee = "Grocery Store",
            EnvelopeId = food.Id
        });

        await _engine.RecalculateAsync(year, month);

        var snapshot = await _engine.GetSnapshotAsync(year, month);
        snapshot.ReadyToAssign.Should().Be(new Money(50m));

        await _engine.RolloverAsync(year, month);

        var next = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(2026, 3);
        // Unassigned cash carries forward via ReadyToAssign.
        next.CarriedOver.Should().Be(new Money(50m));

        var foodNext = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(food.Id, next.Id);
        foodNext.RolloverFromPrevious.Should().Be(new Money(-20m));
    }

    [Fact]
    public async Task EndToEnd_AutoAssignToGoals_ThenRollover_CarriesCashAndRollsGoalFundingForward()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var emergency = Envelope.Create("Emergency Fund");
        await _unitOfWork.Envelopes.AddAsync(emergency);

        await _engine.SetGoalAsync(new SetGoalRequest
        {
            EnvelopeId = emergency.Id,
            Amount = new Money(100m),
            TargetDate = new DateOnly(2026, 12, 31)
        });

        await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(120m),
            Payee = "Employer"
        });

        var autoAssign = await _engine.AutoAssignToGoalsAsync(
            new AutoAssignToGoalsRequest { Mode = AutoAssignMode.EarliestGoalDateFirst },
            year,
            month);

        autoAssign.Success.Should().BeTrue();
        autoAssign.Snapshot!.ReadyToAssign.Should().Be(new Money(20m));
        autoAssign.AllocationChanges.Should().ContainSingle();

        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month);
        var allocation = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(emergency.Id, period.Id);
        allocation.Allocated.Should().Be(new Money(100m));
        allocation.Available.Should().Be(new Money(100m));

        var rollover = await _engine.RolloverAsync(year, month);
        rollover.Success.Should().BeTrue();

        var next = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(2026, 3);
        next.CarriedOver.Should().Be(new Money(20m));

        var nextAllocation = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(emergency.Id, next.Id);
        nextAllocation.RolloverFromPrevious.Should().Be(new Money(100m));
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _connectionFactory.Dispose();
    }
}
