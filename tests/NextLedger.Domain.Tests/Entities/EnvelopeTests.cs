using NextLedger.Domain.Entities;
using NextLedger.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace NextLedger.Domain.Tests.Entities;

public class EnvelopeTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var envelope = Envelope.Create("Groceries", "Needs", "#FF5733");

        envelope.Name.Should().Be("Groceries");
        envelope.GroupName.Should().Be("Needs");
        envelope.Color.Should().Be("#FF5733");
        envelope.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsException()
    {
        var act = () => Envelope.Create("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetGoal_WithValidAmount_SetsGoal()
    {
        var envelope = Envelope.Create("Emergency Fund");
        var goalAmount = new Money(1000m);
        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6));

        envelope.SetGoal(goalAmount, targetDate);

        envelope.GoalAmount.Should().Be(goalAmount);
        envelope.GoalDate.Should().Be(targetDate);
        envelope.HasGoal.Should().BeTrue();
    }

    [Fact]
    public void SetGoal_WithNegativeAmount_ThrowsException()
    {
        var envelope = Envelope.Create("Test");

        var act = () => envelope.SetGoal(new Money(-100m));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ClearGoal_RemovesGoal()
    {
        var envelope = Envelope.Create("Emergency Fund");
        envelope.SetGoal(new Money(1000m));

        envelope.ClearGoal();

        envelope.HasGoal.Should().BeFalse();
        envelope.GoalAmount.Should().BeNull();
    }

    [Fact]
    public void Archive_SetsInactive()
    {
        var envelope = Envelope.Create("Old Category");

        envelope.Archive();

        envelope.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Hide_SetsHidden()
    {
        var envelope = Envelope.Create("Test");

        envelope.Hide();

        envelope.IsHidden.Should().BeTrue();
    }

    [Fact]
    public void Show_AfterHide_IsVisible()
    {
        var envelope = Envelope.Create("Test");
        envelope.Hide();

        envelope.Show();

        envelope.IsHidden.Should().BeFalse();
    }
}

public class EnvelopeAllocationTests
{
    [Fact]
    public void Create_WithDefaults_HasZeroAmounts()
    {
        var allocation = EnvelopeAllocation.Create(Guid.NewGuid(), Guid.NewGuid());

        allocation.Allocated.Should().Be(Money.Zero);
        allocation.Spent.Should().Be(Money.Zero);
        allocation.Available.Should().Be(Money.Zero);
    }

    [Fact]
    public void Available_CalculatesCorrectly()
    {
        var allocation = EnvelopeAllocation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(200m),
            new Money(50m));
        allocation.UpdateSpent(new Money(75m));

        // Available = Allocated + Rollover - Spent = 200 + 50 - 75 = 175
        allocation.Available.Amount.Should().Be(175m);
    }

    [Fact]
    public void IsOverspent_WhenSpentExceedsBudget_ReturnsTrue()
    {
        var allocation = EnvelopeAllocation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(100m));
        allocation.UpdateSpent(new Money(150m));

        allocation.IsOverspent.Should().BeTrue();
    }

    [Fact]
    public void AddToAllocation_IncreasesAmount()
    {
        var allocation = EnvelopeAllocation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(100m));

        allocation.AddToAllocation(new Money(50m));

        allocation.Allocated.Amount.Should().Be(150m);
    }

    [Fact]
    public void MoveTo_TransfersAllocation()
    {
        var from = EnvelopeAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), new Money(200m));
        var to = EnvelopeAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), new Money(50m));

        from.MoveTo(to, new Money(75m));

        from.Allocated.Amount.Should().Be(125m);
        to.Allocated.Amount.Should().Be(125m);
    }

    [Fact]
    public void MoveTo_MoreThanAvailable_ThrowsException()
    {
        var from = EnvelopeAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), new Money(100m));
        from.UpdateSpent(new Money(75m)); // Only 25 available
        var to = EnvelopeAllocation.Create(Guid.NewGuid(), Guid.NewGuid());

        var act = () => from.MoveTo(to, new Money(50m));

        act.Should().Throw<InvalidOperationException>();
    }
}
