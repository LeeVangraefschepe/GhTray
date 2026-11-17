using GhTray.Icons;

namespace GhTray.Tray;

internal sealed class TrayIconUpdater(NotifyIcon trayIcon, Form windowToSync) : IDisposable
{
    private (Badge Badge, bool IsDarkTaskbar, int Size)? _lastRenderedState;
    private Icon? _currentIcon;

    public void Show(Badge badge)
    {
        var state = (Badge: badge, IsDarkTaskbar: TaskbarTheme.IsDark(), Size: SystemInformation.SmallIconSize.Width);
        if (_lastRenderedState == state) return;
        _lastRenderedState = state;

        var icon = IconFactory.Create(state.Badge, state.Size, state.IsDarkTaskbar);
        trayIcon.Icon = icon;
        windowToSync.Icon = icon;

        _currentIcon?.Dispose();
        _currentIcon = icon;
    }

    public void Dispose() => _currentIcon?.Dispose();
}
