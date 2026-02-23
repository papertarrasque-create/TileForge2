#nullable disable

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Editor.Tools;
using Xunit;

namespace TileForge.Tests.Editor;

/// <summary>
/// A minimal ITool implementation for testing ActiveTool event behavior.
/// Does not depend on MonoGame runtime — all graphics methods are no-ops.
/// </summary>
public class StubTool : ITool
{
    public string Name { get; }

    public StubTool(string name = "stub")
    {
        Name = name;
    }

    public void OnPress(int gridX, int gridY, EditorState state) { }
    public void OnDrag(int gridX, int gridY, EditorState state) { }
    public void OnRelease(EditorState state) { }
    public void DrawPreview(SpriteBatch spriteBatch, int gridX, int gridY,
                            EditorState state, Camera camera, Renderer renderer) { }
}

public class EditorStateEventTests
{
    private EditorState CreateState()
    {
        return new EditorState();
    }

    private EditorState CreateStateWithMap(int width = 10, int height = 10)
    {
        return new EditorState
        {
            Map = new MapData(width, height)
        };
    }

    // =====================================================================
    // ActiveToolChanged
    // =====================================================================

    [Fact]
    public void ActiveToolChanged_Fires_WhenSetToDifferentTool()
    {
        var state = CreateState();
        var toolA = new StubTool("A");
        var toolB = new StubTool("B");
        state.ActiveTool = toolA;

        ITool received = null;
        state.ActiveToolChanged += t => received = t;

        state.ActiveTool = toolB;

        Assert.Same(toolB, received);
    }

    [Fact]
    public void ActiveToolChanged_DoesNotFire_WhenSetToSameToolReference()
    {
        var state = CreateState();
        var tool = new StubTool("A");
        state.ActiveTool = tool;

        bool fired = false;
        state.ActiveToolChanged += _ => fired = true;

        state.ActiveTool = tool;

        Assert.False(fired);
    }

    // =====================================================================
    // ActiveLayerChanged
    // =====================================================================

    [Fact]
    public void ActiveLayerChanged_Fires_WhenLayerNameChanges()
    {
        var state = CreateState();
        string received = null;
        state.ActiveLayerChanged += v => received = v;

        state.ActiveLayerName = "Objects";

        Assert.Equal("Objects", received);
    }

    [Fact]
    public void ActiveLayerChanged_DoesNotFire_WhenSetToSameValue()
    {
        var state = CreateState();
        // Default layer name is "Ground"
        bool fired = false;
        state.ActiveLayerChanged += _ => fired = true;

        state.ActiveLayerName = "Ground";

        Assert.False(fired);
    }

    // =====================================================================
    // SelectedGroupChanged
    // =====================================================================

    [Fact]
    public void SelectedGroupChanged_Fires_WhenGroupNameChanges()
    {
        var state = CreateState();
        string received = null;
        state.SelectedGroupChanged += v => received = v;

        state.SelectedGroupName = "grass";

        Assert.Equal("grass", received);
    }

    [Fact]
    public void SelectedGroupChanged_DoesNotFire_WhenSetToSameValue()
    {
        var state = CreateState();
        state.SelectedGroupName = "grass";

        bool fired = false;
        state.SelectedGroupChanged += _ => fired = true;

        state.SelectedGroupName = "grass";

        Assert.False(fired);
    }

    [Fact]
    public void SelectedGroupChanged_Fires_WithNull_WhenCleared()
    {
        var state = CreateState();
        state.SelectedGroupName = "grass";

        string received = "sentinel";
        state.SelectedGroupChanged += v => received = v;

        state.SelectedGroupName = null;

        Assert.Null(received);
    }

    // =====================================================================
    // SelectedEntityChanged
    // =====================================================================

    [Fact]
    public void SelectedEntityChanged_Fires_WhenEntityIdChanges()
    {
        var state = CreateState();
        string received = null;
        state.SelectedEntityChanged += v => received = v;

        state.SelectedEntityId = "entity-1";

        Assert.Equal("entity-1", received);
    }

    [Fact]
    public void SelectedEntityChanged_DoesNotFire_WhenSetToSameValue()
    {
        var state = CreateState();
        state.SelectedEntityId = "entity-1";

        bool fired = false;
        state.SelectedEntityChanged += _ => fired = true;

        state.SelectedEntityId = "entity-1";

        Assert.False(fired);
    }

    // =====================================================================
    // PlayModeChanged
    // =====================================================================

    [Fact]
    public void PlayModeChanged_Fires_WhenSetToTrue()
    {
        var state = CreateState();
        bool? received = null;
        state.PlayModeChanged += v => received = v;

        state.IsPlayMode = true;

        Assert.True(received);
    }

    [Fact]
    public void PlayModeChanged_Fires_WhenSetToFalse()
    {
        var state = CreateState();
        state.IsPlayMode = true;

        bool? received = null;
        state.PlayModeChanged += v => received = v;

        state.IsPlayMode = false;

        Assert.NotNull(received);
        Assert.False(received.Value);
    }

    [Fact]
    public void PlayModeChanged_DoesNotFire_WhenSetToSameValue()
    {
        var state = CreateState();
        // Default is false
        bool fired = false;
        state.PlayModeChanged += _ => fired = true;

        state.IsPlayMode = false;

        Assert.False(fired);
    }

    // =====================================================================
    // MapDirtied
    // =====================================================================

    [Fact]
    public void MapDirtied_Fires_WhenMarkDirtyCalled()
    {
        var state = CreateState();
        bool fired = false;
        state.MapDirtied += () => fired = true;

        state.MarkDirty();

        Assert.True(fired);
    }

    [Fact]
    public void MapDirtied_Fires_OnUndoStackPush()
    {
        var state = CreateState();
        bool fired = false;
        state.MapDirtied += () => fired = true;

        state.UndoStack.Push(new MockCommand());

        Assert.True(fired);
    }

    [Fact]
    public void MapDirtied_Fires_OnUndoStackUndo()
    {
        var state = CreateState();
        state.UndoStack.Push(new MockCommand());

        bool fired = false;
        state.MapDirtied += () => fired = true;

        state.UndoStack.Undo();

        Assert.True(fired);
    }

    [Fact]
    public void MapDirtied_Fires_OnUndoStackRedo()
    {
        var state = CreateState();
        state.UndoStack.Push(new MockCommand());
        state.UndoStack.Undo();

        bool fired = false;
        state.MapDirtied += () => fired = true;

        state.UndoStack.Redo();

        Assert.True(fired);
    }

    // =====================================================================
    // UndoRedoStateChanged
    // =====================================================================

    [Fact]
    public void UndoRedoStateChanged_Fires_OnUndoStackPush()
    {
        var state = CreateState();
        bool fired = false;
        state.UndoRedoStateChanged += () => fired = true;

        state.UndoStack.Push(new MockCommand());

        Assert.True(fired);
    }

    [Fact]
    public void UndoRedoStateChanged_Fires_OnUndoStackUndo()
    {
        var state = CreateState();
        state.UndoStack.Push(new MockCommand());

        bool fired = false;
        state.UndoRedoStateChanged += () => fired = true;

        state.UndoStack.Undo();

        Assert.True(fired);
    }

    [Fact]
    public void UndoRedoStateChanged_Fires_OnUndoStackRedo()
    {
        var state = CreateState();
        state.UndoStack.Push(new MockCommand());
        state.UndoStack.Undo();

        bool fired = false;
        state.UndoRedoStateChanged += () => fired = true;

        state.UndoStack.Redo();

        Assert.True(fired);
    }

    [Fact]
    public void UndoRedoStateChanged_Fires_OnUndoStackClear()
    {
        var state = CreateState();
        state.UndoStack.Push(new MockCommand());

        bool fired = false;
        state.UndoRedoStateChanged += () => fired = true;

        state.UndoStack.Clear();

        Assert.True(fired);
    }

    // =====================================================================
    // IsDirty state tracking
    // =====================================================================

    [Fact]
    public void IsDirty_IsFalse_Initially()
    {
        var state = CreateState();

        Assert.False(state.IsDirty);
    }

    [Fact]
    public void IsDirty_IsTrue_AfterMarkDirty()
    {
        var state = CreateState();

        state.MarkDirty();

        Assert.True(state.IsDirty);
    }

    [Fact]
    public void IsDirty_IsFalse_AfterClearDirty()
    {
        var state = CreateState();
        state.MarkDirty();

        state.ClearDirty();

        Assert.False(state.IsDirty);
    }

    [Fact]
    public void IsDirty_IsTrue_AfterUndoStackPush()
    {
        var state = CreateState();

        state.UndoStack.Push(new MockCommand());

        Assert.True(state.IsDirty);
    }

    // =====================================================================
    // Multiple subscribers
    // =====================================================================

    [Fact]
    public void MultipleSubscribers_AllReceiveEvents()
    {
        var state = CreateState();
        int count1 = 0;
        int count2 = 0;
        int count3 = 0;

        state.MapDirtied += () => count1++;
        state.MapDirtied += () => count2++;
        state.MapDirtied += () => count3++;

        state.MarkDirty();

        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
        Assert.Equal(1, count3);
    }

    // =====================================================================
    // RemoveGroup fires SelectedGroupChanged
    // =====================================================================

    [Fact]
    public void RemoveGroup_WithSelectedGroup_FiresSelectedGroupChanged()
    {
        var state = CreateStateWithMap();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        var wall = new TileGroup { Name = "wall", Type = GroupType.Tile };
        state.AddGroup(grass);
        state.AddGroup(wall);
        state.SelectedGroupName = "grass";

        string received = null;
        bool fired = false;
        state.SelectedGroupChanged += v =>
        {
            fired = true;
            received = v;
        };

        state.RemoveGroup("grass");

        Assert.True(fired);
        // After removing "grass", the first remaining group "wall" becomes selected
        Assert.Equal("wall", received);
    }

    // =====================================================================
    // RenameGroup fires SelectedGroupChanged
    // =====================================================================

    [Fact]
    public void RenameGroup_WithSelectedGroup_FiresSelectedGroupChanged()
    {
        var state = CreateStateWithMap();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(grass);
        state.SelectedGroupName = "grass";

        string received = null;
        bool fired = false;
        state.SelectedGroupChanged += v =>
        {
            fired = true;
            received = v;
        };

        state.RenameGroup("grass", "tall_grass");

        Assert.True(fired);
        Assert.Equal("tall_grass", received);
    }
}
