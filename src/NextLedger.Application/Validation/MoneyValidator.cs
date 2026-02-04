using NextLedger.Domain.ValueObjects;
using FluentValidation;

namespace NextLedger.Application.Validation;

/// <summary>
/// Common validation rules for Money values.
/// </summary>
public static class MoneyValidationRules
{
    public const decimal MaxAmount = 999_999_999.99m;
    public const decimal MinAmount = -999_999_999.99m;

    public static IRuleBuilderOptions<T, Money> ValidAmount<T>(this IRuleBuilder<T, Money> ruleBuilder)
    {
        return ruleBuilder
            .Must(m => m.Amount >= MinAmount && m.Amount <= MaxAmount)
            .WithMessage("Amount must be between -999,999,999.99 and 999,999,999.99.");
    }

    public static IRuleBuilderOptions<T, Money> PositiveAmount<T>(this IRuleBuilder<T, Money> ruleBuilder)
    {
        return ruleBuilder
            .Must(m => m.IsPositive)
            .WithMessage("Amount must be positive.");
    }

    public static IRuleBuilderOptions<T, Money> NonNegativeAmount<T>(this IRuleBuilder<T, Money> ruleBuilder)
    {
        return ruleBuilder
            .Must(m => !m.IsNegative)
            .WithMessage("Amount cannot be negative.");
    }

    public static IRuleBuilderOptions<T, Money> NotZero<T>(this IRuleBuilder<T, Money> ruleBuilder)
    {
        return ruleBuilder
            .Must(m => !m.IsZero)
            .WithMessage("Amount cannot be zero.");
    }
}

/// <summary>
/// Validates that a date is within acceptable range.
/// </summary>
public static class DateValidationRules
{
    public static IRuleBuilderOptions<T, DateOnly> ReasonableDate<T>(this IRuleBuilder<T, DateOnly> ruleBuilder)
    {
        var minDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-10));
        var maxDate = DateOnly.FromDateTime(DateTime.Today.AddYears(5));

        return ruleBuilder
            .Must(d => d >= minDate && d <= maxDate)
            .WithMessage($"Date must be between {minDate:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}.");
    }

    public static IRuleBuilderOptions<T, DateOnly> FutureDate<T>(this IRuleBuilder<T, DateOnly> ruleBuilder)
    {
        return ruleBuilder
            .Must(d => d >= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Date must be in the future.");
    }

    public static IRuleBuilderOptions<T, DateOnly> PastOrTodayDate<T>(this IRuleBuilder<T, DateOnly> ruleBuilder)
    {
        return ruleBuilder
            .Must(d => d <= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Date cannot be in the future.");
    }
}

/// <summary>
/// Common string validation rules.
/// </summary>
public static class StringValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> ruleBuilder, int maxLength = 100)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(maxLength).WithMessage($"Name cannot exceed {maxLength} characters.")
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Name cannot be only whitespace.");
    }

    public static IRuleBuilderOptions<T, string?> OptionalNote<T>(this IRuleBuilder<T, string?> ruleBuilder, int maxLength = 500)
    {
        return ruleBuilder
            .MaximumLength(maxLength).WithMessage($"Note cannot exceed {maxLength} characters.");
    }
}
