namespace GhTray.Platform;

public enum NotificationSeverity
{
    Info,
    Error,
}

public sealed record NotificationMessage(
    string Title,
    string Body,
    NotificationSeverity Severity = NotificationSeverity.Info,
    string? ClickUrl = null);
