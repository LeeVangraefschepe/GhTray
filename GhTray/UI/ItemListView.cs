using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GhTray.Models;

namespace GhTray.UI;

internal sealed class ItemListView : Grid
{
    private static readonly string[] RowBackgroundResources =
    [
        "ListBoxItemBackground",
        "ListBoxItemBackgroundPointerOver",
        "ListBoxItemBackgroundPressed",
        "ListBoxItemBackgroundSelected",
        "ListBoxItemBackgroundSelectedPointerOver",
        "ListBoxItemBackgroundSelectedPressed",
        "ListBoxItemBackgroundSelectedUnfocused",
    ];

    private readonly bool _isPullRequestList;
    private readonly ColumnLayout _layout;
    private readonly ListBox _rows = new();
    private readonly TextBlock _emptyMessage = new();
    private IReadOnlyList<WorkItem> _items = [];

    public ItemListView(bool isPullRequestList)
    {
        _isPullRequestList = isPullRequestList;
        _layout = new ColumnLayout(isPullRequestList);

        RowDefinitions = new RowDefinitions { new(GridLength.Auto), new(1, GridUnitType.Star) };
        Background = Palette.Window;

        ConfigureRows();
        ConfigureEmptyMessage();
        ConfigureContextMenu();

        var header = new ColumnHeaderView(_layout);
        SetRow(_rows, 1);
        SetRow(_emptyMessage, 1);
        Children.Add(header);
        Children.Add(_rows);
        Children.Add(_emptyMessage);
        ShowEmptyState(true);
    }

    public event Action<string>? OpenRequested;

    public event Action? RefreshRequested;

    private ItemRow? SelectedRow => _rows.SelectedItem as ItemRow;

    public void FocusRows() => _rows.Focus();

    public void SetItems(IReadOnlyList<WorkItem> items)
    {
        _items = items;

        var selectedKey = SelectedRow?.Item.Key;
        var scroll = FindScrollViewer();
        var scrollOffset = scroll?.Offset;

        var rows = items.Select((item, index) => new ItemRow(item, index)).ToList();
        _rows.ItemsSource = rows;
        _rows.SelectedItem = rows.FirstOrDefault(row => row.Item.Key == selectedKey);

        if (scroll is not null && scrollOffset is { } offset)
            Dispatcher.UIThread.Post(() => scroll.Offset = offset, DispatcherPriority.Loaded);

        ShowEmptyState(items.Count == 0);
    }

    public void RebuildRows() => SetItems(_items);

    private void ConfigureRows()
    {
        _rows.Background = Palette.Window;
        _rows.BorderThickness = new Thickness(0);
        _rows.SelectionMode = SelectionMode.Single;
        _rows.ItemTemplate = new FuncDataTemplate<ItemRow>((row, _) => new ItemRowView(_layout, row, _isPullRequestList));
        _rows.Styles.Add(CreateCompactRowStyle());
        foreach (var resource in RowBackgroundResources) _rows.Resources[resource] = Brushes.Transparent;

        _rows.SelectionChanged += OnSelectionChanged;
        _rows.DoubleTapped += OnDoubleTapped;
        _rows.KeyDown += OnKeyDown;
    }

    private void ConfigureEmptyMessage()
    {
        _emptyMessage.Foreground = Palette.Muted;
        _emptyMessage.HorizontalAlignment = HorizontalAlignment.Center;
        _emptyMessage.VerticalAlignment = VerticalAlignment.Top;
        _emptyMessage.Margin = new Thickness(0, 40, 0, 0);
        _emptyMessage.Text = _isPullRequestList
            ? "No open pull requests assigned to you or awaiting your review"
            : "No open issues assigned to you";
    }

    private void ConfigureContextMenu()
    {
        var openItem = new MenuItem { Header = "Open in GitHub" };
        openItem.Click += (_, _) => OpenSelected();

        var copyItem = new MenuItem { Header = "Copy link" };
        copyItem.Click += async (_, _) => await CopySelectedLinkAsync();

        _rows.ContextMenu = new ContextMenu { ItemsSource = new[] { openItem, copyItem } };
    }

    private static Style CreateCompactRowStyle() =>
        new(selector => selector.OfType<ListBoxItem>())
        {
            Setters =
            {
                new Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
                new Setter(MinHeightProperty, 0.0),
            },
        };

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        foreach (var removed in e.RemovedItems.OfType<ItemRow>()) removed.IsSelected = false;
        foreach (var added in e.AddedItems.OfType<ItemRow>()) added.IsSelected = true;
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var isOnRow = (e.Source as Visual)?.GetSelfAndVisualAncestors().OfType<ListBoxItem>().Any() ?? false;
        if (isOnRow) OpenSelected();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                OpenSelected();
                e.Handled = true;
                break;
            case Key.F5:
                RefreshRequested?.Invoke();
                e.Handled = true;
                break;
        }
    }

    private void OpenSelected()
    {
        if (SelectedRow is { } row) OpenRequested?.Invoke(row.Item.Url);
    }

    private async Task CopySelectedLinkAsync()
    {
        if (SelectedRow is not { } row) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null) await clipboard.SetTextAsync(row.Item.Url);
    }

    private ScrollViewer? FindScrollViewer() =>
        _rows.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

    private void ShowEmptyState(bool isEmpty)
    {
        _emptyMessage.IsVisible = isEmpty;
        _rows.IsVisible = !isEmpty;
    }
}
