using GhTray.Models;

namespace GhTray.UI;

internal sealed record SortState(ListColumn Column, bool Descending)
{
    public static SortState Default { get; } = new(ListColumn.LastActivity, Descending: true);
}

internal static class ColumnSorter
{
    public static bool IsSortable(ListColumn column) => column != ListColumn.Checks;

    /// <summary>Clicking the active column flips its direction; a new column starts in its natural direction.</summary>
    public static SortState Toggle(SortState current, ListColumn clicked) =>
        current.Column == clicked
            ? current with { Descending = !current.Descending }
            : new SortState(clicked, Descending: clicked == ListColumn.LastActivity);

    public static IReadOnlyList<WorkItem> Sort(IEnumerable<WorkItem> items, SortState state)
    {
        var ordered = state.Column switch
        {
            ListColumn.Repository => Order(items, item => item.Repository, StringComparer.OrdinalIgnoreCase, state.Descending),
            ListColumn.Number => Order(items, item => item.Number, Comparer<int>.Default, state.Descending),
            ListColumn.Title => Order(items, item => item.Title, StringComparer.OrdinalIgnoreCase, state.Descending),
            ListColumn.BaseBranch => Order(items, item => item.BaseBranch ?? "", StringComparer.OrdinalIgnoreCase, state.Descending),
            ListColumn.Author => Order(items, item => item.Author, StringComparer.OrdinalIgnoreCase, state.Descending),
            _ => Order(items, item => item.UpdatedAt, Comparer<DateTimeOffset>.Default, state.Descending),
        };

        // Ties keep the most recently active item first, whichever way the primary column runs.
        return ordered.ThenByDescending(item => item.UpdatedAt).ToList();
    }

    private static IOrderedEnumerable<WorkItem> Order<TKey>(
        IEnumerable<WorkItem> items, Func<WorkItem, TKey> key, IComparer<TKey> comparer, bool descending) =>
        descending ? items.OrderByDescending(key, comparer) : items.OrderBy(key, comparer);
}
