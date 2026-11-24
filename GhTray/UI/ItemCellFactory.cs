using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GhTray.Formatting;
using GhTray.Models;

namespace GhTray.UI;

internal static class ItemCellFactory
{
    private const double CellPaddingX = 10;
    private const double PillSpacing = 8;

    public static Control Create(ListColumn column, WorkItem item, bool isPullRequestList) => column switch
    {
        ListColumn.Repository => Label(item.Repository, Palette.Muted),
        ListColumn.Number => Label("#" + item.Number, Palette.Muted, HorizontalAlignment.Right),
        ListColumn.Title => CreateTitle(item, isPullRequestList),
        ListColumn.Checks => CreateCheckCounts(item.Checks),
        ListColumn.BaseBranch => Label(item.BaseBranch ?? "-", Palette.Muted),
        ListColumn.Author => Label(item.Author, Palette.Muted),
        ListColumn.LastActivity => CreateLastActivity(item),
        _ => throw new ArgumentOutOfRangeException(nameof(column)),
    };

    public static Control CreateFailureLines(IReadOnlyList<string> lines)
    {
        var panel = new StackPanel { Margin = new Thickness(CellPaddingX, 0, CellPaddingX, 6) };
        for (var i = 0; i < lines.Count; i++)
        {
            var isOverflowLine = lines[i].StartsWith("...", StringComparison.Ordinal);
            panel.Children.Add(new TextBlock
            {
                Text = lines[i],
                FontSize = Palette.SmallFontSize,
                Foreground = isOverflowLine ? Palette.Muted : Palette.Failure,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Height = 17,
            });
        }

        return panel;
    }

    private static Control CreateTitle(WorkItem item, bool isPullRequestList)
    {
        var panel = new DockPanel { Margin = new Thickness(CellPaddingX, 0), VerticalAlignment = VerticalAlignment.Center };

        if (item.IsReviewRequested) AddPill(panel, "REVIEW", 52, Palette.ReviewFill, Palette.Accent);
        if (item.IsDraft && isPullRequestList) AddPill(panel, "DRAFT", 44, Palette.PillFill, Palette.Muted);

        panel.Children.Add(new TextBlock
        {
            Text = item.Title,
            Foreground = item.IsDraft ? Palette.Muted : Palette.Text,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        });
        return panel;
    }

    private static void AddPill(DockPanel panel, string text, double width, IBrush fill, IBrush foreground)
    {
        var pill = new Border
        {
            Width = width,
            Height = 16,
            Background = fill,
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(0, 0, PillSpacing, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = text,
                FontSize = Palette.PillFontSize,
                FontWeight = FontWeight.Bold,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        DockPanel.SetDock(pill, Dock.Left);
        panel.Children.Add(pill);
    }

    private static Control CreateCheckCounts(CheckSummary? checks)
    {
        if (checks is null) return Label("-", Palette.Muted);
        if (checks.Total == 0) return Label("no checks", Palette.Muted);

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(CellPaddingX, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        panel.Children.Add(Count($"✓ {checks.Passed}", checks.Passed > 0 ? Palette.Fresh : Palette.Muted));
        panel.Children.Add(Count($"✗ {checks.Failed}", checks.Failed > 0 ? Palette.Failure : Palette.Muted));
        if (checks.Pending > 0) panel.Children.Add(Count($"● {checks.Pending}", Palette.Pending));
        return panel;
    }

    private static Control CreateLastActivity(WorkItem item)
    {
        var age = DateTimeOffset.UtcNow - item.UpdatedAt;
        var isWithinHour = age.TotalHours < 1;
        var color = isWithinHour ? Palette.Fresh : age.TotalDays < 1 ? Palette.Accent : Palette.Muted;

        var label = (TextBlock)Label(RelativeTime.Describe(item.UpdatedAt), color);
        if (isWithinHour) label.FontWeight = FontWeight.Bold;
        return label;
    }

    private static TextBlock Count(string text, IBrush color) => new() { Text = text, Foreground = color };

    private static Control Label(string text, IBrush color, HorizontalAlignment alignment = HorizontalAlignment.Left) => new TextBlock
    {
        Text = text,
        Foreground = color,
        Margin = new Thickness(CellPaddingX, 0),
        HorizontalAlignment = alignment,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };
}
