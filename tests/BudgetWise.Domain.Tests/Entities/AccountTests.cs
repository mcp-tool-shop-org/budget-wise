using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BudgetWise.Domain.Tests.Entities;

public class AccountTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var account = Account.Create("Checking", AccountType.Checking);

        account.Name.Should().Be("Checking");
        account.Type.Should().Be(AccountType.Checking);
        account.Balance.Should().Be(Money.Zero);
        account.IsActive.Should().BeTrue();
        account.IsOnBudget.Should().BeTrue();
    }

    [Fact]
    public void Create_WithInitialBalance_SetsBalance()
    {
        var balance = new Money(1000m);

        var account = Account.Create("Savings", AccountType.Savings, balance);

        account.Balance.Should().Be(balance);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsException()
    {
        var act = () => Account.Create("", AccountType.Checking);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        var account = Account.Create("Old Name", AccountType.Checking);

        account.Rename("New Name");

        account.Name.Should().Be("New Name");
    }

    [Fact]
    public void UpdateBalance_SetsAllBalances()
    {
        var account = Account.Create("Test", AccountType.Checking);
        var cleared = new Money(500m);
        var uncleared = new Money(100m);

        account.UpdateBalance(cleared, uncleared);

        account.ClearedBalance.Should().Be(cleared);
        account.UnclearedBalance.Should().Be(uncleared);
        account.Balance.Should().Be(new Money(600m));
    }

    [Fact]
    public void Close_WithZeroBalance_Succeeds()
    {
        var account = Account.Create("Test", AccountType.Checking);

        account.Close();

        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Close_WithNonZeroBalance_ThrowsException()
    {
        var account = Account.Create("Test", AccountType.Checking, new Money(100m));

        var act = () => account.Close();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reopen_ClosedAccount_MakesActive()
    {
        var account = Account.Create("Test", AccountType.Checking);
        account.Close();

        account.Reopen();

        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsCreditType_ForCreditCard_ReturnsTrue()
    {
        var creditCard = Account.Create("Visa", AccountType.CreditCard);
        var checking = Account.Create("Checking", AccountType.Checking);

        creditCard.IsCreditType.Should().BeTrue();
        checking.IsCreditType.Should().BeFalse();
    }
}
