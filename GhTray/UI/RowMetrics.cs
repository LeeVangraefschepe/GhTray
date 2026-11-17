using GhTray.Models;

namespace GhTray.UI;

internal sealed class RowMetrics(bool isPullRequestList)
{
    public const int RowHeight = 32;
    public const int HeaderHeight = 28;
    public const int FailureLineHeight = 17;
    public const int MaxFailureLines = 8;

    private const int FailureBottomPadding = 6;

    public IReadOnlyList<string> FailedChecksToShow(WorkItem item) =>
        isPullRequestList && item.IsAssigned && item.Checks is { Failed: > 0 } checks ? checks.FailedNames : [];

    public int HeightOf(WorkItem? item)
    {
        if (item is null) return RowHeight;

        var failedCount = FailedChecksToShow(item).Count;
        if (failedCount == 0) return RowHeight;

        var overflowLine = failedCount > MaxFailureLines ? 1 : 0;
        var lineCount = Math.Min(failedCount, MaxFailureLines + overflowLine);
        return RowHeight + lineCount * FailureLineHeight + FailureBottomPadding;
    }
}
