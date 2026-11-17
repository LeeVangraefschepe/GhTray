using GhTray.Formatting;
using GhTray.Models;

namespace GhTray.Tray;

internal sealed class BalloonNotifier(NotifyIcon trayIcon)
{
    private const int BalloonDurationMs = 8000;
    private const int MaxListedItems = 3;

    public string? ClickTargetUrl { get; private set; }

    public void NotifyChanges(IReadOnlyList<ItemChange> changes)
    {
        if (changes.Count == 1) ShowSingleChange(changes[0]);
        else ShowChangeSummary(changes);
    }

    public void NotifyCheckFailures(IReadOnlyList<CheckFailureChange> failures)
    {
        if (failures.Count == 1) ShowSingleCheckFailure(failures[0]);
        else ShowCheckFailureSummary(failures);
    }

    private void ShowSingleChange(ItemChange change)
    {
        var item = change.Item;
        ClickTargetUrl = item.Url;

        var kind = item.IsPullRequest ? "PR" : "Issue";
        var title = $"{DescribeChange(change)} {kind} - {item.Reference}";
        trayIcon.ShowBalloonTip(BalloonDurationMs, title, TextTruncation.Truncate(item.Title, 200), ToolTipIcon.Info);
    }

    private void ShowChangeSummary(IReadOnlyList<ItemChange> changes)
    {
        ClickTargetUrl = null;

        var lines = changes
            .Take(MaxListedItems)
            .Select(change => $"- {change.Item.Reference} {TextTruncation.Truncate(change.Item.Title, 50)}");
        var body = AppendRemainder(lines, changes.Count);
        trayIcon.ShowBalloonTip(BalloonDurationMs, $"{changes.Count} GitHub updates", body, ToolTipIcon.Info);
    }

    private void ShowSingleCheckFailure(CheckFailureChange failure)
    {
        ClickTargetUrl = failure.Item.Url;

        var checkNames = string.Join(", ", failure.NewlyFailedChecks);
        trayIcon.ShowBalloonTip(
            BalloonDurationMs,
            $"Checks failed - {failure.Item.Reference}",
            TextTruncation.Truncate($"{checkNames}\n{failure.Item.Title}", 200),
            ToolTipIcon.Error);
    }

    private void ShowCheckFailureSummary(IReadOnlyList<CheckFailureChange> failures)
    {
        ClickTargetUrl = null;

        var lines = failures
            .Take(MaxListedItems)
            .Select(failure =>
                $"- {failure.Item.Reference}: {TextTruncation.Truncate(string.Join(", ", failure.NewlyFailedChecks), 40)}");
        var body = AppendRemainder(lines, failures.Count);
        trayIcon.ShowBalloonTip(BalloonDurationMs, $"Checks failed on {failures.Count} of your PRs", body, ToolTipIcon.Error);
    }

    private static string DescribeChange(ItemChange change)
    {
        if (!change.IsNew) return "Updated";
        return change.Item.IsAssigned ? "Assigned" : "Review requested";
    }

    private static string AppendRemainder(IEnumerable<string> lines, int totalCount)
    {
        var body = string.Join('\n', lines);
        return totalCount > MaxListedItems ? $"{body}\n...and {totalCount - MaxListedItems} more" : body;
    }
}
