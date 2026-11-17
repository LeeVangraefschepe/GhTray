using GhTray.Models;

namespace GhTray.Storage;

public sealed class FailedCheckStore
{
    private readonly JsonStateFile<Dictionary<string, string[]>> _file = new("checks.json");
    private Dictionary<string, string[]> _failedChecksByKey = new();
    private bool _isFirstRun = true;

    public void Load()
    {
        var saved = _file.TryLoad();
        if (saved is null) return;

        _failedChecksByKey = saved;
        _isFirstRun = false;
    }

    public List<CheckFailureChange> RecordAndFindNewFailures(IReadOnlyList<WorkItem> currentItems)
    {
        var newFailures = new List<CheckFailureChange>();
        var nextState = new Dictionary<string, string[]>();

        foreach (var item in currentItems.Where(item => item.IsPullRequest))
        {
            _failedChecksByKey.TryGetValue(item.Key, out var previouslyFailed);

            if (item.Checks is null)
            {
                if (previouslyFailed is not null) nextState[item.Key] = previouslyFailed;
                continue;
            }

            var failedNames = item.Checks.FailedNames.ToArray();
            if (failedNames.Length > 0) nextState[item.Key] = failedNames;

            if (!item.IsAssigned) continue;

            var added = failedNames
                .Where(name => previouslyFailed is null || !previouslyFailed.Contains(name))
                .ToList();
            if (added.Count > 0) newFailures.Add(new CheckFailureChange(item, added));
        }

        _failedChecksByKey = nextState;
        _file.TrySave(_failedChecksByKey);

        var wasFirstRun = _isFirstRun;
        _isFirstRun = false;
        return wasFirstRun ? [] : newFailures;
    }
}
