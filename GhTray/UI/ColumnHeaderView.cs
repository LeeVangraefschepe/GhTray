using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace GhTray.UI;

internal sealed class ColumnHeaderView : Border
{
    private const double HeaderHeight = 28;

    public ColumnHeaderView(ColumnLayout layout)
    {
        Height = HeaderHeight;
        Background = Palette.HeaderFill;
        BorderBrush = Palette.HeaderBorder;
        BorderThickness = new Thickness(0, 0, 0, 1);

        var grid = new Grid { ColumnDefinitions = layout.CreateColumnDefinitions() };
        for (var index = 0; index < layout.Columns.Count; index++)
        {
            var spec = layout.Columns[index];
            var title = new TextBlock
            {
                Text = spec.Title,
                FontWeight = FontWeight.Bold,
                Foreground = Palette.Muted,
                Margin = new Thickness(10, 0),
                HorizontalAlignment = spec.Column == ListColumn.Number ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(title, index);
            grid.Children.Add(title);
        }

        Child = grid;
    }
}
