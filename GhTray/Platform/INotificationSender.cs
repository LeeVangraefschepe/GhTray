namespace GhTray.Platform;

public interface INotificationSender
{
    /// <summary>Raised when the user clicks a notification that has no URL of its own. May fire on any thread.</summary>
    event Action? Activated;

    void Show(NotificationMessage message);
}
