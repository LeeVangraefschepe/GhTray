namespace GhTray.Models;

public sealed record CheckFailureChange(WorkItem Item, IReadOnlyList<string> NewlyFailedChecks);
