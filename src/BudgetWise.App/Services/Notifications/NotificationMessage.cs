namespace BudgetWise.App.Services.Notifications;

public sealed record NotificationMessage
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required NotificationSeverity Severity { get; init; }

    public NotificationActionKind ActionKind { get; init; } = NotificationActionKind.None;
    public string? ActionLabel { get; init; }

    /// <summary>
    /// If set, the notification will auto-dismiss after this duration.
    /// </summary>
    public TimeSpan? Duration { get; init; }
}
