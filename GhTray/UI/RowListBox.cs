namespace GhTray.UI;

internal sealed class RowListBox : ListBox
{
    public RowListBox()
    {
        DrawMode = DrawMode.OwnerDrawVariable;
        IntegralHeight = false;
        BorderStyle = BorderStyle.None;
        SelectionMode = SelectionMode.One;
        DoubleBuffered = true;
    }
}
