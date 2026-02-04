using NextLedger.Domain.Services;
using NextLedger.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace NextLedger.Domain.Tests.Services;

public class BudgetMathTests
{
    [Fact]
    public void ComputeReadyToAssign_UsesIncomePlusCarryoverMinusAllocated()
    {
        var totalIncome = new Money(100m);
        var carriedOver = new Money(25m);
        var totalAllocated = new Money(60m);

        var ready = BudgetMath.ComputeReadyToAssign(totalIncome, carriedOver, totalAllocated);

        ready.Should().Be(new Money(65m));
    }

    [Fact]
    public void ComputeEnvelopeAvailable_Overspent_GoesNegative()
    {
        var allocated = new Money(100m);
        var rollover = Money.Zero;
        var spent = new Money(120m);

        var available = BudgetMath.ComputeEnvelopeAvailable(allocated, rollover, spent);
        var rolloverNext = BudgetMath.ComputeRollover(available);

        available.Should().Be(new Money(-20m));
        rolloverNext.Should().Be(new Money(-20m));
    }

    [Fact]
    public void ComputeReadyToAssign_Zeroes_AreStable()
    {
        var ready = BudgetMath.ComputeReadyToAssign(Money.Zero, Money.Zero, Money.Zero);
        ready.Should().Be(Money.Zero);
    }
}
