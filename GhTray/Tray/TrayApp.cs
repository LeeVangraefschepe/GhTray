using GhTray.Formatting;
using GhTray.GitHub;
using GhTray.Icons;
using GhTray.Models;
using GhTray.Storage;
using GhTray.UI;
using Timer = System.Windows.Forms.Timer;

namespace GhTray.Tray;

public sealed class TrayApp : ApplicationContext
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private const int MaxTooltipLength = 63;

    private readonly GitHubClient _gitHub = new();
    private readonly SeenActivityStore _seenActivity = new();
    private readonly FailedCheckStore _failedChecks = new();
    private readonly NotifyIcon _trayIcon = new();
    private readonly ItemsForm _itemsForm = new();
    private readonly Timer _pollTimer = new();
    private readonly BalloonNotifier _balloons;
    private readonly TrayIconUpdater _iconUpdater;

    private bool _isRefreshing;
    private bool _isFirstRun;
    private bool _lastFetchFailed;
    private bool _hasUnseenActivity;

    public TrayApp()
    {
        _balloons = new BalloonNotifier(_trayIcon);
        _iconUpdater = new TrayIconUpdater(_trayIcon, _itemsForm);

        _seenActivity.Load();
        _failedChecks.Load();
        _isFirstRun = _seenActivity.IsEmpty;

        ConfigureTrayIcon();
        ConfigureItemsForm();
        StartPolling();
    }

    private void ConfigureTrayIcon()
    {
        _trayIcon.ContextMenuStrip = BuildContextMenu();
        _trayIcon.Text = "GitHub assigned - loading...";
        UpdateIcon();
        _trayIcon.Visible = true;

        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) ToggleItemsForm();
        };
        _trayIcon.BalloonTipClicked += (_, _) => OpenBalloonTarget();
    }

    private void ConfigureItemsForm()
    {
        _itemsForm.RefreshRequested += async () => await RefreshAsync(notify: false);
        _itemsForm.VisibleChanged += (_, _) =>
        {
            if (!_itemsForm.Visible) return;

            _itemsForm.RepaintRelativeTimes();
            _hasUnseenActivity = false;
            UpdateIcon();
        };
    }

    private void StartPolling()
    {
        _pollTimer.Interval = (int)PollInterval.TotalMilliseconds;
        _pollTimer.Tick += async (_, _) => await RefreshAsync(notify: true);
        _pollTimer.Start();

        _ = RefreshAsync(notify: true);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var autostartItem = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true,
            Checked = AutostartRegistration.IsEnabled(),
        };
        autostartItem.CheckedChanged += (_, _) => AutostartRegistration.SetEnabled(autostartItem.Checked);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => _itemsForm.ShowNearTray());
        menu.Items.Add("Refresh now", null, async (_, _) => await RefreshAsync(notify: false));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(autostartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    private void ToggleItemsForm()
    {
        if (_itemsForm.Visible && _itemsForm.ContainsFocus) _itemsForm.Hide();
        else _itemsForm.ShowNearTray();
    }

    private void OpenBalloonTarget()
    {
        if (_balloons.ClickTargetUrl is { } url) BrowserLauncher.Open(url);
        else _itemsForm.ShowNearTray();
    }

    private async Task RefreshAsync(bool notify)
    {
        if (_isRefreshing) return;

        _isRefreshing = true;
        _itemsForm.ShowBusy();
        try
        {
            await FetchAndApplyAsync(notify);
        }
        catch (Exception ex)
        {
            ShowFetchFailure(ex);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private async Task FetchAndApplyAsync(bool notify)
    {
        var items = await _gitHub.GetAssignedAsync();
        var changes = _seenActivity.RecordAndFindChanges(items);
        var newCheckFailures = _failedChecks.RecordAndFindNewFailures(items);

        if (!_isFirstRun)
            changes = await DropOwnUpdatesAsync(changes);

        _itemsForm.ShowItems(items, DateTime.Now);
        _trayIcon.Text = TextTruncation.Truncate($"GitHub: {items.Count} open", MaxTooltipLength);
        _lastFetchFailed = false;

        var hasActivity = changes.Count > 0 || newCheckFailures.Count > 0;
        if (!_isFirstRun && hasActivity && !_itemsForm.Visible) _hasUnseenActivity = true;
        UpdateIcon();

        // The very first run only seeds state; otherwise every open item would notify at once.
        if (notify && !_isFirstRun && changes.Count > 0) _balloons.NotifyChanges(changes);
        if (notify && newCheckFailures.Count > 0) _balloons.NotifyCheckFailures(newCheckFailures);
        _isFirstRun = false;
    }

    /// <summary>New assignments always notify; updates whose latest activity is the user's own do not.</summary>
    private async Task<List<ItemChange>> DropOwnUpdatesAsync(List<ItemChange> changes)
    {
        if (changes.Count == 0) return changes;

        var isOwnUpdate = await Task.WhenAll(changes.Select(async change =>
            !change.IsNew && await _gitHub.IsLatestActivityMineAsync(change.Item)));

        return changes.Where((_, index) => !isOwnUpdate[index]).ToList();
    }

    private void ShowFetchFailure(Exception error)
    {
        _itemsForm.ShowError(error.Message);
        _trayIcon.Text = "GitHub: trouble fetching - see window";
        _lastFetchFailed = true; // Warning badge only: GitHub outages produced a balloon on every poll.
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        var badge = _lastFetchFailed ? Badge.Warning
            : _hasUnseenActivity ? Badge.Unseen
            : Badge.None;
        _iconUpdater.Show(badge);
    }

    protected override void ExitThreadCore()
    {
        _pollTimer.Stop();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _itemsForm.Dispose();
        _iconUpdater.Dispose();
        base.ExitThreadCore();
    }
}
