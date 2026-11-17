using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace GhTray.UI;

internal sealed class ItemRowView : Grid
{
    private const double RowHeight = 32;

    private readonly ItemRow _row;

    public ItemRowView(ColumnLayout layout, ItemRow row, bool isPullRequestList)
    {
        _row = row;

        RowDefinitions = new RowDefinitions { new(RowHeight, GridUnitType.Pixel), new(GridLength.Auto) };
        ColumnDefinitions = layout.CreateColumnDefinitions();

        AddCells(layout, isPullRequestList);
        AddFailureLines(layout, isPullRequestList);
        UpdateBackground();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _row.SelectionChanged += UpdateBackground;
        UpdateBackground();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _row.SelectionChanged -= UpdateBackground;
        base.OnDetachedFromVisualTree(e);
    }

    private void AddCells(ColumnLayout layout, bool isPullRequestList)
    {
        for (var index = 0; index < layout.Columns.Count; index++)
        {
            var cell = ItemCellFactory.Create(layout.Columns[index].Column, _row.Item, isPullRequestList);
            SetColumn(cell, index);
            Children.Add(cell);
        }
    }

    private void AddFailureLines(ColumnLayout layout, bool isPullRequestList)
    {
        var lines = FailedCheckDisplay.Lines(_row.Item, isPullRequestList);
        if (lines.Count == 0) return;

        var lineBlock = ItemCellFactory.CreateFailureLines(lines);
        SetRow(lineBlock, 1);
        SetColumn(lineBlock, layout.IndexOf(ListColumn.Title));
        SetColumnSpan(lineBlock, layout.Has(ListColumn.Checks) ? 2 : 1);
        Children.Add(lineBlock);
    }

    private void UpdateBackground()
    {
        Background = _row.IsSelected ? Palette.Selection
            : _row.IsStriped ? Palette.Stripe
            : Palette.Window;
    }
}
