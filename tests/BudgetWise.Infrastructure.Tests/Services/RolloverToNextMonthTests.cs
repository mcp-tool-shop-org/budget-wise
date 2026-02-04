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

public class RolloverToNextMonthTests : IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly UnitOfWork _unitOfWork;

    public RolloverToNextMonthTests()
    {
        _connectionFactory = SqliteConnectionFactory.CreateInMemory();
        _connectionFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
        _unitOfWork = new UnitOfWork(_connectionFactory);
    }

    [Fact]
    public async Task RolloverToNextMonthAsync_WhenEnvelopeOverspent_CarriesNegativeRolloverForward()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        var period = BudgetPeriod.Create(year, month, Money.Zero);
        period.UpdateIncome(new Money(20m));
        await _unitOfWork.BudgetPeriods.AddAsync(period);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period);

        var envelopeService = new EnvelopeService(_unitOfWork);
        var transactionService = new TransactionService(_unitOfWork);

        await envelopeService.AllocateAsync(envelope.Id, new Money(20m), year, month);

        await transactionService.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 5),
            Amount = new Money(30m),
            Payee = "Grocery Store",
            EnvelopeId = envelope.Id
        });

        await envelopeService.RolloverToNextMonthAsync(year, month);

        var nextPeriod = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month + 1);
        var nextAllocation = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(envelope.Id, nextPeriod.Id);

        nextAllocation.RolloverFromPrevious.Should().Be(new Money(-10m));
    }

    [Fact]
    public async Task RolloverToNextMonthAsync_WhenEnvelopeHasSurplus_CarriesPositiveRolloverForward()
    {
        var year = 2026;
        var month = 2;

        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        var period = BudgetPeriod.Create(year, month, Money.Zero);
        period.UpdateIncome(new Money(50m));
        await _unitOfWork.BudgetPeriods.AddAsync(period);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period);

        var envelopeService = new EnvelopeService(_unitOfWork);
        var transactionService = new TransactionService(_unitOfWork);

        await envelopeService.AllocateAsync(envelope.Id, new Money(50m), year, month);

        await transactionService.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(year, month, 5),
            Amount = new Money(20m),
            Payee = "Grocery Store",
            EnvelopeId = envelope.Id
        });

        await envelopeService.RolloverToNextMonthAsync(year, month);

        var nextPeriod = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month + 1);
        var nextAllocation = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(envelope.Id, nextPeriod.Id);

        nextAllocation.RolloverFromPrevious.Should().Be(new Money(30m));
    }

    [Fact]
    public async Task AllocateAsync_WhenPeriodIsClosed_ThrowsWithoutMutatingAllocation()
    {
        var year = 2026;
        var month = 2;

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        var period = BudgetPeriod.Create(year, month, Money.Zero);
        period.UpdateIncome(new Money(100m));
        await _unitOfWork.BudgetPeriods.AddAsync(period);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period);

        var envelopeService = new EnvelopeService(_unitOfWork);

        await envelopeService.AllocateAsync(envelope.Id, new Money(50m), year, month);

        period.Close();
        await _unitOfWork.BudgetPeriods.UpdateAsync(period);

        var act = async () => await envelopeService.AllocateAsync(envelope.Id, new Money(60m), year, month);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*closed budget period*");

        var allocations = await _unitOfWork.EnvelopeAllocations.GetByPeriodAsync(period.Id);
        var allocation = allocations.Single(a => a.EnvelopeId == envelope.Id);
        allocation.Allocated.Should().Be(new Money(50m));
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _connectionFactory.Dispose();
    }
}
