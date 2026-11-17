namespace GhTray.Platform;

internal sealed class StaticPanelTheme(bool isDark) : IPanelTheme
{
    public bool IsDark() => isDark;
}
