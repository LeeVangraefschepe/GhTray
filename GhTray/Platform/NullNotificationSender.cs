namespace GhTray.Platform;

internal sealed class NullNotificationSender : INotificationSender
{
    public event Action? Activated
    {
        add { }
        remove { }
    }

    public void Show(NotificationMessage message)
    {
    }
}
