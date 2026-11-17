using GhTray.Models;

namespace GhTray.UI;

public sealed class ItemsForm : Form
{
    private const string PullRequestsTitle = "Pull requests";
    private const string IssuesTitle = "Issues";
    private const int ScreenMargin = 12;

    private readonly TabControl _tabs = new();
    private readonly TabPage _pullRequestsTab = new(PullRequestsTitle);
    private readonly TabPage _issuesTab = new(IssuesTitle);
    private readonly ItemListControl _pullRequestList = new(isPullRequestList: true);
    private readonly ItemListControl _issueList = new(isPullRequestList: false);
    private readonly RefreshStatusStrip _statusStrip = new();

    public ItemsForm()
    {
        ConfigureWindow();
        AddListTab(_pullRequestsTab, _pullRequestList);
        AddListTab(_issuesTab, _issueList);
        ConfigureTabs();

        _statusStrip.RefreshRequested += () => RefreshRequested?.Invoke();

        Controls.Add(_tabs);
        Controls.Add(_statusStrip);
    }

    public event Action? RefreshRequested;

    public void ShowItems(IReadOnlyList<WorkItem> items, DateTime refreshedAt)
    {
        var pullRequests = items.Where(item => item.IsPullRequest).ToList();
        var issues = items.Where(item => !item.IsPullRequest).ToList();

        _pullRequestList.SetItems(pullRequests);
        _issueList.SetItems(issues);
        _pullRequestsTab.Text = $"{PullRequestsTitle} ({pullRequests.Count})";
        _issuesTab.Text = $"{IssuesTitle} ({issues.Count})";
        _statusStrip.ShowRefreshed(refreshedAt);
    }

    public void ShowBusy() => _statusStrip.ShowBusy();

    public void ShowError(string message) => _statusStrip.ShowError(message);

    public void RepaintRelativeTimes()
    {
        _pullRequestList.Invalidate(true);
        _issueList.Invalidate(true);
    }

    public void ShowNearTray()
    {
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        if (!Visible)
            Location = new Point(workingArea.Right - Width - ScreenMargin, workingArea.Bottom - Height - ScreenMargin);

        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnFormClosing(e);
    }

    private void ConfigureWindow()
    {
        Text = "GitHub - assigned to me";
        Size = new Size(1260, 560);
        MinimumSize = new Size(640, 300);
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        Font = new Font(Theme.FontFamily, Theme.BaseFontSize);
        BackColor = SystemColors.Window;
    }

    private void ConfigureTabs()
    {
        _tabs.Dock = DockStyle.Fill;
        _tabs.Padding = new Point(18, 6);
        _tabs.SelectedIndexChanged += (_, _) =>
            (_tabs.SelectedTab == _pullRequestsTab ? _pullRequestList : _issueList).FocusRows();
    }

    private void AddListTab(TabPage page, ItemListControl list)
    {
        page.Padding = new Padding(0);
        page.BackColor = SystemColors.Window;

        list.Dock = DockStyle.Fill;
        list.OpenRequested += BrowserLauncher.Open;
        list.RefreshRequested += () => RefreshRequested?.Invoke();

        page.Controls.Add(list);
        _tabs.TabPages.Add(page);
    }
}
