using GhTray.Formatting;
using GhTray.Models;
using GhTray.Platform;

namespace GhTray.Tray;

internal sealed class NotificationComposer(INotificationSender sender)
{
    private const int MaxListedItems = 3;

    public void NotifyChanges(IReadOnlyList<ItemChange> changes)
    {
        sender.Show(changes.Count == 1 ? ComposeSingleChange(changes[0]) : ComposeChangeSummary(changes));
    }

    public void NotifyCheckFailures(IReadOnlyList<CheckFailureChange> failures)
    {
        sender.Show(failures.Count == 1 ? ComposeSingleCheckFailure(failures[0]) : ComposeCheckFailureSummary(failures));
    }

    private static NotificationMessage ComposeSingleChange(ItemChange change)
    {
        var item = change.Item;
        var kind = item.IsPullRequest ? "PR" : "Issue";
        return new NotificationMessage(
            $"{DescribeChange(change)} {kind} - {item.Reference}",
            TextTruncation.Truncate(item.Title, 200),
            NotificationSeverity.Info,
            item.Url);
    }

    private static NotificationMessage ComposeChangeSummary(IReadOnlyList<ItemChange> changes)
    {
        var lines = changes
            .Take(MaxListedItems)
            .Select(change => $"- {change.Item.Reference} {TextTruncation.Truncate(change.Item.Title, 50)}");
        return new NotificationMessage($"{changes.Count} GitHub updates", AppendRemainder(lines, changes.Count));
    }

    private static NotificationMessage ComposeSingleCheckFailure(CheckFailureChange failure)
    {
        var checkNames = string.Join(", ", failure.NewlyFailedChecks);
        return new NotificationMessage(
            $"Checks failed - {failure.Item.Reference}",
            TextTruncation.Truncate($"{checkNames}\n{failure.Item.Title}", 200),
            NotificationSeverity.Error,
            failure.Item.Url);
    }

    private static NotificationMessage ComposeCheckFailureSummary(IReadOnlyList<CheckFailureChange> failures)
    {
        var lines = failures
            .Take(MaxListedItems)
            .Select(failure =>
                $"- {failure.Item.Reference}: {TextTruncation.Truncate(string.Join(", ", failure.NewlyFailedChecks), 40)}");
        return new NotificationMessage(
            $"Checks failed on {failures.Count} of your PRs",
            AppendRemainder(lines, failures.Count),
            NotificationSeverity.Error);
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
