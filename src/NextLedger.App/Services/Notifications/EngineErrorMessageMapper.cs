using NextLedger.Application.DTOs;

namespace NextLedger.App.Services.Notifications;

public sealed class EngineErrorMessageMapper : IEngineErrorMessageMapper
{
    public (string Title, string Message) Map(IReadOnlyList<BudgetOperationError> errors)
    {
        if (errors is null || errors.Count == 0)
            return ("Couldn't complete", "Try again. If it keeps happening, open Diagnostics and copy details.");

        var primary = errors[0];
        var formatted = FormatMessages(errors, maxMessages: 3);

        // Check for specific error patterns to provide targeted recovery guidance.
        var (specificTitle, specificMessage) = GetSpecificGuidance(primary, errors);
        if (specificTitle is not null)
            return (specificTitle, specificMessage!);

        return primary.Code switch
        {
            "VALIDATION" => (
                "Fix a few things",
                formatted.Length == 0
                    ? "One or more inputs are invalid. Review your entries and try again."
                    : $"Review and try again:{Environment.NewLine}{formatted}"),

            "INVALID_OPERATION" => (
                "That action isn't allowed right now",
                formatted.Length == 0
                    ? "The app can't apply that change in the current state. Adjust your inputs and try again."
                    : formatted),

            "NOT_IMPLEMENTED" => (
                "Not available yet",
                "This feature isn't available yet in this version."),

            "UNEXPECTED" => (
                "Something went wrong",
                "Try again. If it keeps happening, open Diagnostics and copy details."),

            _ => (
                "Couldn't complete",
                formatted.Length == 0
                    ? "Try again. If it keeps happening, open Diagnostics and copy details."
                    : formatted)
        };
    }

    /// <summary>
    /// Provides targeted recovery guidance for common error scenarios.
    /// </summary>
    private static (string? Title, string? Message) GetSpecificGuidance(
        BudgetOperationError primary,
        IReadOnlyList<BudgetOperationError> errors)
    {
        var msg = primary.Message?.ToLowerInvariant() ?? string.Empty;

        // Envelope-related errors
        if (msg.Contains("envelope") && msg.Contains("not found"))
            return ("Envelope not found", "The envelope may have been deleted. Refresh the page and try again.");

        if (msg.Contains("envelope") && msg.Contains("already exists"))
            return ("Duplicate envelope", "An envelope with that name already exists. Choose a different name.");

        // Account-related errors
        if (msg.Contains("account") && msg.Contains("not found"))
            return ("Account not found", "The account may have been deleted. Go to Budget and verify your accounts.");

        if (msg.Contains("no account"))
            return ("No account selected", "Please select an account before continuing.");

        // Transaction-related errors
        if (msg.Contains("transaction") && msg.Contains("not found"))
            return ("Transaction not found", "The transaction may have been deleted or modified. Refresh and try again.");

        if (msg.Contains("duplicate"))
            return ("Duplicate detected", "This transaction appears to already exist. Check your recent transactions.");

        if (msg.Contains("reconciled") && msg.Contains("edit"))
            return ("Can't edit reconciled transaction", "Reconciled transactions are locked. To change it, first un-reconcile the account period.");

        // Amount-related errors
        if (msg.Contains("negative") && msg.Contains("amount"))
            return ("Invalid amount", "Amounts must be positive. Use the correct transaction type (income/expense) instead.");

        if (msg.Contains("zero") && msg.Contains("amount"))
            return ("Amount required", "Please enter an amount greater than zero.");

        if (msg.Contains("overbudget") || msg.Contains("overspent"))
            return ("Not enough funds", "You've assigned more than available. Reduce other envelopes or add income first.");

        // Date-related errors
        if (msg.Contains("date") && (msg.Contains("future") || msg.Contains("invalid")))
            return ("Invalid date", "Please enter a valid date. Future dates may not be allowed for some operations.");

        if (msg.Contains("closed") && msg.Contains("month"))
            return ("Month is closed", "This budget month has been closed. You can only edit current or future months.");

        // Import-related errors
        if (msg.Contains("csv") || msg.Contains("parse") || msg.Contains("format"))
            return ("Import format issue", "Check your CSV format. Expected columns: Date, Payee, Amount (or Date, Description, Deposit, Withdrawal).");

        // Reconciliation errors
        if (msg.Contains("balance") && msg.Contains("match"))
            return ("Balance doesn't match", "The selected transactions don't match your statement balance. Check your selections or enable 'Create adjustment'.");

        return (null, null);
    }

    private static string FormatMessages(IReadOnlyList<BudgetOperationError> errors, int maxMessages)
    {
        if (errors is null || errors.Count == 0 || maxMessages <= 0)
            return string.Empty;

        var messages = errors
            .Select(e => string.IsNullOrWhiteSpace(e.Message) ? string.Empty : e.Message.Trim())
            .Where(m => m.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(maxMessages)
            .Select(m => $"- {m}")
            .ToList();

        if (messages.Count == 0)
            return string.Empty;

        var remaining = errors.Count - messages.Count;
        if (remaining > 0)
            messages.Add($"- …and {remaining} more.");

        return string.Join(Environment.NewLine, messages);
    }
}
