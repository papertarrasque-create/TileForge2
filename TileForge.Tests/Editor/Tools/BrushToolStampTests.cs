using Microsoft.Xna.Framework.Input;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Editor.Tools;
using TileForge.Tests.Helpers;
using Xunit;

namespace TileForge.Tests.Editor.Tools;

public class BrushToolStampTests
{
    private static EditorState CreateState(int mapW = 10, int mapH = 10)
    {
        var state = new EditorState
        {
            Map = new MapData(mapW, mapH),
            Sheet = new MockSpriteSheet(),
        };
        state.ActiveLayerName = "Ground";
        return state;
    }

    private static TileClipboard Create2x2Clipboard()
    {
        // 2x2 clipboard: "grass", "wall", "sand", null
        return new TileClipboard(2, 2, new[] { "grass", "wall", "sand", null });
    }

    // ===== Stamp mode activation =====

    [Fact]
    public void Paint_WithClipboard_StampsPattern()
    {
        var state = CreateState();
        state.Clipboard = Create2x2Clipboard();
        var brush = new BrushTool();

        brush.OnPress(3, 3, state);
        brush.OnRelease(state);

        var layer = state.Map.GetLayer("Ground");
        Assert.Equal("grass", layer.GetCell(3, 3, 10));
        Assert.Equal("wall", layer.GetCell(4, 3, 10));
        Assert.Equal("sand", layer.GetCell(3, 4, 10));
        Assert.Null(layer.GetCell(4, 4, 10)); // null cell in clipboard = not painted
    }

    [Fact]
    public void Paint_WithClipboard_SkipsNullCells()
    {
        var state = CreateState();
        // Pre-fill the cell that should be skipped
        state.Map.GetLayer("Ground").SetCell(4, 4, 10, "existing");
        state.Clipboard = Create2x2Clipboard();
        var brush = new BrushTool();

        brush.OnPress(3, 3, state);
        brush.OnRelease(state);

        // Cell at (4,4) in clipboard is null — existing value should be preserved
        Assert.Equal("existing", state.Map.GetLayer("Ground").GetCell(4, 4, 10));
    }

    [Fact]
    public void Paint_WithClipboard_ClipsToMapBounds()
    {
        var state = CreateState(5, 5);
        state.Clipboard = Create2x2Clipboard();
        var brush = new BrushTool();

        // Stamp at (4,4) — only (4,4) is in bounds from the 2x2 stamp
        brush.OnPress(4, 4, state);
        brush.OnRelease(state);

        Assert.Equal("grass", state.Map.GetLayer("Ground").GetCell(4, 4, 5));
        Assert.True(state.UndoStack.CanUndo);
    }

    [Fact]
    public void Paint_WithClipboard_RecordsChangesForUndo()
    {
        var state = CreateState();
        state.Clipboard = Create2x2Clipboard();
        var brush = new BrushTool();

        brush.OnPress(0, 0, state);
        brush.OnRelease(state);

        Assert.True(state.UndoStack.CanUndo);

        // Undo should restore all cells to null
        state.UndoStack.Undo();
        var layer = state.Map.GetLayer("Ground");
        Assert.Null(layer.GetCell(0, 0, 10));
        Assert.Null(layer.GetCell(1, 0, 10));
        Assert.Null(layer.GetCell(0, 1, 10));
    }

    [Fact]
    public void Paint_WithClipboard_SkipsDuplicateValues()
    {
        var state = CreateState();
        // Pre-fill (0,0) with "grass" — stamp has "grass" at (0,0), should skip it
        state.Map.GetLayer("Ground").SetCell(0, 0, 10, "grass");
        state.Clipboard = Create2x2Clipboard();
        var brush = new BrushTool();

        brush.OnPress(0, 0, state);
        brush.OnRelease(state);

        // Should still have painted the other cells
        Assert.Equal("wall", state.Map.GetLayer("Ground").GetCell(1, 0, 10));

        // Undo and verify (0,0) was NOT part of the stroke (still "grass")
        state.UndoStack.Undo();
        Assert.Equal("grass", state.Map.GetLayer("Ground").GetCell(0, 0, 10));
    }

    [Fact]
    public void Paint_NoClipboard_PaintsSingleTile()
    {
        var state = CreateState();
        var group = new TileGroup
        {
            Name = "grass",
            Type = GroupType.Tile,
            Sprites = new() { new SpriteRef { Col = 0, Row = 0 } },
        };
        state.AddGroup(group);
        state.SelectedGroupName = "grass";
        var brush = new BrushTool();

        brush.OnPress(2, 3, state);
        brush.OnRelease(state);

        Assert.Equal("grass", state.Map.GetLayer("Ground").GetCell(2, 3, 10));
    }

    [Fact]
    public void Paint_NullMap_DoesNotThrow()
    {
        var state = new EditorState { Sheet = new MockSpriteSheet() };
        state.Clipboard = Create2x2Clipboard();
        var brush = new BrushTool();

        var ex = Record.Exception(() =>
        {
            brush.OnPress(0, 0, state);
            brush.OnRelease(state);
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Paint_NullLayer_DoesNotThrow()
    {
        var state = CreateState();
        state.ActiveLayerName = "NonExistent";
        state.Clipboard = Create2x2Clipboard();
        var brush = new BrushTool();

        var ex = Record.Exception(() =>
        {
            brush.OnPress(0, 0, state);
            brush.OnRelease(state);
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Paint_EmptyClipboard_DoesNotThrow()
    {
        var state = CreateState();
        state.Clipboard = new TileClipboard(2, 2, new string[4]); // all nulls
        var brush = new BrushTool();

        brush.OnPress(0, 0, state);
        brush.OnRelease(state);

        // No changes should have been recorded
        Assert.False(state.UndoStack.CanUndo);
    }

    [Fact]
    public void Drag_PaintsMultipleStampPositions()
    {
        var state = CreateState();
        state.Clipboard = new TileClipboard(1, 1, new[] { "grass" });
        var brush = new BrushTool();

        brush.OnPress(0, 0, state);
        brush.OnDrag(1, 0, state);
        brush.OnDrag(2, 0, state);
        brush.OnRelease(state);

        var layer = state.Map.GetLayer("Ground");
        Assert.Equal("grass", layer.GetCell(0, 0, 10));
        Assert.Equal("grass", layer.GetCell(1, 0, 10));
        Assert.Equal("grass", layer.GetCell(2, 0, 10));
    }

    // ===== Escape clears clipboard before selection =====

    [Fact]
    public void Escape_WithClipboard_ClearsClipboardFirst()
    {
        var state = CreateState();
        state.Clipboard = Create2x2Clipboard();
        state.TileSelection = new Microsoft.Xna.Framework.Rectangle(0, 0, 2, 2);

        var router = new InputRouter(
            state,
            save: () => { }, open: () => { },
            enterPlayMode: () => { }, exitPlayMode: () => { },
            exitGame: () => { }, resizeMap: () => { });

        var current = new KeyboardState(Keys.Escape);
        var prev = new KeyboardState();
        router.Update(current, prev, default);

        // Clipboard should be cleared but selection preserved
        Assert.Null(state.Clipboard);
        Assert.NotNull(state.TileSelection);
    }

    [Fact]
    public void Escape_WithClipboardAndSelection_SecondEscapeClearsSelection()
    {
        var state = CreateState();
        state.Clipboard = Create2x2Clipboard();
        state.TileSelection = new Microsoft.Xna.Framework.Rectangle(0, 0, 2, 2);

        var router = new InputRouter(
            state,
            save: () => { }, open: () => { },
            enterPlayMode: () => { }, exitPlayMode: () => { },
            exitGame: () => { }, resizeMap: () => { });

        // First escape: clears clipboard
        var current = new KeyboardState(Keys.Escape);
        var prev = new KeyboardState();
        router.Update(current, prev, default);

        Assert.Null(state.Clipboard);
        Assert.NotNull(state.TileSelection);

        // Second escape: clears selection
        router.Update(current, prev, default);

        Assert.Null(state.TileSelection);
    }
}
