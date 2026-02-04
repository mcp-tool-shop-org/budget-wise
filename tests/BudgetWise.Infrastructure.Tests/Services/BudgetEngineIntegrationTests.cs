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

public class BudgetEngineIntegrationTests : IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly UnitOfWork _unitOfWork;
    private readonly BudgetEngine _engine;
    private readonly TransactionService _transactions;

    public BudgetEngineIntegrationTests()
    {
        _connectionFactory = SqliteConnectionFactory.CreateInMemory();
        _connectionFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
        _unitOfWork = new UnitOfWork(_connectionFactory);
        _engine = new BudgetEngine(_unitOfWork);
        _transactions = new TransactionService(_unitOfWork);
    }

    [Fact]
    public async Task SetEnvelopeAllocationAsync_ReturnsSnapshotAndChange()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        var result = await _engine.SetEnvelopeAllocationAsync(
            new AllocateToEnvelopeRequest { EnvelopeId = envelope.Id, Amount = new Money(40m) },
            year,
            month);

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Snapshot.Should().NotBeNull();
        result.Snapshot!.ReadyToAssign.Should().Be(new Money(60m));

        result.AllocationChanges.Should().ContainSingle();
        result.AllocationChanges[0].BeforeAllocated.Should().Be(Money.Zero);
        result.AllocationChanges[0].AfterAllocated.Should().Be(new Money(40m));
    }

    [Fact]
    public async Task AdjustEnvelopeAllocationAsync_UpdatesReadyToAssign()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        await _engine.SetEnvelopeAllocationAsync(
            new AllocateToEnvelopeRequest { EnvelopeId = envelope.Id, Amount = new Money(40m) },
            year,
            month);

        var result = await _engine.AdjustEnvelopeAllocationAsync(
            new AdjustEnvelopeAllocationRequest { EnvelopeId = envelope.Id, Delta = new Money(-10m) },
            year,
            month);

        result.Success.Should().BeTrue();
        result.Snapshot!.ReadyToAssign.Should().Be(new Money(70m));
        result.AllocationChanges.Should().ContainSingle();
        result.AllocationChanges[0].BeforeAllocated.Should().Be(new Money(40m));
        result.AllocationChanges[0].AfterAllocated.Should().Be(new Money(30m));
    }

    [Fact]
    public async Task MoveAsync_ProducesTwoAllocationChanges_AndKeepsReadyToAssignStable()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var from = Envelope.Create("Food");
        var to = Envelope.Create("Gas");
        await _unitOfWork.Envelopes.AddAsync(from);
        await _unitOfWork.Envelopes.AddAsync(to);

        await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        await _engine.SetEnvelopeAllocationAsync(
            new AllocateToEnvelopeRequest { EnvelopeId = from.Id, Amount = new Money(40m) },
            year,
            month);

        await _engine.SetEnvelopeAllocationAsync(
            new AllocateToEnvelopeRequest { EnvelopeId = to.Id, Amount = new Money(20m) },
            year,
            month);

        var result = await _engine.MoveAsync(
            new MoveMoneyRequest { FromEnvelopeId = from.Id, ToEnvelopeId = to.Id, Amount = new Money(15m) },
            year,
            month);

        result.Success.Should().BeTrue();
        result.Snapshot!.ReadyToAssign.Should().Be(new Money(40m));
        result.AllocationChanges.Should().HaveCount(2);

        var fromChange = result.AllocationChanges.Single(c => c.EnvelopeId == from.Id);
        fromChange.BeforeAllocated.Should().Be(new Money(40m));
        fromChange.AfterAllocated.Should().Be(new Money(25m));

        var toChange = result.AllocationChanges.Single(c => c.EnvelopeId == to.Id);
        toChange.BeforeAllocated.Should().Be(new Money(20m));
        toChange.AfterAllocated.Should().Be(new Money(35m));
    }

    [Fact]
    public async Task RolloverAsync_WhenOverspent_CreatesNextPeriodWithNegativeRollover_AndCarriedOverReadyToAssign()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        await _engine.SetEnvelopeAllocationAsync(
            new AllocateToEnvelopeRequest { EnvelopeId = envelope.Id, Amount = new Money(50m) },
            year,
            month);

        await _transactions.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 5),
            Amount = new Money(70m),
            Payee = "Grocery Store",
            EnvelopeId = envelope.Id
        });

        var result = await _engine.RolloverAsync(year, month);

        result.Success.Should().BeTrue();

        var nextPeriod = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(2026, 3);
        nextPeriod.CarriedOver.Should().Be(new Money(50m));

        var nextAllocation = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(envelope.Id, nextPeriod.Id);
        nextAllocation.RolloverFromPrevious.Should().Be(new Money(-20m));

        var prevPeriod = await _unitOfWork.BudgetPeriods.GetByYearMonthAsync(year, month);
        prevPeriod!.IsClosed.Should().BeTrue();
    }

    [Fact]
    public async Task AutoAssignToGoalsAsync_EarliestGoalDateFirst_FundsEarlierGoalsFirst()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var earlier = Envelope.Create("Car Repair");
        var later = Envelope.Create("Vacation");
        await _unitOfWork.Envelopes.AddAsync(earlier);
        await _unitOfWork.Envelopes.AddAsync(later);

        await _engine.SetGoalAsync(new SetGoalRequest
        {
            EnvelopeId = earlier.Id,
            Amount = new Money(80m),
            TargetDate = new DateOnly(2026, 2, 10)
        });

        await _engine.SetGoalAsync(new SetGoalRequest
        {
            EnvelopeId = later.Id,
            Amount = new Money(80m),
            TargetDate = new DateOnly(2026, 3, 10)
        });

        await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        var result = await _engine.AutoAssignToGoalsAsync(
            new AutoAssignToGoalsRequest { Mode = AutoAssignMode.EarliestGoalDateFirst },
            year,
            month);

        result.Success.Should().BeTrue();
        result.Snapshot!.ReadyToAssign.Should().Be(Money.Zero);

        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month);
        var earlierAlloc = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(earlier.Id, period.Id);
        var laterAlloc = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(later.Id, period.Id);

        earlierAlloc.Allocated.Should().Be(new Money(80m));
        laterAlloc.Allocated.Should().Be(new Money(20m));
    }

    [Fact]
    public async Task AutoAssignToGoalsAsync_SmallestGoalFirst_FundsSmallerGoalsFirst()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var small = Envelope.Create("Phone");
        var big = Envelope.Create("Rent");
        await _unitOfWork.Envelopes.AddAsync(small);
        await _unitOfWork.Envelopes.AddAsync(big);

        await _engine.SetGoalAsync(new SetGoalRequest
        {
            EnvelopeId = small.Id,
            Amount = new Money(30m),
            TargetDate = new DateOnly(2026, 12, 31)
        });

        await _engine.SetGoalAsync(new SetGoalRequest
        {
            EnvelopeId = big.Id,
            Amount = new Money(70m),
            TargetDate = new DateOnly(2026, 1, 1)
        });

        await _transactions.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(50m),
            Payee = "Employer"
        });

        var result = await _engine.AutoAssignToGoalsAsync(
            new AutoAssignToGoalsRequest { Mode = AutoAssignMode.SmallestGoalFirst },
            year,
            month);

        result.Success.Should().BeTrue();
        result.Snapshot!.ReadyToAssign.Should().Be(Money.Zero);

        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month);
        var smallAlloc = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(small.Id, period.Id);
        var bigAlloc = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(big.Id, period.Id);

        smallAlloc.Allocated.Should().Be(new Money(30m));
        bigAlloc.Allocated.Should().Be(new Money(20m));
    }

    [Fact]
    public async Task CreateInflowAsync_ReturnsSnapshot_WithReadyToAssign()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var result = await _engine.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(125m),
            Payee = "Employer"
        });

        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Value.Should().NotBeNull();
        result.Snapshot.Should().NotBeNull();
        result.Snapshot!.ReadyToAssign.Should().Be(new Money(125m));
    }

    [Fact]
    public async Task CreateOutflowAsync_WhenEnvelopeAssigned_UpdatesEnvelopeSpent_AfterRecalc()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        await _engine.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        await _engine.SetEnvelopeAllocationAsync(
            new AllocateToEnvelopeRequest { EnvelopeId = envelope.Id, Amount = new Money(50m) },
            year,
            month);

        var outflow = await _engine.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 2),
            Amount = new Money(20m),
            Payee = "Grocery",
            EnvelopeId = envelope.Id
        });

        outflow.Success.Should().BeTrue();
        outflow.Snapshot!.TotalSpent.Should().Be(new Money(20m));

        var summary = await _engine.GetBudgetSummaryAsync(year, month);
        var food = summary.Envelopes.Single(e => e.Id == envelope.Id);
        food.Spent.Should().Be(new Money(20m));
        food.Available.Should().Be(new Money(30m));
    }

    [Fact]
    public async Task CreateOutflowAsync_WhenSplitAssigned_UpdatesEnvelopeSpent_AfterRecalc()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var food = Envelope.Create("Food");
        var fuel = Envelope.Create("Fuel");
        await _unitOfWork.Envelopes.AddAsync(food);
        await _unitOfWork.Envelopes.AddAsync(fuel);

        await _engine.CreateInflowAsync(new CreateInflowRequest
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

        await _engine.SetEnvelopeAllocationAsync(
            new AllocateToEnvelopeRequest { EnvelopeId = fuel.Id, Amount = new Money(50m) },
            year,
            month);

        var outflow = await _engine.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 2),
            Amount = new Money(20m),
            Payee = "Errands",
            SplitLines =
            [
                new TransactionSplitLineRequest { EnvelopeId = food.Id, Amount = new Money(7m) },
                new TransactionSplitLineRequest { EnvelopeId = fuel.Id, Amount = new Money(13m) }
            ]
        });

        outflow.Success.Should().BeTrue();
        outflow.Snapshot!.TotalSpent.Should().Be(new Money(20m));

        var summary = await _engine.GetBudgetSummaryAsync(year, month);
        var foodRow = summary.Envelopes.Single(e => e.Id == food.Id);
        var fuelRow = summary.Envelopes.Single(e => e.Id == fuel.Id);

        foodRow.Spent.Should().Be(new Money(7m));
        fuelRow.Spent.Should().Be(new Money(13m));
    }

    [Fact]
    public async Task GetUnassignedTransactionsAsync_DoesNotReturnSplitOutflows()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var a = Envelope.Create("A");
        var b = Envelope.Create("B");
        await _unitOfWork.Envelopes.AddAsync(a);
        await _unitOfWork.Envelopes.AddAsync(b);

        var created = await _engine.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 2),
            Amount = new Money(10m),
            Payee = "Split",
            SplitLines =
            [
                new TransactionSplitLineRequest { EnvelopeId = a.Id, Amount = new Money(4m) },
                new TransactionSplitLineRequest { EnvelopeId = b.Id, Amount = new Money(6m) }
            ]
        });

        created.Success.Should().BeTrue();

        var unassigned = await _engine.GetUnassignedTransactionsAsync();
        unassigned.Any(t => t.Id == created.Value!.Id).Should().BeFalse();
    }

    [Fact]
    public async Task CreateOutflowAsync_WhenSplitDoesNotSumToAmount_Fails()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var a = Envelope.Create("A");
        var b = Envelope.Create("B");
        await _unitOfWork.Envelopes.AddAsync(a);
        await _unitOfWork.Envelopes.AddAsync(b);

        var result = await _engine.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 2),
            Amount = new Money(10m),
            Payee = "BadSplit",
            SplitLines =
            [
                new TransactionSplitLineRequest { EnvelopeId = a.Id, Amount = new Money(4m) },
                new TransactionSplitLineRequest { EnvelopeId = b.Id, Amount = new Money(7m) }
            ]
        });

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTransferAsync_DoesNotAffectBudgetTotals()
    {
        var year = 2026;
        var month = 2;

        var from = Account.Create("Checking", AccountType.Checking);
        var to = Account.Create("Savings", AccountType.Savings);
        await _unitOfWork.Accounts.AddAsync(from);
        await _unitOfWork.Accounts.AddAsync(to);

        await _engine.CreateInflowAsync(new CreateInflowRequest
        {
            AccountId = from.Id,
            Date = new DateOnly(year, month, 1),
            Amount = new Money(100m),
            Payee = "Employer"
        });

        var transfer = await _engine.CreateTransferAsync(new CreateTransferRequest
        {
            FromAccountId = from.Id,
            ToAccountId = to.Id,
            Date = new DateOnly(year, month, 3),
            Amount = new Money(25m),
            Memo = "Move to savings"
        });

        transfer.Success.Should().BeTrue();
        transfer.Value.Should().NotBeNull();
        transfer.Value!.From.IsTransfer.Should().BeTrue();
        transfer.Value.To.IsTransfer.Should().BeTrue();

        var snapshot = await _engine.GetSnapshotAsync(year, month);
        snapshot.TotalIncome.Should().Be(new Money(100m));
        snapshot.TotalSpent.Should().Be(Money.Zero);
        snapshot.ReadyToAssign.Should().Be(new Money(100m));
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _connectionFactory.Dispose();
    }
}
