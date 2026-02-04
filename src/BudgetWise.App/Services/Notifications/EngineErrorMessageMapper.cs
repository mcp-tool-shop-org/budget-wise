using BudgetWise.Application.DTOs;

namespace BudgetWise.App.Services.Notifications;

public sealed class EngineErrorMessageMapper : IEngineErrorMessageMapper
{
    public (string Title, string Message) Map(IReadOnlyList<BudgetOperationError> errors)
    {
        if (errors is null || errors.Count == 0)
            return ("Couldn’t complete", "Try again. If it keeps happening, open Diagnostics and copy details.");

        var primary = errors[0];
        var formatted = FormatMessages(errors, maxMessages: 3);

        return primary.Code switch
        {
            "VALIDATION" => (
                "Fix a few things",
                formatted.Length == 0
                    ? "One or more inputs are invalid. Review your entries and try again."
                    : $"Review and try again:{Environment.NewLine}{formatted}"),

            "INVALID_OPERATION" => (
                "That action isn’t allowed right now",
                formatted.Length == 0
                    ? "The app can’t apply that change in the current state. Adjust your inputs and try again."
                    : formatted),

            "NOT_IMPLEMENTED" => (
                "Not available yet",
                "This feature isn’t available yet in this version."),

            "UNEXPECTED" => (
                "Something went wrong",
                "Try again. If it keeps happening, open Diagnostics and copy details."),

            _ => (
                "Couldn’t complete",
                formatted.Length == 0
                    ? "Try again. If it keeps happening, open Diagnostics and copy details."
                    : formatted)
        };
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
