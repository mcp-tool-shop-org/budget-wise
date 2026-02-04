using NextLedger.Application.DTOs;
using NextLedger.Domain.ValueObjects;
using FluentValidation;

namespace NextLedger.Application.Validation;

public sealed class AllocateToEnvelopeRequestValidator : AbstractValidator<AllocateToEnvelopeRequest>
{
    public AllocateToEnvelopeRequestValidator()
    {
        RuleFor(x => x.EnvelopeId)
            .NotEmpty().WithMessage("Envelope is required.");

        RuleFor(x => x.Amount)
            .Must(m => !m.IsNegative).WithMessage("Allocation amount cannot be negative.")
            .Must(m => m.Amount <= 999_999_999.99m).WithMessage("Amount is too large.");
    }
}

public sealed class MoveMoneyRequestValidator : AbstractValidator<MoveMoneyRequest>
{
    public MoveMoneyRequestValidator()
    {
        RuleFor(x => x.FromEnvelopeId)
            .NotEmpty().WithMessage("Source envelope is required.");

        RuleFor(x => x.ToEnvelopeId)
            .NotEmpty().WithMessage("Destination envelope is required.")
            .NotEqual(x => x.FromEnvelopeId).WithMessage("Cannot move money to the same envelope.");

        RuleFor(x => x.Amount)
            .Must(m => m.IsPositive).WithMessage("Amount must be positive.")
            .Must(m => m.Amount <= 999_999_999.99m).WithMessage("Amount is too large.");
    }
}

public sealed class AdjustEnvelopeAllocationRequestValidator : AbstractValidator<AdjustEnvelopeAllocationRequest>
{
    public AdjustEnvelopeAllocationRequestValidator()
    {
        RuleFor(x => x.EnvelopeId)
            .NotEmpty().WithMessage("Envelope is required.");

        // Delta can be positive (increase) or negative (decrease), but not zero.
        RuleFor(x => x.Delta)
            .Must(m => !m.IsZero).WithMessage("Delta must be non-zero.")
            .Must(m => Math.Abs(m.Amount) <= 999_999_999.99m).WithMessage("Amount is too large.");
    }
}

/// <summary>
/// Validates envelope creation input.
/// </summary>
public sealed class CreateEnvelopeRequestValidator : AbstractValidator<CreateEnvelopeRequest>
{
    public CreateEnvelopeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Envelope name is required.")
            .MaximumLength(100).WithMessage("Envelope name cannot exceed 100 characters.");

        When(x => x.GroupName is not null, () =>
        {
            RuleFor(x => x.GroupName)
                .MaximumLength(50).WithMessage("Group name cannot exceed 50 characters.");
        });

        When(x => x.Color is not null, () =>
        {
            RuleFor(x => x.Color)
                .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color code (e.g., #FF5733).");
        });
    }
}

/// <summary>
/// Request to create an envelope (for validation).
/// </summary>
public sealed record CreateEnvelopeRequest
{
    public required string Name { get; init; }
    public string? GroupName { get; init; }
    public string? Color { get; init; }
}

/// <summary>
/// Validates goal setting input.
/// </summary>
public sealed class SetGoalRequestValidator : AbstractValidator<SetGoalRequest>
{
    public SetGoalRequestValidator()
    {
        RuleFor(x => x.EnvelopeId)
            .NotEmpty().WithMessage("Envelope is required.");

        RuleFor(x => x.Amount)
            .Must(m => m.IsPositive).WithMessage("Goal amount must be positive.")
            .Must(m => m.Amount <= 999_999_999.99m).WithMessage("Goal amount is too large.");

        When(x => x.TargetDate.HasValue, () =>
        {
            RuleFor(x => x.TargetDate!.Value)
                .Must(d => d >= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Target date must be in the future.")
                .Must(d => d <= DateOnly.FromDateTime(DateTime.Today.AddYears(50)))
                .WithMessage("Target date is too far in the future.");
        });
    }
}
