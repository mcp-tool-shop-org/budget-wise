using NextLedger.Application.DTOs;
using NextLedger.Application.Validation;
using NextLedger.Domain.Enums;
using NextLedger.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace NextLedger.Application.Tests.Validation;

public class AccountValidationTests
{
    private readonly CreateAccountRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new CreateAccountRequest
        {
            Name = "My Checking",
            Type = AccountType.Checking
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyName_Fails()
    {
        var request = new CreateAccountRequest
        {
            Name = "",
            Type = AccountType.Checking
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void NameTooLong_Fails()
    {
        var request = new CreateAccountRequest
        {
            Name = new string('a', 101),
            Type = AccountType.Checking
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidCharactersInName_Fails()
    {
        var request = new CreateAccountRequest
        {
            Name = "Test<script>",
            Type = AccountType.Checking
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}

public class TransactionValidationTests
{
    private readonly CreateOutflowRequestValidator _outflowValidator = new();
    private readonly CreateTransferRequestValidator _transferValidator = new();

    [Fact]
    public void ValidOutflow_Passes()
    {
        var request = new CreateOutflowRequest
        {
            AccountId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Amount = new Money(50m),
            Payee = "Grocery Store"
        };

        var result = _outflowValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void NegativeAmount_Fails()
    {
        var request = new CreateOutflowRequest
        {
            AccountId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Amount = new Money(-50m),
            Payee = "Store"
        };

        var result = _outflowValidator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DateTooFarInFuture_Fails()
    {
        var request = new CreateOutflowRequest
        {
            AccountId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today.AddYears(2)),
            Amount = new Money(50m),
            Payee = "Store"
        };

        var result = _outflowValidator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyPayee_Fails()
    {
        var request = new CreateOutflowRequest
        {
            AccountId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Amount = new Money(50m),
            Payee = ""
        };

        var result = _outflowValidator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TransferToSameAccount_Fails()
    {
        var accountId = Guid.NewGuid();
        var request = new CreateTransferRequest
        {
            FromAccountId = accountId,
            ToAccountId = accountId,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Amount = new Money(100m)
        };

        var result = _transferValidator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AmountTooLarge_Fails()
    {
        var request = new CreateOutflowRequest
        {
            AccountId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Amount = new Money(1_000_000_000m),
            Payee = "Big Purchase"
        };

        var result = _outflowValidator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}

public class EnvelopeValidationTests
{
    private readonly MoveMoneyRequestValidator _moveValidator = new();
    private readonly AllocateToEnvelopeRequestValidator _allocateValidator = new();
    private readonly AdjustEnvelopeAllocationRequestValidator _adjustValidator = new();

    [Fact]
    public void ValidAllocation_Passes()
    {
        var request = new AllocateToEnvelopeRequest
        {
            EnvelopeId = Guid.NewGuid(),
            Amount = new Money(500m)
        };

        var result = _allocateValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void NegativeAllocation_Fails()
    {
        var request = new AllocateToEnvelopeRequest
        {
            EnvelopeId = Guid.NewGuid(),
            Amount = new Money(-100m)
        };

        var result = _allocateValidator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void MoveToSameEnvelope_Fails()
    {
        var envelopeId = Guid.NewGuid();
        var request = new MoveMoneyRequest
        {
            FromEnvelopeId = envelopeId,
            ToEnvelopeId = envelopeId,
            Amount = new Money(50m)
        };

        var result = _moveValidator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ZeroMoveAmount_Fails()
    {
        var request = new MoveMoneyRequest
        {
            FromEnvelopeId = Guid.NewGuid(),
            ToEnvelopeId = Guid.NewGuid(),
            Amount = Money.Zero
        };

        var result = _moveValidator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeAdjustment_Passes()
    {
        var request = new AdjustEnvelopeAllocationRequest
        {
            EnvelopeId = Guid.NewGuid(),
            Delta = new Money(-25m)
        };

        var result = _adjustValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ZeroAdjustment_Fails()
    {
        var request = new AdjustEnvelopeAllocationRequest
        {
            EnvelopeId = Guid.NewGuid(),
            Delta = Money.Zero
        };

        var result = _adjustValidator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
