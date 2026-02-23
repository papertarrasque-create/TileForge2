using TileForge.Data;
using TileForge.Editor;
using TileForge.Editor.Tools;
using Xunit;

namespace TileForge.Tests.Editor.Tools;

public class PickerToolTests
{
    /// <summary>
    /// Creates an EditorState with a MapData, an active layer, and registered tile groups.
    /// </summary>
    private static EditorState CreateState(int width = 10, int height = 10)
    {
        var state = new EditorState
        {
            Map = new MapData(width, height),
        };
        return state;
    }

    /// <summary>
    /// Registers a tile group in the editor state.
    /// </summary>
    private static TileGroup AddTileGroup(EditorState state, string name, GroupType type = GroupType.Tile)
    {
        var group = new TileGroup
        {
            Name = name,
            Type = type,
            LayerName = "Ground",
        };
        state.AddGroup(group);
        return group;
    }

    [Fact]
    public void Name_ReturnsPicker()
    {
        var tool = new PickerTool();

        Assert.Equal("Picker", tool.Name);
    }

    [Fact]
    public void OnPress_CellWithTileGroup_SetsSelectedGroupName()
    {
        var state = CreateState();
        AddTileGroup(state, "grass");
        state.ActiveLayerName = "Ground";
        state.Map.GetLayer("Ground").SetCell(3, 4, state.Map.Width, "grass");

        var tool = new PickerTool();
        tool.OnPress(3, 4, state);

        Assert.Equal("grass", state.SelectedGroupName);
    }

    [Fact]
    public void OnPress_CellWithTileGroup_SwitchesToBrushTool()
    {
        var state = CreateState();
        AddTileGroup(state, "grass");
        state.ActiveLayerName = "Ground";
        state.Map.GetLayer("Ground").SetCell(3, 4, state.Map.Width, "grass");

        var tool = new PickerTool();
        tool.OnPress(3, 4, state);

        Assert.IsType<BrushTool>(state.ActiveTool);
    }

    [Fact]
    public void OnPress_EmptyCell_DoesNotChangeSelectedGroupName()
    {
        var state = CreateState();
        AddTileGroup(state, "grass");
        state.SelectedGroupName = "grass";
        state.ActiveLayerName = "Ground";

        var tool = new PickerTool();
        tool.OnPress(0, 0, state);

        // Should remain unchanged because cell (0,0) is null
        Assert.Equal("grass", state.SelectedGroupName);
    }

    [Fact]
    public void OnPress_CellWithEntityTypeGroup_SwitchesToEntityTool()
    {
        var state = CreateState();
        AddTileGroup(state, "door", GroupType.Entity);
        state.ActiveLayerName = "Ground";
        state.Map.GetLayer("Ground").SetCell(2, 2, state.Map.Width, "door");

        var tool = new PickerTool();
        tool.OnPress(2, 2, state);

        Assert.IsType<EntityTool>(state.ActiveTool);
        Assert.Equal("door", state.SelectedGroupName);
    }

    [Fact]
    public void OnPress_PicksFromActiveLayer_NotOtherLayers()
    {
        var state = CreateState();
        AddTileGroup(state, "grass");
        AddTileGroup(state, "wall");

        // Paint "wall" on Objects layer, leave Ground empty
        state.Map.GetLayer("Objects").SetCell(1, 1, state.Map.Width, "wall");

        // Active layer is Ground, which has nothing at (1,1)
        state.ActiveLayerName = "Ground";
        state.SelectedGroupName = "grass";

        var tool = new PickerTool();
        tool.OnPress(1, 1, state);

        // Should not pick "wall" from Objects layer
        Assert.Equal("grass", state.SelectedGroupName);
    }

    [Fact]
    public void OnPress_EntityAtPosition_SelectsEntityGroupAndSwitchesToEntityTool()
    {
        var state = CreateState();
        AddTileGroup(state, "chest", GroupType.Entity);

        var entity = new Entity { GroupName = "chest", X = 5, Y = 5 };
        state.Map.Entities.Add(entity);

        var tool = new PickerTool();
        tool.OnPress(5, 5, state);

        Assert.Equal("chest", state.SelectedGroupName);
        Assert.Equal(entity.Id, state.SelectedEntityId);
        Assert.IsType<EntityTool>(state.ActiveTool);
    }

    [Fact]
    public void OnPress_EntityTakesPriorityOverTileInCell()
    {
        var state = CreateState();
        AddTileGroup(state, "grass");
        AddTileGroup(state, "chest", GroupType.Entity);

        // Place a tile AND an entity at the same position
        state.Map.GetLayer("Ground").SetCell(5, 5, state.Map.Width, "grass");
        var entity = new Entity { GroupName = "chest", X = 5, Y = 5 };
        state.Map.Entities.Add(entity);

        state.ActiveLayerName = "Ground";

        var tool = new PickerTool();
        tool.OnPress(5, 5, state);

        // Entity should win
        Assert.Equal("chest", state.SelectedGroupName);
        Assert.IsType<EntityTool>(state.ActiveTool);
    }

    [Fact]
    public void OnRelease_DoesNotThrow()
    {
        var state = CreateState();
        var tool = new PickerTool();

        var exception = Record.Exception(() => tool.OnRelease(state));

        Assert.Null(exception);
    }

    [Fact]
    public void OnDrag_DoesNotThrow()
    {
        var state = CreateState();
        var tool = new PickerTool();

        var exception = Record.Exception(() => tool.OnDrag(3, 4, state));

        Assert.Null(exception);
    }

    [Fact]
    public void OnPress_OutOfBounds_DoesNotChangeState()
    {
        var state = CreateState(5, 5);
        AddTileGroup(state, "grass");
        state.SelectedGroupName = "grass";
        state.ActiveLayerName = "Ground";

        var tool = new PickerTool();
        tool.OnPress(10, 10, state);

        Assert.Equal("grass", state.SelectedGroupName);
    }

    [Fact]
    public void OnPress_NullMap_DoesNotThrow()
    {
        var state = new EditorState();
        state.Map = null;

        var tool = new PickerTool();

        var exception = Record.Exception(() => tool.OnPress(0, 0, state));

        Assert.Null(exception);
    }

    [Fact]
    public void OnPress_CellWithUnregisteredGroupName_DoesNotChangeSelection()
    {
        var state = CreateState();
        state.ActiveLayerName = "Ground";

        // Write a group name directly into the cell, but don't register it in GroupsByName
        state.Map.GetLayer("Ground").SetCell(1, 1, state.Map.Width, "unknown_group");
        state.SelectedGroupName = "original";

        var tool = new PickerTool();
        tool.OnPress(1, 1, state);

        // Should not change because "unknown_group" is not in GroupsByName
        Assert.Equal("original", state.SelectedGroupName);
    }
}
