using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class GameMenuListTests
{
    // =========================================================================
    // Constructor
    // =========================================================================

    [Fact]
    public void DefaultConstructor_BothFieldsAreZero()
    {
        var menu = new GameMenuList();

        Assert.Equal(0, menu.SelectedIndex);
        Assert.Equal(0, menu.ScrollOffset);
    }

    [Fact]
    public void ExplicitConstructor_SetsSelectedIndex()
    {
        var menu = new GameMenuList(selectedIndex: 3);

        Assert.Equal(3, menu.SelectedIndex);
    }

    [Fact]
    public void ExplicitConstructor_SetsScrollOffset()
    {
        var menu = new GameMenuList(scrollOffset: 5);

        Assert.Equal(5, menu.ScrollOffset);
    }

    [Fact]
    public void ExplicitConstructor_SetsBothFields()
    {
        var menu = new GameMenuList(selectedIndex: 2, scrollOffset: 4);

        Assert.Equal(2, menu.SelectedIndex);
        Assert.Equal(4, menu.ScrollOffset);
    }

    // =========================================================================
    // MoveUp
    // =========================================================================

    [Fact]
    public void MoveUp_FromZero_WrapsToLast()
    {
        var menu = new GameMenuList(selectedIndex: 0);
        menu.MoveUp(5);

        Assert.Equal(4, menu.SelectedIndex);
    }

    [Fact]
    public void MoveUp_FromMiddle_Decrements()
    {
        var menu = new GameMenuList(selectedIndex: 3);
        menu.MoveUp(5);

        Assert.Equal(2, menu.SelectedIndex);
    }

    [Fact]
    public void MoveUp_CountZero_IsNoOp()
    {
        var menu = new GameMenuList(selectedIndex: 2);
        menu.MoveUp(0);

        Assert.Equal(2, menu.SelectedIndex);
    }

    [Fact]
    public void MoveUp_CountOne_StaysAtZero()
    {
        var menu = new GameMenuList(selectedIndex: 0);
        menu.MoveUp(1);

        Assert.Equal(0, menu.SelectedIndex);
    }

    [Fact]
    public void MoveUp_NegativeCount_IsNoOp()
    {
        var menu = new GameMenuList(selectedIndex: 2);
        menu.MoveUp(-1);

        Assert.Equal(2, menu.SelectedIndex);
    }

    // =========================================================================
    // MoveDown
    // =========================================================================

    [Fact]
    public void MoveDown_FromLast_WrapsToZero()
    {
        var menu = new GameMenuList(selectedIndex: 4);
        menu.MoveDown(5);

        Assert.Equal(0, menu.SelectedIndex);
    }

    [Fact]
    public void MoveDown_FromMiddle_Increments()
    {
        var menu = new GameMenuList(selectedIndex: 2);
        menu.MoveDown(5);

        Assert.Equal(3, menu.SelectedIndex);
    }

    [Fact]
    public void MoveDown_CountZero_IsNoOp()
    {
        var menu = new GameMenuList(selectedIndex: 2);
        menu.MoveDown(0);

        Assert.Equal(2, menu.SelectedIndex);
    }

    [Fact]
    public void MoveDown_CountOne_StaysAtZero()
    {
        var menu = new GameMenuList(selectedIndex: 0);
        menu.MoveDown(1);

        Assert.Equal(0, menu.SelectedIndex);
    }

    [Fact]
    public void MoveDown_NegativeCount_IsNoOp()
    {
        var menu = new GameMenuList(selectedIndex: 2);
        menu.MoveDown(-3);

        Assert.Equal(2, menu.SelectedIndex);
    }

    // =========================================================================
    // ClampIndex
    // =========================================================================

    [Fact]
    public void ClampIndex_InRange_Unchanged()
    {
        var menu = new GameMenuList(selectedIndex: 2);
        menu.ClampIndex(5);

        Assert.Equal(2, menu.SelectedIndex);
    }

    [Fact]
    public void ClampIndex_AboveRange_ClampsToLast()
    {
        var menu = new GameMenuList(selectedIndex: 10);
        menu.ClampIndex(5);

        Assert.Equal(4, menu.SelectedIndex);
    }

    [Fact]
    public void ClampIndex_NegativeIndex_ClampsToZero()
    {
        var menu = new GameMenuList(selectedIndex: -1);
        menu.ClampIndex(5);

        Assert.Equal(0, menu.SelectedIndex);
    }

    [Fact]
    public void ClampIndex_ZeroCount_SetsToZero()
    {
        var menu = new GameMenuList(selectedIndex: 3);
        menu.ClampIndex(0);

        Assert.Equal(0, menu.SelectedIndex);
    }

    // =========================================================================
    // ScrollUp
    // =========================================================================

    [Fact]
    public void ScrollUp_FromPositive_Decrements()
    {
        var menu = new GameMenuList(scrollOffset: 3);
        menu.ScrollUp();

        Assert.Equal(2, menu.ScrollOffset);
    }

    [Fact]
    public void ScrollUp_FromZero_StaysAtZero()
    {
        var menu = new GameMenuList(scrollOffset: 0);
        menu.ScrollUp();

        Assert.Equal(0, menu.ScrollOffset);
    }

    // =========================================================================
    // ScrollDown
    // =========================================================================

    [Fact]
    public void ScrollDown_BelowMax_Increments()
    {
        var menu = new GameMenuList(scrollOffset: 2);
        menu.ScrollDown(totalLines: 20, visibleLines: 10);

        Assert.Equal(3, menu.ScrollOffset);
    }

    [Fact]
    public void ScrollDown_AtMax_StaysAtMax()
    {
        var menu = new GameMenuList(scrollOffset: 10);
        menu.ScrollDown(totalLines: 20, visibleLines: 10);

        Assert.Equal(10, menu.ScrollOffset);
    }

    [Fact]
    public void ScrollDown_TotalLessThanOrEqualVisible_StaysAtZero()
    {
        var menu = new GameMenuList(scrollOffset: 0);
        menu.ScrollDown(totalLines: 5, visibleLines: 10);

        Assert.Equal(0, menu.ScrollOffset);
    }

    [Fact]
    public void ScrollDown_TotalEqualsVisible_StaysAtZero()
    {
        var menu = new GameMenuList(scrollOffset: 0);
        menu.ScrollDown(totalLines: 10, visibleLines: 10);

        Assert.Equal(0, menu.ScrollOffset);
    }

    // =========================================================================
    // ClampScroll
    // =========================================================================

    [Fact]
    public void ClampScroll_AboveMax_ClampsDown()
    {
        var menu = new GameMenuList(scrollOffset: 15);
        menu.ClampScroll(totalLines: 20, visibleLines: 10);

        Assert.Equal(10, menu.ScrollOffset);
    }

    [Fact]
    public void ClampScroll_AtMax_Unchanged()
    {
        var menu = new GameMenuList(scrollOffset: 10);
        menu.ClampScroll(totalLines: 20, visibleLines: 10);

        Assert.Equal(10, menu.ScrollOffset);
    }

    [Fact]
    public void ClampScroll_NegativeOffset_ClampsToZero()
    {
        var menu = new GameMenuList(scrollOffset: -1);
        menu.ClampScroll(totalLines: 20, visibleLines: 10);

        Assert.Equal(0, menu.ScrollOffset);
    }

    [Fact]
    public void ClampScroll_TotalLessThanVisible_ClampsToZero()
    {
        var menu = new GameMenuList(scrollOffset: 5);
        menu.ClampScroll(totalLines: 5, visibleLines: 10);

        Assert.Equal(0, menu.ScrollOffset);
    }

    // =========================================================================
    // Field independence
    // =========================================================================

    [Fact]
    public void MoveDown_DoesNotAffectScrollOffset()
    {
        var menu = new GameMenuList(selectedIndex: 1, scrollOffset: 7);
        menu.MoveDown(5);

        Assert.Equal(7, menu.ScrollOffset);
    }

    [Fact]
    public void ScrollDown_DoesNotAffectSelectedIndex()
    {
        var menu = new GameMenuList(selectedIndex: 3, scrollOffset: 2);
        menu.ScrollDown(totalLines: 20, visibleLines: 10);

        Assert.Equal(3, menu.SelectedIndex);
    }

    // =========================================================================
    // Round-trip
    // =========================================================================

    [Fact]
    public void MoveDown_ThenMoveUp_ReturnsSameIndex()
    {
        var menu = new GameMenuList(selectedIndex: 2);
        int original = menu.SelectedIndex;

        menu.MoveDown(5);
        menu.MoveUp(5);

        Assert.Equal(original, menu.SelectedIndex);
    }
}
