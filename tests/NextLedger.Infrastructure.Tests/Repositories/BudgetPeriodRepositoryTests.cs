using NextLedger.Domain.Entities;
using NextLedger.Domain.ValueObjects;
using NextLedger.Infrastructure.Database;
using NextLedger.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace NextLedger.Infrastructure.Tests.Repositories;

public class BudgetPeriodRepositoryTests : IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly BudgetPeriodRepository _repository;

    public BudgetPeriodRepositoryTests()
    {
        _connectionFactory = SqliteConnectionFactory.CreateInMemory();
        _connectionFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
        _repository = new BudgetPeriodRepository(_connectionFactory);
    }

    [Fact]
    public async Task AddAsync_CreatesPeriod()
    {
        var period = BudgetPeriod.Create(2024, 6);

        var id = await _repository.AddAsync(period);

        id.Should().Be(period.Id);
    }

    [Fact]
    public async Task GetByYearMonthAsync_ReturnsPeriod()
    {
        var period = BudgetPeriod.Create(2024, 7, new Money(100m));
        period.UpdateIncome(new Money(5000m));
        await _repository.AddAsync(period);

        var result = await _repository.GetByYearMonthAsync(2024, 7);

        result.Should().NotBeNull();
        result!.Year.Should().Be(2024);
        result.Month.Should().Be(7);
        result.TotalIncome.Amount.Should().Be(5000m);
        result.CarriedOver.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task GetByYearMonthAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByYearMonthAsync(2099, 12);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateAsync_CreatesIfNotExists()
    {
        var result = await _repository.GetOrCreateAsync(2024, 8);

        result.Should().NotBeNull();
        result.Year.Should().Be(2024);
        result.Month.Should().Be(8);
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnsExisting()
    {
        var original = BudgetPeriod.Create(2024, 9);
        original.UpdateIncome(new Money(3000m));
        await _repository.AddAsync(original);

        var result = await _repository.GetOrCreateAsync(2024, 9);

        result.Id.Should().Be(original.Id);
        result.TotalIncome.Amount.Should().Be(3000m);
    }

    [Fact]
    public async Task GetByYearAsync_ReturnsAllPeriodsForYear()
    {
        await _repository.AddAsync(BudgetPeriod.Create(2024, 1));
        await _repository.AddAsync(BudgetPeriod.Create(2024, 2));
        await _repository.AddAsync(BudgetPeriod.Create(2024, 3));
        await _repository.AddAsync(BudgetPeriod.Create(2023, 12)); // Different year

        var results = await _repository.GetByYearAsync(2024);

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(p => p.Year.Should().Be(2024));
    }

    [Fact]
    public async Task GetPreviousPeriodAsync_ReturnsPreviousMonth()
    {
        await _repository.AddAsync(BudgetPeriod.Create(2024, 5));

        var result = await _repository.GetPreviousPeriodAsync(2024, 6);

        result.Should().NotBeNull();
        result!.Month.Should().Be(5);
    }

    [Fact]
    public async Task GetPreviousPeriodAsync_CrossYear_Works()
    {
        await _repository.AddAsync(BudgetPeriod.Create(2023, 12));

        var result = await _repository.GetPreviousPeriodAsync(2024, 1);

        result.Should().NotBeNull();
        result!.Year.Should().Be(2023);
        result.Month.Should().Be(12);
    }

    [Fact]
    public async Task GetOrCreateAsync_CarriesOverReadyToAssign_NotRemaining()
    {
        // Arrange: Previous period has zero ReadyToAssign, but has envelope surplus (Remaining > 0)
        // If we carried Remaining, we'd double-count because envelope balances roll via RolloverFromPrevious.
        var prevYear = 2026;
        var prevMonth = 1;

        var prevPeriod = BudgetPeriod.Create(prevYear, prevMonth, Money.Zero);
        await _repository.AddAsync(prevPeriod);

        // Income 100, allocate 100 => ReadyToAssign should be 0
        // Spend 50 => Remaining would be 50
        prevPeriod.UpdateIncome(new Money(100m));
        prevPeriod.UpdateAllocated(new Money(100m));
        prevPeriod.UpdateSpent(new Money(50m));
        await _repository.UpdateAsync(prevPeriod);

        // Act
        var next = await _repository.GetOrCreateAsync(2026, 2);

        // Assert
        next.CarriedOver.Should().Be(Money.Zero);
    }

    [Fact]
    public async Task GetOrCreateAsync_CarriesOverUnassignedMoney_FromReadyToAssign()
    {
        var prevYear = 2026;
        var prevMonth = 3;

        var prevPeriod = BudgetPeriod.Create(prevYear, prevMonth, Money.Zero);
        await _repository.AddAsync(prevPeriod);

        // Income 100, allocate 40 => ReadyToAssign should be 60
        prevPeriod.UpdateIncome(new Money(100m));
        prevPeriod.UpdateAllocated(new Money(40m));
        await _repository.UpdateAsync(prevPeriod);

        var next = await _repository.GetOrCreateAsync(2026, 4);

        next.CarriedOver.Should().Be(new Money(60m));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesPeriod()
    {
        var period = BudgetPeriod.Create(2024, 10);
        await _repository.AddAsync(period);

        period.UpdateIncome(new Money(5000m));
        period.UpdateAllocated(new Money(4500m));
        period.UpdateSpent(new Money(3000m));
        await _repository.UpdateAsync(period);

        var result = await _repository.GetByIdAsync(period.Id);
        result!.TotalIncome.Amount.Should().Be(5000m);
        result.TotalAllocated.Amount.Should().Be(4500m);
        result.TotalSpent.Amount.Should().Be(3000m);
    }

    public void Dispose()
    {
        _connectionFactory.Dispose();
    }
}
