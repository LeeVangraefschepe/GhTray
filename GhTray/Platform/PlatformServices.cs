namespace GhTray.Platform;

public static class PlatformServices
{
    public static IAutostartRegistration CreateAutostart()
    {
        if (OperatingSystem.IsWindows()) return new WindowsAutostartRegistration();
        if (OperatingSystem.IsLinux()) return new LinuxAutostartRegistration();
        return new UnsupportedAutostartRegistration();
    }

    public static IPanelTheme CreatePanelTheme()
    {
        if (OperatingSystem.IsWindows()) return new WindowsPanelTheme();
        if (OperatingSystem.IsLinux()) return new LinuxPanelTheme();
        return new StaticPanelTheme(isDark: false);
    }

    public static INotificationSender CreateNotificationSender()
    {
        if (OperatingSystem.IsWindows()) return new WindowsNotificationSender();
        if (OperatingSystem.IsLinux()) return new LinuxNotificationSender();
        return new NullNotificationSender();
    }
}
