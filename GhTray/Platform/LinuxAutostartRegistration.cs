namespace GhTray.Platform;

internal sealed class LinuxAutostartRegistration : IAutostartRegistration
{
    private static readonly string DesktopFilePath = Path.Combine(ConfigHome(), "autostart", "ghtray.desktop");

    public bool IsSupported => true;

    public bool IsEnabled() => File.Exists(DesktopFilePath);

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            File.Delete(DesktopFilePath);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(DesktopFilePath)!);
        File.WriteAllText(DesktopFilePath, BuildDesktopEntry());
    }

    private static string BuildDesktopEntry() => string.Join('\n',
        "[Desktop Entry]",
        "Type=Application",
        "Name=GhTray",
        "Comment=GitHub assigned pull requests and issues in the system tray",
        $"Exec=\"{Environment.ProcessPath}\"",
        "X-GNOME-Autostart-enabled=true",
        "");

    private static string ConfigHome()
    {
        var configured = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
            : configured;
    }
}
