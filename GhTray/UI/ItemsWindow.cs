using Avalonia;
using Avalonia.Controls;
using GhTray.Models;

namespace GhTray.UI;

public sealed class ItemsWindow : Window
{
    private const string PullRequestsTitle = "Pull requests";
    private const string IssuesTitle = "Issues";
    private const int ScreenMargin = 12;

    private readonly TabItem _pullRequestsTab = new() { Header = PullRequestsTitle, FontSize = Palette.TabFontSize };
    private readonly TabItem _issuesTab = new() { Header = IssuesTitle, FontSize = Palette.TabFontSize };
    private readonly ItemListView _pullRequestList = new(isPullRequestList: true);
    private readonly ItemListView _issueList = new(isPullRequestList: false);
    private readonly RefreshStatusBar _statusBar = new();
    private readonly TabControl _tabs = new() { Padding = new Thickness(0) };

    public ItemsWindow()
    {
        ConfigureWindow();
        ConfigureTabs();

        _statusBar.RefreshRequested += () => RefreshRequested?.Invoke();
        DockPanel.SetDock(_statusBar, Dock.Bottom);
        Content = new DockPanel { Children = { _statusBar, _tabs } };
    }

    public event Action? RefreshRequested;

    public event Action? Revealed;

    public void ShowItems(IReadOnlyList<WorkItem> items, DateTime refreshedAt)
    {
        var pullRequests = items.Where(item => item.IsPullRequest).ToList();
        var issues = items.Where(item => !item.IsPullRequest).ToList();

        _pullRequestList.SetItems(pullRequests);
        _issueList.SetItems(issues);
        _pullRequestsTab.Header = $"{PullRequestsTitle} ({pullRequests.Count})";
        _issuesTab.Header = $"{IssuesTitle} ({issues.Count})";
        _statusBar.ShowRefreshed(refreshedAt);
    }

    public void ShowBusy() => _statusBar.ShowBusy();

    public void ShowError(string message) => _statusBar.ShowError(message);

    public void RepaintRelativeTimes()
    {
        _pullRequestList.RebuildRows();
        _issueList.RebuildRows();
    }

    public void ShowNearTray()
    {
        if (!IsVisible) PositionNearTray();

        Show();
        WindowState = WindowState.Normal;
        Activate();
        Revealed?.Invoke();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (e.CloseReason == WindowCloseReason.WindowClosing)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }

    private void ConfigureWindow()
    {
        Title = "GitHub - assigned to me";
        Width = 1260;
        Height = 560;
        MinWidth = 640;
        MinHeight = 300;
        WindowStartupLocation = WindowStartupLocation.Manual;
        ShowInTaskbar = false;
        FontSize = Palette.BodyFontSize;
        Background = Palette.Window;
    }

    private void ConfigureTabs()
    {
        _pullRequestsTab.Content = _pullRequestList;
        _issuesTab.Content = _issueList;
        _tabs.Items.Add(_pullRequestsTab);
        _tabs.Items.Add(_issuesTab);

        foreach (var list in new[] { _pullRequestList, _issueList })
        {
            list.OpenRequested += Platform.BrowserLauncher.Open;
            list.RefreshRequested += () => RefreshRequested?.Invoke();
        }

        _tabs.SelectionChanged += (_, _) =>
            (_tabs.SelectedItem == _pullRequestsTab ? _pullRequestList : _issueList).FocusRows();
    }

    private void PositionNearTray()
    {
        var screen = Screens.Primary;
        if (screen is null) return;

        var area = screen.WorkingArea;
        var widthInPixels = (int)(Width * screen.Scaling);
        var heightInPixels = (int)(Height * screen.Scaling);
        var margin = (int)(ScreenMargin * screen.Scaling);

        // Windows keeps its tray at the bottom; Linux panels (GNOME, KDE default) are usually at the top.
        var top = OperatingSystem.IsWindows() ? area.Bottom - heightInPixels - margin : area.Y + margin;
        Position = new PixelPoint(area.Right - widthInPixels - margin, top);
    }
}
