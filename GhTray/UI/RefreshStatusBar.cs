using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using GhTray.Formatting;

namespace GhTray.UI;

internal sealed class RefreshStatusBar : Border
{
    private const int MaxErrorLength = 160;

    private readonly TextBlock _message = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
        FontSize = Palette.SmallFontSize + 1,
    };

    public RefreshStatusBar()
    {
        Padding = new Thickness(10, 4);
        Background = Palette.StatusFill;

        var refreshButton = new Button
        {
            Content = "Refresh (F5)",
            Padding = new Thickness(10, 2),
            FontSize = Palette.SmallFontSize + 1,
        };
        refreshButton.Click += (_, _) => RefreshRequested?.Invoke();
        DockPanel.SetDock(refreshButton, Dock.Right);

        Child = new DockPanel { Children = { refreshButton, _message } };
        UseNormalColors();
    }

    public event Action? RefreshRequested;

    public void ShowBusy()
    {
        UseNormalColors();
        _message.Text = "Refreshing...";
    }

    public void ShowRefreshed(DateTime refreshedAt)
    {
        UseNormalColors();
        _message.Text = $"Refreshed {refreshedAt:HH:mm} - double-click to open on GitHub";
    }

    public void ShowError(string errorMessage)
    {
        var singleLine = string.Join(' ', errorMessage.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        Background = Palette.WarningFill;
        _message.Foreground = Palette.WarningText;
        _message.Text =
            $"Trouble fetching from GitHub (showing last known data, retrying every minute): {TextTruncation.Truncate(singleLine, MaxErrorLength)}";
    }

    private void UseNormalColors()
    {
        Background = Palette.StatusFill;
        _message.Foreground = Palette.Text;
    }
}
