using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace GhTray.UI;

internal sealed class ColumnHeaderView : Border
{
    private const double HeaderHeight = 28;
    private const string AscendingArrow = " ▲";
    private const string DescendingArrow = " ▼";

    private readonly ColumnLayout _layout;
    private readonly Dictionary<ListColumn, TextBlock> _titles = [];

    public ColumnHeaderView(ColumnLayout layout)
    {
        _layout = layout;
        Height = HeaderHeight;
        Background = Palette.HeaderFill;
        BorderBrush = Palette.HeaderBorder;
        BorderThickness = new Thickness(0, 0, 0, 1);

        var grid = new Grid { ColumnDefinitions = layout.CreateColumnDefinitions() };
        for (var index = 0; index < layout.Columns.Count; index++)
        {
            var spec = layout.Columns[index];
            var isSortable = ColumnSorter.IsSortable(spec.Column);
            var title = new TextBlock
            {
                Text = spec.Title,
                FontWeight = FontWeight.Bold,
                Foreground = Palette.Muted,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var cell = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(10, 0),
                Child = title,
            };
            title.HorizontalAlignment = spec.Column == ListColumn.Number ? HorizontalAlignment.Right : HorizontalAlignment.Left;

            if (isSortable)
            {
                var column = spec.Column;
                cell.Cursor = new Cursor(StandardCursorType.Hand);
                cell.PointerPressed += (_, e) =>
                {
                    if (!e.GetCurrentPoint(cell).Properties.IsLeftButtonPressed) return;

                    SortRequested?.Invoke(column);
                    e.Handled = true;
                };
            }

            _titles[spec.Column] = title;
            Grid.SetColumn(cell, index);
            grid.Children.Add(cell);
        }

        Child = grid;
    }

    public event Action<ListColumn>? SortRequested;

    public void ShowSort(ListColumn column, bool descending)
    {
        foreach (var spec in _layout.Columns)
        {
            var title = _titles[spec.Column];
            var isSorted = spec.Column == column;
            title.Text = isSorted ? spec.Title + (descending ? DescendingArrow : AscendingArrow) : spec.Title;
            title.Foreground = isSorted ? Palette.Text : Palette.Muted;
        }
    }
}
