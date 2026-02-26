namespace TileForge.Game;

/// <summary>
/// Value-type navigation state for keyboard-driven list screens.
/// Tracks a cursor index (for menu screens) and a scroll offset (for document viewers).
/// </summary>
public struct GameMenuList
{
    public int SelectedIndex;
    public int ScrollOffset;

    public GameMenuList(int selectedIndex = 0, int scrollOffset = 0)
    {
        SelectedIndex = selectedIndex;
        ScrollOffset = scrollOffset;
    }

    public void MoveUp(int count)
    {
        if (count <= 0) return;
        SelectedIndex--;
        if (SelectedIndex < 0)
            SelectedIndex = count - 1;
    }

    public void MoveDown(int count)
    {
        if (count <= 0) return;
        SelectedIndex++;
        if (SelectedIndex >= count)
            SelectedIndex = 0;
    }

    public void ClampIndex(int count)
    {
        if (count <= 0)
        {
            SelectedIndex = 0;
            return;
        }
        if (SelectedIndex >= count)
            SelectedIndex = count - 1;
        if (SelectedIndex < 0)
            SelectedIndex = 0;
    }

    public void ScrollUp()
    {
        if (ScrollOffset > 0)
            ScrollOffset--;
    }

    public void ScrollDown(int totalLines, int visibleLines)
    {
        int maxOffset = totalLines > visibleLines ? totalLines - visibleLines : 0;
        if (ScrollOffset < maxOffset)
            ScrollOffset++;
    }

    public void ClampScroll(int totalLines, int visibleLines)
    {
        if (ScrollOffset < 0) ScrollOffset = 0;
        int maxOffset = totalLines > visibleLines ? totalLines - visibleLines : 0;
        if (ScrollOffset > maxOffset)
            ScrollOffset = maxOffset;
    }
}
