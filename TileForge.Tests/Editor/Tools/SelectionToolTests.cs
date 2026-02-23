using Microsoft.Xna.Framework;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Editor.Tools;
using Xunit;

namespace TileForge.Tests.Editor.Tools;

public class SelectionToolTests
{
    private static EditorState CreateState(int width = 10, int height = 10)
    {
        return new EditorState { Map = new MapData(width, height) };
    }

    [Fact]
    public void Name_ReturnsSelection()
    {
        var tool = new SelectionTool();
        Assert.Equal("Selection", tool.Name);
    }

    [Fact]
    public void OnPress_Sets1x1SelectionAtPressPoint()
    {
        var state = CreateState();
        var tool = new SelectionTool();

        tool.OnPress(3, 4, state);

        Assert.True(state.TileSelection.HasValue);
        Assert.Equal(new Rectangle(3, 4, 1, 1), state.TileSelection.Value);
    }

    [Fact]
    public void OnDrag_ExpandsSelectionFromAnchor()
    {
        var state = CreateState();
        var tool = new SelectionTool();

        tool.OnPress(2, 2, state);
        tool.OnDrag(5, 6, state);

        Assert.True(state.TileSelection.HasValue);
        Assert.Equal(new Rectangle(2, 2, 4, 5), state.TileSelection.Value);
    }

    [Fact]
    public void OnDrag_DragToTopLeft_CalculatesCorrectRectangle()
    {
        var state = CreateState();
        var tool = new SelectionTool();

        tool.OnPress(5, 5, state);
        tool.OnDrag(2, 3, state);

        Assert.True(state.TileSelection.HasValue);
        Assert.Equal(new Rectangle(2, 3, 4, 3), state.TileSelection.Value);
    }

    [Fact]
    public void OnDrag_WithoutPress_DoesNothing()
    {
        var state = CreateState();
        var tool = new SelectionTool();

        // OnDrag without OnPress should be a no-op
        tool.OnDrag(3, 4, state);

        Assert.Null(state.TileSelection);
    }

    [Fact]
    public void OnRelease_StopsDragging()
    {
        var state = CreateState();
        var tool = new SelectionTool();

        tool.OnPress(2, 2, state);
        tool.OnDrag(5, 5, state);
        tool.OnRelease(state);

        // After release, further OnDrag should not change the selection
        var selBefore = state.TileSelection;
        tool.OnDrag(8, 8, state);
        Assert.Equal(selBefore, state.TileSelection);
    }

    [Fact]
    public void OnPress_ClickOutsideExistingSelection_ClearsAndStartsNew()
    {
        var state = CreateState();
        var tool = new SelectionTool();

        // Create a selection at (2,2)-(4,4)
        tool.OnPress(2, 2, state);
        tool.OnDrag(4, 4, state);
        tool.OnRelease(state);

        // Click outside the selection
        tool.OnPress(8, 8, state);

        // Should have a new 1x1 selection at (8,8)
        Assert.True(state.TileSelection.HasValue);
        Assert.Equal(new Rectangle(8, 8, 1, 1), state.TileSelection.Value);
    }

    [Fact]
    public void OnPress_ClickInsideExistingSelection_StartsNewSelectionAtClickPoint()
    {
        var state = CreateState();
        var tool = new SelectionTool();

        // Create a selection at (2,2)-(5,5)
        tool.OnPress(2, 2, state);
        tool.OnDrag(5, 5, state);
        tool.OnRelease(state);

        // Click inside the selection
        tool.OnPress(3, 3, state);

        // Should start a new 1x1 selection at the click point
        Assert.True(state.TileSelection.HasValue);
        Assert.Equal(new Rectangle(3, 3, 1, 1), state.TileSelection.Value);
    }

    [Fact]
    public void OnDrag_SingleCellDrag_Produces1x1Selection()
    {
        var state = CreateState();
        var tool = new SelectionTool();

        tool.OnPress(3, 3, state);
        tool.OnDrag(3, 3, state);

        Assert.Equal(new Rectangle(3, 3, 1, 1), state.TileSelection.Value);
    }

    [Fact]
    public void OnPress_WithNullMap_DoesNotThrow()
    {
        var state = new EditorState();
        state.Map = null;
        var tool = new SelectionTool();

        var ex = Record.Exception(() => tool.OnPress(0, 0, state));
        Assert.Null(ex);
    }

    [Fact]
    public void OnRelease_WithoutPress_DoesNotThrow()
    {
        var state = CreateState();
        var tool = new SelectionTool();

        var ex = Record.Exception(() => tool.OnRelease(state));
        Assert.Null(ex);
    }
}
