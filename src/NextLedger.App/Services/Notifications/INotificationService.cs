namespace NextLedger.App.Services.Notifications;

public interface INotificationService
{
    event EventHandler<NotificationMessage>? NotificationRaised;

    void Show(NotificationMessage message);

    void ShowSuccess(string title, string message, TimeSpan? duration = null)
        => Show(new NotificationMessage { Title = title, Message = message, Severity = NotificationSeverity.Success, Duration = duration });

    void ShowInfo(string title, string message, TimeSpan? duration = null)
        => Show(new NotificationMessage { Title = title, Message = message, Severity = NotificationSeverity.Informational, Duration = duration });

    void ShowWarning(string title, string message, TimeSpan? duration = null)
        => Show(new NotificationMessage { Title = title, Message = message, Severity = NotificationSeverity.Warning, Duration = duration });

    void ShowError(string title, string message, TimeSpan? duration = null)
        => Show(new NotificationMessage { Title = title, Message = message, Severity = NotificationSeverity.Error, Duration = duration });

    void ShowErrorAction(string title, string message, NotificationActionKind actionKind, string actionLabel, TimeSpan? duration = null)
        => Show(new NotificationMessage
        {
            Title = title,
            Message = message,
            Severity = NotificationSeverity.Error,
            Duration = duration,
            ActionKind = actionKind,
            ActionLabel = actionLabel
        });
}
