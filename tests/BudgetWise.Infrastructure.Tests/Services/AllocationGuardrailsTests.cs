

using BudgetWise.Application.Services;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.ValueObjects;
using BudgetWise.Infrastructure.Database;
using BudgetWise.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace BudgetWise.Infrastructure.Tests.Services;

public class AllocationGuardrailsTests : IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly UnitOfWork _unitOfWork;

    public AllocationGuardrailsTests()
    {
        _connectionFactory = SqliteConnectionFactory.CreateInMemory();
        _connectionFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
        _unitOfWork = new UnitOfWork(_connectionFactory);
    }

    [Fact]
    public async Task AllocateAsync_WhenDeltaExceedsReadyToAssign_Throws()
    {
        var year = 2026;
        var month = 2;

        var period = BudgetPeriod.Create(year, month, Money.Zero);
        period.UpdateIncome(new Money(50m));
        await _unitOfWork.BudgetPeriods.AddAsync(period);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period);

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        var service = new EnvelopeService(_unitOfWork);

        var act = async () => await service.AllocateAsync(envelope.Id, new Money(100m), year, month);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ReadyToAssign*");
    }

    [Fact]
    public async Task AllocateAsync_WithinReadyToAssign_Succeeds()
    {
        var year = 2026;
        var month = 2;

        var period = BudgetPeriod.Create(year, month, Money.Zero);
        period.UpdateIncome(new Money(50m));
        await _unitOfWork.BudgetPeriods.AddAsync(period);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period);

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        var service = new EnvelopeService(_unitOfWork);

        var allocation = await service.AllocateAsync(envelope.Id, new Money(50m), year, month);

        allocation.Allocated.Should().Be(new Money(50m));
    }

    [Fact]
    public async Task AllocateAsync_DecreaseAllowed_EvenIfPeriodIsOverallocated()
    {
        var year = 2026;
        var month = 2;

        // Period with zero income.
        var period = BudgetPeriod.Create(year, month, Money.Zero);
        period.UpdateIncome(Money.Zero);
        await _unitOfWork.BudgetPeriods.AddAsync(period);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period);

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        // Seed an allocation directly (bypassing guardrails) and mark the period as overallocated.
        var seeded = EnvelopeAllocation.Create(envelope.Id, period.Id, new Money(100m));
        await _unitOfWork.EnvelopeAllocations.AddAsync(seeded);

        period.UpdateAllocated(new Money(100m));
        await _unitOfWork.BudgetPeriods.UpdateAsync(period);

        var service = new EnvelopeService(_unitOfWork);

        var allocation = await service.AllocateAsync(envelope.Id, new Money(50m), year, month);

        allocation.Allocated.Should().Be(new Money(50m));
    }

    [Fact]
    public async Task AddToAllocationAsync_IncreaseAboveReadyToAssign_Throws()
    {
        var year = 2026;
        var month = 2;

        var period = BudgetPeriod.Create(year, month, Money.Zero);
        period.UpdateIncome(new Money(10m));
        await _unitOfWork.BudgetPeriods.AddAsync(period);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period);

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        var service = new EnvelopeService(_unitOfWork);

        // Current ReadyToAssign = 10
        var act = async () => await service.AddToAllocationAsync(envelope.Id, new Money(11m), year, month);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ReadyToAssign*");
    }

    [Fact]
    public async Task AddToAllocationAsync_DecreaseAllowed_WhenNotBelowZero()
    {
        var year = 2026;
        var month = 2;

        var period = BudgetPeriod.Create(year, month, Money.Zero);
        period.UpdateIncome(new Money(0m));
        await _unitOfWork.BudgetPeriods.AddAsync(period);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period);

        var envelope = Envelope.Create("Food");
        await _unitOfWork.Envelopes.AddAsync(envelope);

        var service = new EnvelopeService(_unitOfWork);

        // Seed an allocation directly (bypassing guardrails) so we can test decreases when ReadyToAssign is 0.
        var seeded = EnvelopeAllocation.Create(envelope.Id, period.Id, new Money(20m));
        await _unitOfWork.EnvelopeAllocations.AddAsync(seeded);
        period.UpdateAllocated(new Money(20m));
        await _unitOfWork.BudgetPeriods.UpdateAsync(period);

        var allocation = await service.AddToAllocationAsync(envelope.Id, new Money(-5m), year, month);

        allocation.Allocated.Should().Be(new Money(15m));
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _connectionFactory.Dispose();
    }
}
