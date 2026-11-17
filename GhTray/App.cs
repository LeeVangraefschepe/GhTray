using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using GhTray.Tray;

namespace GhTray;

public sealed class App : Application
{
    private TrayController? _trayController;

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = ThemeVariant.Light;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _trayController = new TrayController(desktop);
            desktop.Exit += (_, _) => _trayController.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
