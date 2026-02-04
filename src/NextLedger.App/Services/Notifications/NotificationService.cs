namespace NextLedger.App.Services.Notifications;

public sealed class NotificationService : INotificationService
{
    public event EventHandler<NotificationMessage>? NotificationRaised;

    public void Show(NotificationMessage message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        NotificationRaised?.Invoke(this, message);
    }
}
