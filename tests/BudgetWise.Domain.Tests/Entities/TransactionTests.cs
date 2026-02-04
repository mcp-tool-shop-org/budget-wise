using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BudgetWise.Domain.Tests.Entities;

public class TransactionTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void CreateOutflow_StoresNegativeAmount()
    {
        var amount = new Money(50m);

        var tx = Transaction.CreateOutflow(_accountId, _today, amount, "Store");

        tx.Amount.Should().Be(amount.Negate());
        tx.Type.Should().Be(TransactionType.Outflow);
        tx.IsOutflow.Should().BeTrue();
    }

    [Fact]
    public void CreateInflow_StoresPositiveAmount()
    {
        var amount = new Money(1000m);

        var tx = Transaction.CreateInflow(_accountId, _today, amount, "Employer");

        tx.Amount.Should().Be(amount);
        tx.Type.Should().Be(TransactionType.Inflow);
        tx.IsInflow.Should().BeTrue();
    }

    [Fact]
    public void CreateTransfer_CreatesTwoTransferTransactions_AndLinkToEstablishesLinkage()
    {
        var fromAccount = Guid.NewGuid();
        var toAccount = Guid.NewGuid();
        var amount = new Money(500m);

        var (from, to) = Transaction.CreateTransfer(fromAccount, toAccount, _today, amount);

        from.Amount.Should().Be(amount.Negate());
        to.Amount.Should().Be(amount);
        from.LinkedTransactionId.Should().BeNull();
        to.LinkedTransactionId.Should().BeNull();
        from.IsTransfer.Should().BeTrue();
        to.IsTransfer.Should().BeTrue();

        from.LinkTo(to.Id);
        to.LinkTo(from.Id);

        from.LinkedTransactionId.Should().Be(to.Id);
        to.LinkedTransactionId.Should().Be(from.Id);
    }

    [Fact]
    public void LinkTo_OnNonTransfer_ThrowsException()
    {
        var tx = Transaction.CreateOutflow(_accountId, _today, new Money(10m), "Store");

        var act = () => tx.LinkTo(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateTransfer_ToSameAccount_ThrowsException()
    {
        var accountId = Guid.NewGuid();

        var act = () => Transaction.CreateTransfer(accountId, accountId, _today, new Money(100m));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignToEnvelope_SetsEnvelopeId()
    {
        var envelopeId = Guid.NewGuid();
        var tx = Transaction.CreateOutflow(_accountId, _today, new Money(50m), "Store");

        tx.AssignToEnvelope(envelopeId);

        tx.EnvelopeId.Should().Be(envelopeId);
        tx.IsAssigned.Should().BeTrue();
    }

    [Fact]
    public void AssignToEnvelope_OnTransfer_ThrowsException()
    {
        var (from, _) = Transaction.CreateTransfer(_accountId, Guid.NewGuid(), _today, new Money(100m));

        var act = () => from.AssignToEnvelope(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkCleared_SetsCleared()
    {
        var tx = Transaction.CreateOutflow(_accountId, _today, new Money(50m), "Store");

        tx.MarkCleared();

        tx.IsCleared.Should().BeTrue();
    }

    [Fact]
    public void MarkReconciled_WhenNotCleared_ThrowsException()
    {
        var tx = Transaction.CreateOutflow(_accountId, _today, new Money(50m), "Store");

        var act = () => tx.MarkReconciled();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkReconciled_WhenCleared_Succeeds()
    {
        var tx = Transaction.CreateOutflow(_accountId, _today, new Money(50m), "Store");
        tx.MarkCleared();

        tx.MarkReconciled();

        tx.IsReconciled.Should().BeTrue();
    }

    [Fact]
    public void SetAmount_WhenReconciled_ThrowsException()
    {
        var tx = Transaction.CreateOutflow(_accountId, _today, new Money(50m), "Store");
        tx.MarkCleared();
        tx.MarkReconciled();

        var act = () => tx.SetAmount(new Money(100m));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AbsoluteAmount_ReturnsPositive()
    {
        var tx = Transaction.CreateOutflow(_accountId, _today, new Money(50m), "Store");

        tx.AbsoluteAmount.Amount.Should().Be(50m);
    }
}
