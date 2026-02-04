using FluentValidation;
using FluentValidation.Results;

namespace NextLedger.Application.Validation;

/// <summary>
/// Service for validating requests before processing.
/// </summary>
public sealed class ValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Validate a request object.
    /// </summary>
    public async Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken ct = default)
        where T : class
    {
        var validator = _serviceProvider.GetService(typeof(IValidator<T>)) as IValidator<T>;
        if (validator is null)
        {
            // No validator registered, return success
            return new ValidationResult();
        }

        return await validator.ValidateAsync(instance, ct);
    }

    /// <summary>
    /// Validate and throw if invalid.
    /// </summary>
    public async Task ValidateAndThrowAsync<T>(T instance, CancellationToken ct = default)
        where T : class
    {
        var result = await ValidateAsync(instance, ct);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : Exception
{
    public IReadOnlyList<ValidationFailure> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> errors)
        : base("Validation failed.")
    {
        Errors = errors.ToList();
    }

    public override string Message
    {
        get
        {
            var messages = Errors.Select(e => e.ErrorMessage);
            return string.Join(Environment.NewLine, messages);
        }
    }
}

/// <summary>
/// Extensions for registering validators.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Get all validation errors as a dictionary.
    /// </summary>
    public static Dictionary<string, string[]> ToDictionary(this ValidationResult result)
    {
        return result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );
    }

    /// <summary>
    /// Get first error message for a property.
    /// </summary>
    public static string? GetFirstError(this ValidationResult result, string propertyName)
    {
        return result.Errors.FirstOrDefault(e => e.PropertyName == propertyName)?.ErrorMessage;
    }
}
