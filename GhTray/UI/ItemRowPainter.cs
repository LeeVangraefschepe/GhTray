using GhTray.Formatting;
using GhTray.Models;

namespace GhTray.UI;

internal sealed class ItemRowPainter : IDisposable
{
    private const int CellPadding = 10;
    private const int PillHeight = 16;
    private const int PillGap = 8;
    private const int CountSpacing = 10;
    private const int ReviewPillWidth = 52;
    private const int DraftPillWidth = 44;

    private const TextFormatFlags CellTextFlags =
        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
        TextFormatFlags.SingleLine | TextFormatFlags.Left;

    private readonly bool _isPullRequestList;
    private readonly RowMetrics _metrics;
    private readonly Font _regularFont;
    private readonly Font _boldFont;
    private readonly Font _pillFont = new(Theme.FontFamily, 7.5f, FontStyle.Bold);
    private readonly Font _failureFont = new(Theme.FontFamily, 8f);

    public ItemRowPainter(Font baseFont, bool isPullRequestList, RowMetrics metrics)
    {
        _regularFont = baseFont;
        _boldFont = new Font(baseFont, FontStyle.Bold);
        _isPullRequestList = isPullRequestList;
        _metrics = metrics;
    }

    public void PaintHeader(Graphics graphics, Rectangle bounds, IReadOnlyList<ColumnBounds> columns)
    {
        using (var background = new SolidBrush(Theme.HeaderFill)) graphics.FillRectangle(background, bounds);
        using (var border = new Pen(Theme.HeaderBorder))
            graphics.DrawLine(border, 0, RowMetrics.HeaderHeight - 1, bounds.Width, RowMetrics.HeaderHeight - 1);

        foreach (var column in columns)
        {
            var area = Rectangle.Inflate(new Rectangle(column.X, 0, column.Width, RowMetrics.HeaderHeight), -CellPadding, 0);
            var alignment = column.Column == ListColumn.Number ? TextFormatFlags.Right : TextFormatFlags.Left;
            TextRenderer.DrawText(graphics, column.Title, _boldFont, area, Theme.Muted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | alignment);
        }
    }

    public void PaintRow(
        Graphics graphics, Rectangle bounds, WorkItem item, bool isSelected, int rowIndex, IReadOnlyList<ColumnBounds> columns)
    {
        FillBackground(graphics, bounds, isSelected, rowIndex);

        foreach (var column in columns)
        {
            var cell = new Rectangle(bounds.Left + column.X, bounds.Top, column.Width, RowMetrics.RowHeight);
            PaintCell(graphics, column.Column, cell, item);
        }

        PaintFailedChecks(graphics, bounds, item, columns);
    }

    public void Dispose()
    {
        _boldFont.Dispose();
        _pillFont.Dispose();
        _failureFont.Dispose();
    }

    private static void FillBackground(Graphics graphics, Rectangle bounds, bool isSelected, int rowIndex)
    {
        var color = isSelected ? Theme.Selection
            : rowIndex % 2 == 1 ? Theme.Stripe
            : SystemColors.Window;
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, bounds);
    }

    private void PaintCell(Graphics graphics, ListColumn column, Rectangle cell, WorkItem item)
    {
        var textArea = Rectangle.Inflate(cell, -CellPadding, 0);

        switch (column)
        {
            case ListColumn.Repository:
                DrawText(graphics, item.Repository, _regularFont, textArea, Theme.Muted);
                break;
            case ListColumn.Number:
                DrawText(graphics, "#" + item.Number, _regularFont, textArea, Theme.Muted, TextFormatFlags.Right);
                break;
            case ListColumn.Title:
                PaintTitle(graphics, cell, textArea, item);
                break;
            case ListColumn.Checks:
                PaintCheckCounts(graphics, textArea, item.Checks);
                break;
            case ListColumn.Author:
                DrawText(graphics, item.Author, _regularFont, textArea, Theme.Muted);
                break;
            case ListColumn.LastActivity:
                PaintLastActivity(graphics, textArea, item);
                break;
        }
    }

    private void PaintTitle(Graphics graphics, Rectangle cell, Rectangle textArea, WorkItem item)
    {
        if (item.IsReviewRequested)
            textArea = DrawPill(graphics, cell, textArea, "REVIEW", ReviewPillWidth, Theme.ReviewFill, Theme.Accent);
        if (item.IsDraft && _isPullRequestList)
            textArea = DrawPill(graphics, cell, textArea, "DRAFT", DraftPillWidth, Theme.PillFill, Theme.Muted);

        var color = item.IsDraft ? Theme.Muted : SystemColors.ControlText;
        DrawText(graphics, item.Title, _regularFont, textArea, color);
    }

    private void PaintLastActivity(Graphics graphics, Rectangle textArea, WorkItem item)
    {
        var age = DateTimeOffset.UtcNow - item.UpdatedAt;
        var isWithinHour = age.TotalHours < 1;
        var color = isWithinHour ? Theme.Fresh : age.TotalDays < 1 ? Theme.Accent : Theme.Muted;
        var font = isWithinHour ? _boldFont : _regularFont;
        DrawText(graphics, RelativeTime.Describe(item.UpdatedAt), font, textArea, color);
    }

    private void PaintCheckCounts(Graphics graphics, Rectangle textArea, CheckSummary? checks)
    {
        if (checks is null)
        {
            DrawText(graphics, "-", _regularFont, textArea, Theme.Muted);
            return;
        }

        if (checks.Total == 0)
        {
            DrawText(graphics, "no checks", _regularFont, textArea, Theme.Muted);
            return;
        }

        DrawCount(graphics, ref textArea, $"✓ {checks.Passed}", checks.Passed > 0 ? Theme.Fresh : Theme.Muted);
        DrawCount(graphics, ref textArea, $"✗ {checks.Failed}", checks.Failed > 0 ? Theme.Failure : Theme.Muted);
        if (checks.Pending > 0)
            DrawCount(graphics, ref textArea, $"● {checks.Pending}", Theme.Pending);
    }

    private void DrawCount(Graphics graphics, ref Rectangle area, string text, Color color)
    {
        var measured = TextRenderer.MeasureText(
            graphics, text, _regularFont, new Size(int.MaxValue, area.Height),
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(graphics, text, _regularFont, area, color, CellTextFlags | TextFormatFlags.NoPadding);

        var consumed = measured.Width + CountSpacing;
        area = new Rectangle(area.Left + consumed, area.Top, Math.Max(0, area.Width - consumed), area.Height);
    }

    private void PaintFailedChecks(Graphics graphics, Rectangle rowBounds, WorkItem item, IReadOnlyList<ColumnBounds> columns)
    {
        var failedChecks = _metrics.FailedChecksToShow(item);
        if (failedChecks.Count == 0) return;

        var title = columns.First(column => column.Column == ListColumn.Title);
        var spanWidth = columns.Where(column => column.Column is ListColumn.Title or ListColumn.Checks).Sum(column => column.Width);
        var line = new Rectangle(
            rowBounds.Left + title.X + CellPadding,
            rowBounds.Top + RowMetrics.RowHeight,
            Math.Max(0, spanWidth - 2 * CellPadding),
            RowMetrics.FailureLineHeight);

        var shownCount = Math.Min(failedChecks.Count, RowMetrics.MaxFailureLines);
        for (var i = 0; i < shownCount; i++)
        {
            DrawText(graphics, "✗ " + failedChecks[i], _failureFont, line, Theme.Failure);
            line.Y += RowMetrics.FailureLineHeight;
        }

        if (failedChecks.Count > shownCount)
            DrawText(graphics, $"...and {failedChecks.Count - shownCount} more", _failureFont, line, Theme.Muted);
    }

    private Rectangle DrawPill(
        Graphics graphics, Rectangle cell, Rectangle textArea, string text, int width, Color fill, Color foreground)
    {
        var pill = new Rectangle(textArea.Left, cell.Top + (cell.Height - PillHeight) / 2, width, PillHeight);
        using (var brush = new SolidBrush(fill)) graphics.FillRectangle(brush, pill);

        TextRenderer.DrawText(graphics, text, _pillFont, pill, foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        var consumed = width + PillGap;
        return new Rectangle(textArea.Left + consumed, textArea.Top, Math.Max(0, textArea.Width - consumed), textArea.Height);
    }

    private static void DrawText(
        Graphics graphics, string text, Font font, Rectangle area, Color color, TextFormatFlags extraFlags = TextFormatFlags.Left) =>
        TextRenderer.DrawText(graphics, text, font, area, color, CellTextFlags | extraFlags);
}
