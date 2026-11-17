using Avalonia.Controls;
using GhTray.Icons;
using GhTray.Platform;

namespace GhTray.Tray;

internal sealed class TrayIconUpdater(TrayIcon trayIcon, Window windowToSync, IPanelTheme panelTheme)
{
    private const int WindowsBaseIconSize = 16;
    private const int DefaultBaseIconSize = 32;

    private (Badge Badge, bool IsDarkPanel, int Size)? _lastRenderedState;

    public void Show(Badge badge)
    {
        var state = (Badge: badge, IsDarkPanel: panelTheme.IsDark(), Size: ChooseIconSize());
        if (_lastRenderedState == state) return;
        _lastRenderedState = state;

        var icon = IconFactory.Create(state.Badge, state.Size, state.IsDarkPanel);
        trayIcon.Icon = icon;
        windowToSync.Icon = icon;
    }

    private int ChooseIconSize()
    {
        var scaling = windowToSync.Screens.Primary?.Scaling ?? 1.0;
        var baseSize = OperatingSystem.IsWindows() ? WindowsBaseIconSize : DefaultBaseIconSize;
        return (int)Math.Round(baseSize * scaling);
    }
}
