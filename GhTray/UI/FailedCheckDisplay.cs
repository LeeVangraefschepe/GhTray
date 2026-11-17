using GhTray.Models;

namespace GhTray.UI;

internal static class FailedCheckDisplay
{
    public const int MaxLines = 8;

    public static IReadOnlyList<string> Lines(WorkItem item, bool isPullRequestList)
    {
        if (!isPullRequestList || !item.IsAssigned || item.Checks is not { Failed: > 0 } checks) return [];

        var lines = checks.FailedNames.Take(MaxLines).Select(name => "✗ " + name).ToList();
        if (checks.FailedNames.Count > MaxLines)
            lines.Add($"...and {checks.FailedNames.Count - MaxLines} more");
        return lines;
    }
}
