using GhTray.Models;

namespace GhTray.UI;

internal sealed class ItemRow(WorkItem item, int index)
{
    private bool _isSelected;

    public event Action? SelectionChanged;

    public WorkItem Item { get; } = item;

    public bool IsStriped { get; } = index % 2 == 1;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;

            _isSelected = value;
            SelectionChanged?.Invoke();
        }
    }
}
