using NextLedger.Application.DTOs;
using FluentValidation;

namespace NextLedger.Application.Validation;

public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(100).WithMessage("Account name cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9\s\-_'&]+$").WithMessage("Account name contains invalid characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid account type.");

        When(x => x.InitialBalance.HasValue, () =>
        {
            RuleFor(x => x.InitialBalance!.Value.Amount)
                .Must(amount => Math.Abs(amount) <= 999_999_999.99m)
                .WithMessage("Initial balance is too large.");
        });
    }
}

public sealed class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Account ID is required.");

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Account name cannot be empty.")
                .MaximumLength(100).WithMessage("Account name cannot exceed 100 characters.")
                .Matches(@"^[a-zA-Z0-9\s\-_'&]+$").WithMessage("Account name contains invalid characters.");
        });

        When(x => x.Note is not null, () =>
        {
            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
        });
    }
}
