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

internal readonly record struct ColumnBounds(ListColumn Column, string Title, int X, int Width);

internal sealed class ColumnLayout
{
    private const int MinTitleWidth = 200;
    private const int FlexibleWidth = 0;

    private readonly (ListColumn Column, string Title, int Width)[] _columns;

    public ColumnLayout(bool isPullRequest)
    {
        _columns = isPullRequest
            ?
            [
                (ListColumn.Repository, "Repository", 230),
                (ListColumn.Number, "#", 70),
                (ListColumn.Title, "Title", FlexibleWidth),
                (ListColumn.Checks, "Checks", 150),
                (ListColumn.Author, "Author", 120),
                (ListColumn.LastActivity, "Last activity", 110),
            ]
            :
            [
                (ListColumn.Repository, "Repository", 230),
                (ListColumn.Number, "#", 70),
                (ListColumn.Title, "Title", FlexibleWidth),
                (ListColumn.Author, "Author", 120),
                (ListColumn.LastActivity, "Last activity", 110),
            ];
    }

    public IReadOnlyList<ColumnBounds> Compute(int availableWidth)
    {
        var fixedWidth = _columns.Where(c => c.Column != ListColumn.Title).Sum(c => c.Width);
        var titleWidth = Math.Max(MinTitleWidth, availableWidth - fixedWidth);

        var bounds = new List<ColumnBounds>(_columns.Length);
        var x = 0;
        foreach (var (column, title, width) in _columns)
        {
            var actualWidth = column == ListColumn.Title ? titleWidth : width;
            bounds.Add(new ColumnBounds(column, title, x, actualWidth));
            x += actualWidth;
        }

        return bounds;
    }
}
