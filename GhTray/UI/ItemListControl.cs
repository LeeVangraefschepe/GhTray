using GhTray.Models;

namespace GhTray.UI;

internal sealed class ItemListControl : Panel
{
    private readonly ColumnLayout _columnLayout;
    private readonly RowMetrics _rowMetrics;
    private readonly ItemRowPainter _painter;
    private readonly RowListBox _rows = new();
    private readonly DoubleBufferedPanel _header = new();
    private readonly Label _emptyMessage = new();

    public ItemListControl(bool isPullRequestList)
    {
        BackColor = SystemColors.Window;
        Font = new Font(Theme.FontFamily, Theme.BaseFontSize);

        _columnLayout = new ColumnLayout(isPullRequestList);
        _rowMetrics = new RowMetrics(isPullRequestList);
        _painter = new ItemRowPainter(Font, isPullRequestList, _rowMetrics);

        ConfigureRows();
        ConfigureEmptyMessage(isPullRequestList);
        ConfigureHeader();

        // Fill controls first, Top last, so docking resolves correctly.
        Controls.Add(_rows);
        Controls.Add(_emptyMessage);
        Controls.Add(_header);
        ShowEmptyState(true);

        ConfigureContextMenu();
        ConfigureInput();
    }

    public event Action<string>? OpenRequested;

    public event Action? RefreshRequested;

    private WorkItem? SelectedItem => _rows.SelectedItem as WorkItem;

    private int AvailableColumnsWidth => _rows.Visible ? _rows.ClientSize.Width : ClientSize.Width;

    public bool FocusRows() => _rows.Focus();

    public void SetItems(IReadOnlyList<WorkItem> items)
    {
        var selectedKey = SelectedItem?.Key;
        var topIndex = Math.Max(0, _rows.TopIndex);

        _rows.BeginUpdate();
        _rows.Items.Clear();

        var selectedIndex = -1;
        foreach (var item in items)
        {
            if (item.Key == selectedKey) selectedIndex = _rows.Items.Count;
            _rows.Items.Add(item);
        }

        if (selectedIndex >= 0) _rows.SelectedIndex = selectedIndex;
        if (_rows.Items.Count > 0) _rows.TopIndex = Math.Min(topIndex, _rows.Items.Count - 1);
        _rows.EndUpdate();

        ShowEmptyState(items.Count == 0);
        _rows.Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _painter.Dispose();
        base.Dispose(disposing);
    }

    private void ConfigureRows()
    {
        _rows.Dock = DockStyle.Fill;
        _rows.Font = Font;
        _rows.BackColor = SystemColors.Window;
        _rows.MeasureItem += (_, e) => e.ItemHeight = _rowMetrics.HeightOf(ItemAt(e.Index));
        _rows.DrawItem += (_, e) => PaintRow(e);
        _rows.ClientSizeChanged += (_, _) =>
        {
            _header.Invalidate();
            _rows.Invalidate();
        };
    }

    private void ConfigureEmptyMessage(bool isPullRequestList)
    {
        _emptyMessage.Dock = DockStyle.Fill;
        _emptyMessage.Font = Font;
        _emptyMessage.ForeColor = Theme.Muted;
        _emptyMessage.TextAlign = ContentAlignment.TopCenter;
        _emptyMessage.Padding = new Padding(0, 40, 0, 0);
        _emptyMessage.Text = isPullRequestList
            ? "No open pull requests assigned to you or awaiting your review"
            : "No open issues assigned to you";
    }

    private void ConfigureHeader()
    {
        _header.Dock = DockStyle.Top;
        _header.Height = RowMetrics.HeaderHeight;
        _header.Paint += (_, e) =>
            _painter.PaintHeader(e.Graphics, _header.ClientRectangle, _columnLayout.Compute(AvailableColumnsWidth));
    }

    private void ConfigureContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open in GitHub", null, (_, _) => OpenSelected());
        menu.Items.Add("Copy link", null, (_, _) =>
        {
            if (SelectedItem is { } item) Clipboard.SetText(item.Url);
        });
        _rows.ContextMenuStrip = menu;
    }

    private void ConfigureInput()
    {
        _rows.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;

            var index = _rows.IndexFromPoint(e.Location);
            if (index >= 0) _rows.SelectedIndex = index;
        };

        _rows.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left && _rows.IndexFromPoint(e.Location) >= 0) OpenSelected();
        };

        _rows.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                OpenSelected();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                RefreshRequested?.Invoke();
                e.Handled = true;
            }
        };
    }

    private WorkItem? ItemAt(int index) =>
        index >= 0 && index < _rows.Items.Count ? _rows.Items[index] as WorkItem : null;

    private void PaintRow(DrawItemEventArgs e)
    {
        if (ItemAt(e.Index) is not { } item) return;

        var isSelected = (e.State & DrawItemState.Selected) != 0;
        _painter.PaintRow(e.Graphics, e.Bounds, item, isSelected, e.Index, _columnLayout.Compute(AvailableColumnsWidth));
    }

    private void OpenSelected()
    {
        if (SelectedItem is { } item) OpenRequested?.Invoke(item.Url);
    }

    private void ShowEmptyState(bool isEmpty)
    {
        _emptyMessage.Visible = isEmpty;
        _rows.Visible = !isEmpty;
    }
}
