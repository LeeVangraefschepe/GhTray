using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using GhTray.Formatting;
using GhTray.GitHub;
using GhTray.Icons;
using GhTray.Models;
using GhTray.Platform;
using GhTray.Storage;
using GhTray.UI;

namespace GhTray.Tray;

public sealed class TrayController : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private const int MaxTooltipLength = 63;

    private readonly IClassicDesktopStyleApplicationLifetime _lifetime;
    private readonly GitHubClient _gitHub = new();
    private readonly SeenActivityStore _seenActivity = new();
    private readonly FailedCheckStore _failedChecks = new();
    private readonly IAutostartRegistration _autostart = PlatformServices.CreateAutostart();
    private readonly INotificationSender _notificationSender = PlatformServices.CreateNotificationSender();
    private readonly TrayIcon _trayIcon = new();
    private readonly ItemsWindow _itemsWindow = new();
    private readonly DispatcherTimer _pollTimer = new();
    private readonly NotificationComposer _notifications;
    private readonly TrayIconUpdater _iconUpdater;

    private bool _isRefreshing;
    private bool _isFirstRun;
    private bool _lastFetchFailed;
    private bool _hasUnseenActivity;

    public TrayController(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
        _notifications = new NotificationComposer(_notificationSender);
        _iconUpdater = new TrayIconUpdater(_trayIcon, _itemsWindow, PlatformServices.CreatePanelTheme());

        _seenActivity.Load();
        _failedChecks.Load();
        _isFirstRun = _seenActivity.IsEmpty;

        ConfigureTrayIcon();
        ConfigureItemsWindow();
        StartPolling();
    }

    public void Dispose()
    {
        _pollTimer.Stop();
        _trayIcon.Dispose();
    }

    private void ConfigureTrayIcon()
    {
        _trayIcon.ToolTipText = "GitHub assigned - loading...";
        _trayIcon.Menu = BuildContextMenu();
        _trayIcon.Clicked += (_, _) => ToggleItemsWindow();
        UpdateIcon();

        TrayIcon.SetIcons(Application.Current!, new TrayIcons { _trayIcon });

        _notificationSender.Activated += () => Dispatcher.UIThread.Post(_itemsWindow.ShowNearTray);
    }

    private void ConfigureItemsWindow()
    {
        _itemsWindow.RefreshRequested += async () => await RefreshAsync(notify: false);
        _itemsWindow.Revealed += () =>
        {
            _itemsWindow.RepaintRelativeTimes();
            _hasUnseenActivity = false;
            UpdateIcon();
        };
    }

    private void StartPolling()
    {
        _pollTimer.Interval = PollInterval;
        _pollTimer.Tick += async (_, _) => await RefreshAsync(notify: true);
        _pollTimer.Start();

        _ = RefreshAsync(notify: true);
    }

    private NativeMenu BuildContextMenu()
    {
        var menu = new NativeMenu();
        menu.Add(CreateMenuItem("Open", _itemsWindow.ShowNearTray));
        menu.Add(CreateMenuItem("Refresh now", async () => await RefreshAsync(notify: false)));

        if (_autostart.IsSupported)
        {
            menu.Add(new NativeMenuItemSeparator());
            menu.Add(CreateAutostartMenuItem());
        }

        menu.Add(new NativeMenuItemSeparator());
        menu.Add(CreateMenuItem("Exit", Exit));
        return menu;
    }

    private static NativeMenuItem CreateMenuItem(string header, Action onClick)
    {
        var item = new NativeMenuItem(header);
        item.Click += (_, _) => onClick();
        return item;
    }

    private NativeMenuItem CreateAutostartMenuItem()
    {
        var item = new NativeMenuItem("Start at login")
        {
            ToggleType = NativeMenuItemToggleType.CheckBox,
            IsChecked = _autostart.IsEnabled(),
        };
        item.Click += (_, _) =>
        {
            _autostart.SetEnabled(!_autostart.IsEnabled());
            item.IsChecked = _autostart.IsEnabled();
        };
        return item;
    }

    private void ToggleItemsWindow()
    {
        if (_itemsWindow.IsVisible && _itemsWindow.IsActive) _itemsWindow.Hide();
        else _itemsWindow.ShowNearTray();
    }

    private void Exit()
    {
        Dispose();
        _lifetime.Shutdown();
    }

    private async Task RefreshAsync(bool notify)
    {
        if (_isRefreshing) return;

        _isRefreshing = true;
        _itemsWindow.ShowBusy();
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

        _itemsWindow.ShowItems(items, DateTime.Now);
        _trayIcon.ToolTipText = TextTruncation.Truncate($"GitHub: {items.Count} open", MaxTooltipLength);
        _lastFetchFailed = false;

        var hasActivity = changes.Count > 0 || newCheckFailures.Count > 0;
        if (!_isFirstRun && hasActivity && !_itemsWindow.IsVisible) _hasUnseenActivity = true;
        UpdateIcon();

        // The very first run only seeds state; otherwise every open item would notify at once.
        if (notify && !_isFirstRun && changes.Count > 0) _notifications.NotifyChanges(changes);
        if (notify && newCheckFailures.Count > 0) _notifications.NotifyCheckFailures(newCheckFailures);
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
        _itemsWindow.ShowError(error.Message);
        _trayIcon.ToolTipText = "GitHub: trouble fetching - see window";
        _lastFetchFailed = true; // Warning badge only: GitHub outages produced a notification on every poll.
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        var badge = _lastFetchFailed ? Badge.Warning
            : _hasUnseenActivity ? Badge.Unseen
            : Badge.None;
        _iconUpdater.Show(badge);
    }
}
