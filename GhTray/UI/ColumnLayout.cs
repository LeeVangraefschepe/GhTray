using Avalonia.Controls;

namespace GhTray.UI;

internal enum ListColumn
{
    Repository,
    Number,
    Title,
    Checks,
    Author,
    LastActivity,
}

internal sealed record ColumnSpec(ListColumn Column, string Title, double Width);

internal sealed class ColumnLayout
{
    private const double MinTitleWidth = 200;
    private const double FlexibleWidth = 0;

    public ColumnLayout(bool isPullRequestList)
    {
        List<ColumnSpec> columns =
        [
            new(ListColumn.Repository, "Repository", 230),
            new(ListColumn.Number, "#", 70),
            new(ListColumn.Title, "Title", FlexibleWidth),
        ];

        if (isPullRequestList) columns.Add(new ColumnSpec(ListColumn.Checks, "Checks", 150));

        columns.Add(new ColumnSpec(ListColumn.Author, "Author", 120));
        columns.Add(new ColumnSpec(ListColumn.LastActivity, "Last activity", 110));
        Columns = columns;
    }

    public IReadOnlyList<ColumnSpec> Columns { get; }

    public int IndexOf(ListColumn column) =>
        Columns.Select((spec, index) => (spec, index)).First(entry => entry.spec.Column == column).index;

    public bool Has(ListColumn column) => Columns.Any(spec => spec.Column == column);

    public ColumnDefinitions CreateColumnDefinitions()
    {
        var definitions = new ColumnDefinitions();
        foreach (var spec in Columns)
        {
            definitions.Add(spec.Column == ListColumn.Title
                ? new ColumnDefinition(1, GridUnitType.Star) { MinWidth = MinTitleWidth }
                : new ColumnDefinition(spec.Width, GridUnitType.Pixel));
        }

        return definitions;
    }
}
