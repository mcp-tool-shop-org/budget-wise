using BudgetWise.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BudgetWise.Domain.Tests.ValueObjects;

public class DateRangeTests
{
    [Fact]
    public void Create_WithValidDates_Succeeds()
    {
        var start = new DateOnly(2024, 1, 1);
        var end = new DateOnly(2024, 1, 31);

        var range = new DateRange(start, end);

        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Fact]
    public void Create_WithEndBeforeStart_ThrowsException()
    {
        var start = new DateOnly(2024, 1, 31);
        var end = new DateOnly(2024, 1, 1);

        var act = () => new DateRange(start, end);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TotalDays_CalculatesCorrectly()
    {
        var range = new DateRange(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        range.TotalDays.Should().Be(31);
    }

    [Fact]
    public void Contains_DateWithinRange_ReturnsTrue()
    {
        var range = new DateRange(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        range.Contains(new DateOnly(2024, 1, 15)).Should().BeTrue();
    }

    [Fact]
    public void Contains_DateOutsideRange_ReturnsFalse()
    {
        var range = new DateRange(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 31));

        range.Contains(new DateOnly(2024, 2, 1)).Should().BeFalse();
    }

    [Fact]
    public void ForMonth_ReturnsCorrectRange()
    {
        var range = DateRange.ForMonth(2024, 2);

        range.Start.Should().Be(new DateOnly(2024, 2, 1));
        range.End.Should().Be(new DateOnly(2024, 2, 29)); // Leap year
    }

    [Fact]
    public void ForYear_ReturnsFullYear()
    {
        var range = DateRange.ForYear(2024);

        range.Start.Should().Be(new DateOnly(2024, 1, 1));
        range.End.Should().Be(new DateOnly(2024, 12, 31));
        range.TotalDays.Should().Be(366); // Leap year
    }

    [Fact]
    public void LastNDays_ReturnsCorrectRange()
    {
        var range = DateRange.LastNDays(7);

        range.TotalDays.Should().Be(7);
        range.End.Should().Be(DateOnly.FromDateTime(DateTime.Today));
    }

    [Fact]
    public void Overlaps_WithOverlappingRange_ReturnsTrue()
    {
        var range1 = new DateRange(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 15));
        var range2 = new DateRange(
            new DateOnly(2024, 1, 10),
            new DateOnly(2024, 1, 20));

        range1.Overlaps(range2).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithNonOverlappingRange_ReturnsFalse()
    {
        var range1 = new DateRange(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 10));
        var range2 = new DateRange(
            new DateOnly(2024, 1, 15),
            new DateOnly(2024, 1, 20));

        range1.Overlaps(range2).Should().BeFalse();
    }
}
