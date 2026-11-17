using GhTray.Models;

namespace GhTray.Storage;

public sealed class SeenActivityStore
{
    private readonly JsonStateFile<Dictionary<string, DateTimeOffset>> _file = new("seen.json");
    private Dictionary<string, DateTimeOffset> _lastSeenByKey = new();

    public bool IsEmpty => _lastSeenByKey.Count == 0;

    public void Load() => _lastSeenByKey = _file.TryLoad() ?? new();

    public List<ItemChange> RecordAndFindChanges(IReadOnlyList<WorkItem> currentItems)
    {
        var changes = new List<ItemChange>();
        foreach (var item in currentItems)
        {
            if (!_lastSeenByKey.TryGetValue(item.Key, out var lastSeen))
                changes.Add(new ItemChange(item, IsNew: true));
            else if (item.UpdatedAt > lastSeen)
                changes.Add(new ItemChange(item, IsNew: false));
        }

        _lastSeenByKey = currentItems.ToDictionary(item => item.Key, item => item.UpdatedAt);
        _file.TrySave(_lastSeenByKey);
        return changes;
    }
}
