namespace GhTray.Platform;

public interface IPanelTheme
{
    /// <summary>True when the surface behind the tray icon is dark, so the icon should be drawn in white.</summary>
    bool IsDark();
}
