using BudgetWise.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BudgetWise.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithAmount_RoundsToTwoDecimals()
    {
        var money = new Money(10.126m);

        money.Amount.Should().Be(10.13m);
    }

    [Fact]
    public void Create_WithCurrency_DefaultsToUsd()
    {
        var money = new Money(100m);

        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Zero_ReturnsZeroAmount()
    {
        Money.Zero.Amount.Should().Be(0m);
        Money.Zero.IsZero.Should().BeTrue();
    }

    [Fact]
    public void IsPositive_WithPositiveAmount_ReturnsTrue()
    {
        var money = new Money(50m);

        money.IsPositive.Should().BeTrue();
        money.IsNegative.Should().BeFalse();
    }

    [Fact]
    public void IsNegative_WithNegativeAmount_ReturnsTrue()
    {
        var money = new Money(-50m);

        money.IsNegative.Should().BeTrue();
        money.IsPositive.Should().BeFalse();
    }

    [Fact]
    public void Add_TwoMoneyValues_ReturnsSummed()
    {
        var a = new Money(10m);
        var b = new Money(25m);

        var result = a + b;

        result.Amount.Should().Be(35m);
    }

    [Fact]
    public void Subtract_TwoMoneyValues_ReturnsDifference()
    {
        var a = new Money(100m);
        var b = new Money(30m);

        var result = a - b;

        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void Multiply_ByDecimal_ReturnsProduct()
    {
        var money = new Money(100m);

        var result = money * 0.5m;

        result.Amount.Should().Be(50m);
    }

    [Fact]
    public void Divide_ByDecimal_ReturnsQuotient()
    {
        var money = new Money(100m);

        var result = money / 4m;

        result.Amount.Should().Be(25m);
    }

    [Fact]
    public void Divide_ByZero_ThrowsException()
    {
        var money = new Money(100m);

        var act = () => money / 0m;

        act.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void Abs_WithNegative_ReturnsPositive()
    {
        var money = new Money(-50m);

        var result = money.Abs();

        result.Amount.Should().Be(50m);
    }

    [Fact]
    public void Negate_FlipsSign()
    {
        var positive = new Money(50m);
        var negative = new Money(-30m);

        positive.Negate().Amount.Should().Be(-50m);
        negative.Negate().Amount.Should().Be(30m);
    }

    [Fact]
    public void Comparison_GreaterThan_Works()
    {
        var larger = new Money(100m);
        var smaller = new Money(50m);

        (larger > smaller).Should().BeTrue();
        (smaller > larger).Should().BeFalse();
    }

    [Fact]
    public void Comparison_LessThan_Works()
    {
        var larger = new Money(100m);
        var smaller = new Money(50m);

        (smaller < larger).Should().BeTrue();
        (larger < smaller).Should().BeFalse();
    }

    [Fact]
    public void Equality_SameAmount_AreEqual()
    {
        var a = new Money(100m);
        var b = new Money(100m);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentAmount_AreNotEqual()
    {
        var a = new Money(100m);
        var b = new Money(200m);

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ToFormattedString_ReturnsUsdFormat()
    {
        var money = new Money(1234.56m);

        money.ToFormattedString().Should().Be("$1,234.56");
    }

    [Fact]
    public void Operations_WithDifferentCurrencies_ThrowsException()
    {
        var usd = new Money(100m, "USD");
        var eur = new Money(100m, "EUR");

        var act = () => usd + eur;

        act.Should().Throw<InvalidOperationException>();
    }
}
