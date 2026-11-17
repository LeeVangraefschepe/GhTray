using GhTray.Formatting;

namespace GhTray.UI;

internal sealed class RefreshStatusStrip : StatusStrip
{
    private const int MaxErrorLength = 160;

    private readonly ToolStripStatusLabel _message = new()
    {
        Spring = true,
        TextAlign = ContentAlignment.MiddleLeft,
    };

    public RefreshStatusStrip()
    {
        SizingGrip = false;

        var refreshButton = new ToolStripButton("Refresh (F5)") { Alignment = ToolStripItemAlignment.Right };
        refreshButton.Click += (_, _) => RefreshRequested?.Invoke();

        Items.Add(_message);
        Items.Add(refreshButton);
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

        _message.BackColor = Theme.WarningFill;
        _message.ForeColor = Theme.WarningText;
        _message.Text =
            $"Trouble fetching from GitHub (showing last known data, retrying every minute): {TextTruncation.Truncate(singleLine, MaxErrorLength)}";
    }

    private void UseNormalColors()
    {
        _message.BackColor = SystemColors.Control;
        _message.ForeColor = SystemColors.ControlText;
    }
}
