using NextLedger.Application.DTOs;
using FluentValidation;

namespace NextLedger.Application.Validation;

public sealed class CreateOutflowRequestValidator : AbstractValidator<CreateOutflowRequest>
{
    public CreateOutflowRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account is required.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.Today.AddYears(1)))
            .WithMessage("Date cannot be more than 1 year in the future.")
            .Must(d => d >= DateOnly.FromDateTime(DateTime.Today.AddYears(-10)))
            .WithMessage("Date cannot be more than 10 years in the past.");

        RuleFor(x => x.Amount)
            .Must(m => m.IsPositive).WithMessage("Amount must be positive.")
            .Must(m => m.Amount <= 999_999_999.99m).WithMessage("Amount is too large.");

        RuleFor(x => x.Payee)
            .NotEmpty().WithMessage("Payee is required.")
            .MaximumLength(200).WithMessage("Payee name cannot exceed 200 characters.");

        When(x => x.Memo is not null, () =>
        {
            RuleFor(x => x.Memo)
                .MaximumLength(500).WithMessage("Memo cannot exceed 500 characters.");
        });
    }
}

public sealed class CreateInflowRequestValidator : AbstractValidator<CreateInflowRequest>
{
    public CreateInflowRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account is required.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.Today.AddYears(1)))
            .WithMessage("Date cannot be more than 1 year in the future.")
            .Must(d => d >= DateOnly.FromDateTime(DateTime.Today.AddYears(-10)))
            .WithMessage("Date cannot be more than 10 years in the past.");

        RuleFor(x => x.Amount)
            .Must(m => m.IsPositive).WithMessage("Amount must be positive.")
            .Must(m => m.Amount <= 999_999_999.99m).WithMessage("Amount is too large.");

        RuleFor(x => x.Payee)
            .NotEmpty().WithMessage("Payee is required.")
            .MaximumLength(200).WithMessage("Payee name cannot exceed 200 characters.");

        When(x => x.Memo is not null, () =>
        {
            RuleFor(x => x.Memo)
                .MaximumLength(500).WithMessage("Memo cannot exceed 500 characters.");
        });
    }
}

public sealed class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferRequestValidator()
    {
        RuleFor(x => x.FromAccountId)
            .NotEmpty().WithMessage("Source account is required.");

        RuleFor(x => x.ToAccountId)
            .NotEmpty().WithMessage("Destination account is required.")
            .NotEqual(x => x.FromAccountId).WithMessage("Cannot transfer to the same account.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.Today.AddYears(1)))
            .WithMessage("Date cannot be more than 1 year in the future.");

        RuleFor(x => x.Amount)
            .Must(m => m.IsPositive).WithMessage("Amount must be positive.")
            .Must(m => m.Amount <= 999_999_999.99m).WithMessage("Amount is too large.");

        When(x => x.Memo is not null, () =>
        {
            RuleFor(x => x.Memo)
                .MaximumLength(500).WithMessage("Memo cannot exceed 500 characters.");
        });
    }
}

public sealed class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Transaction ID is required.");

        When(x => x.Date.HasValue, () =>
        {
            RuleFor(x => x.Date!.Value)
                .Must(d => d <= DateOnly.FromDateTime(DateTime.Today.AddYears(1)))
                .WithMessage("Date cannot be more than 1 year in the future.")
                .Must(d => d >= DateOnly.FromDateTime(DateTime.Today.AddYears(-10)))
                .WithMessage("Date cannot be more than 10 years in the past.");
        });

        When(x => x.Amount.HasValue, () =>
        {
            RuleFor(x => x.Amount!.Value)
                .Must(m => m.Amount <= 999_999_999.99m).WithMessage("Amount is too large.");
        });

        When(x => x.Payee is not null, () =>
        {
            RuleFor(x => x.Payee)
                .NotEmpty().WithMessage("Payee cannot be empty.")
                .MaximumLength(200).WithMessage("Payee name cannot exceed 200 characters.");
        });

        When(x => x.Memo is not null, () =>
        {
            RuleFor(x => x.Memo)
                .MaximumLength(500).WithMessage("Memo cannot exceed 500 characters.");
        });
    }
}
