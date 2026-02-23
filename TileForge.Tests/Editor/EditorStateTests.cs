using TileForge.Data;
using TileForge.Editor;
using Xunit;

namespace TileForge.Tests.Editor;

public class EditorStateTests
{
    private EditorState CreateEditorWithMap(int width = 10, int height = 10)
    {
        var state = new EditorState
        {
            Map = new MapData(width, height)
        };
        return state;
    }

    // --- AddGroup ---

    [Fact]
    public void AddGroup_AddsToGroupsList()
    {
        var state = CreateEditorWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };

        state.AddGroup(group);

        Assert.Contains(group, state.Groups);
    }

    [Fact]
    public void AddGroup_AddsToGroupsByNameDictionary()
    {
        var state = CreateEditorWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };

        state.AddGroup(group);

        Assert.True(state.GroupsByName.ContainsKey("grass"));
        Assert.Same(group, state.GroupsByName["grass"]);
    }

    [Fact]
    public void AddGroup_MultipleGroups_AllAccessible()
    {
        var state = CreateEditorWithMap();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        var wall = new TileGroup { Name = "wall", Type = GroupType.Tile };

        state.AddGroup(grass);
        state.AddGroup(wall);

        Assert.Equal(2, state.Groups.Count);
        Assert.Equal(2, state.GroupsByName.Count);
        Assert.Same(grass, state.GroupsByName["grass"]);
        Assert.Same(wall, state.GroupsByName["wall"]);
    }

    // --- RemoveGroup ---

    [Fact]
    public void RemoveGroup_RemovesFromGroupsList()
    {
        var state = CreateEditorWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);

        state.RemoveGroup("grass");

        Assert.DoesNotContain(group, state.Groups);
    }

    [Fact]
    public void RemoveGroup_RemovesFromGroupsByName()
    {
        var state = CreateEditorWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);

        state.RemoveGroup("grass");

        Assert.False(state.GroupsByName.ContainsKey("grass"));
    }

    [Fact]
    public void RemoveGroup_ClearsMapCellReferences()
    {
        var state = CreateEditorWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);

        // Paint some cells with this group
        var layer = state.Map.GetLayer("Ground");
        layer.SetCell(0, 0, state.Map.Width, "grass");
        layer.SetCell(5, 5, state.Map.Width, "grass");

        state.RemoveGroup("grass");

        Assert.Null(layer.GetCell(0, 0, state.Map.Width));
        Assert.Null(layer.GetCell(5, 5, state.Map.Width));
    }

    [Fact]
    public void RemoveGroup_ClearsReferencesAcrossAllLayers()
    {
        var state = CreateEditorWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);

        // Paint on both layers
        var ground = state.Map.GetLayer("Ground");
        var objects = state.Map.GetLayer("Objects");
        ground.SetCell(0, 0, state.Map.Width, "grass");
        objects.SetCell(1, 1, state.Map.Width, "grass");

        state.RemoveGroup("grass");

        Assert.Null(ground.GetCell(0, 0, state.Map.Width));
        Assert.Null(objects.GetCell(1, 1, state.Map.Width));
    }

    [Fact]
    public void RemoveGroup_DoesNotClearOtherGroupCells()
    {
        var state = CreateEditorWithMap();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        var wall = new TileGroup { Name = "wall", Type = GroupType.Tile };
        state.AddGroup(grass);
        state.AddGroup(wall);

        var layer = state.Map.GetLayer("Ground");
        layer.SetCell(0, 0, state.Map.Width, "grass");
        layer.SetCell(1, 1, state.Map.Width, "wall");

        state.RemoveGroup("grass");

        Assert.Equal("wall", layer.GetCell(1, 1, state.Map.Width));
    }

    [Fact]
    public void RemoveGroup_RemovesEntitiesReferencingGroup()
    {
        var state = CreateEditorWithMap();
        var door = new TileGroup { Name = "door", Type = GroupType.Entity };
        state.AddGroup(door);

        state.Map.Entities.Add(new Entity { GroupName = "door", X = 3, Y = 4 });
        state.Map.Entities.Add(new Entity { GroupName = "door", X = 5, Y = 6 });

        state.RemoveGroup("door");

        Assert.Empty(state.Map.Entities);
    }

    [Fact]
    public void RemoveGroup_DoesNotRemoveOtherEntities()
    {
        var state = CreateEditorWithMap();
        var door = new TileGroup { Name = "door", Type = GroupType.Entity };
        var chest = new TileGroup { Name = "chest", Type = GroupType.Entity };
        state.AddGroup(door);
        state.AddGroup(chest);

        state.Map.Entities.Add(new Entity { GroupName = "door", X = 3, Y = 4 });
        state.Map.Entities.Add(new Entity { GroupName = "chest", X = 5, Y = 6 });

        state.RemoveGroup("door");

        Assert.Single(state.Map.Entities);
        Assert.Equal("chest", state.Map.Entities[0].GroupName);
    }

    [Fact]
    public void RemoveGroup_NonExistentGroup_DoesNotThrow()
    {
        var state = CreateEditorWithMap();

        var exception = Record.Exception(() => state.RemoveGroup("nonexistent"));

        Assert.Null(exception);
    }

    [Fact]
    public void RemoveGroup_UpdatesSelectedGroupName_WhenRemovedGroupWasSelected()
    {
        var state = CreateEditorWithMap();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        var wall = new TileGroup { Name = "wall", Type = GroupType.Tile };
        state.AddGroup(grass);
        state.AddGroup(wall);
        state.SelectedGroupName = "grass";

        state.RemoveGroup("grass");

        // Should select the first remaining group
        Assert.Equal("wall", state.SelectedGroupName);
    }

    [Fact]
    public void RemoveGroup_LastGroup_SetsSelectedGroupToNull()
    {
        var state = CreateEditorWithMap();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(grass);
        state.SelectedGroupName = "grass";

        state.RemoveGroup("grass");

        Assert.Null(state.SelectedGroupName);
    }

    // --- RenameGroup ---

    [Fact]
    public void RenameGroup_UpdatesGroupName()
    {
        var state = CreateEditorWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);

        state.RenameGroup("grass", "tall_grass");

        Assert.Equal("tall_grass", group.Name);
    }

    [Fact]
    public void RenameGroup_UpdatesGroupsByNameDictionary()
    {
        var state = CreateEditorWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);

        state.RenameGroup("grass", "tall_grass");

        Assert.False(state.GroupsByName.ContainsKey("grass"));
        Assert.True(state.GroupsByName.ContainsKey("tall_grass"));
        Assert.Same(group, state.GroupsByName["tall_grass"]);
    }

    [Fact]
    public void RenameGroup_UpdatesMapCellReferences()
    {
        var state = CreateEditorWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);

        var layer = state.Map.GetLayer("Ground");
        layer.SetCell(0, 0, state.Map.Width, "grass");
        layer.SetCell(5, 5, state.Map.Width, "grass");

        state.RenameGroup("grass", "tall_grass");

        Assert.Equal("tall_grass", layer.GetCell(0, 0, state.Map.Width));
        Assert.Equal("tall_grass", layer.GetCell(5, 5, state.Map.Width));
    }

    [Fact]
    public void RenameGroup_UpdatesEntityReferences()
    {
        var state = CreateEditorWithMap();
        var door = new TileGroup { Name = "door", Type = GroupType.Entity };
        state.AddGroup(door);

        state.Map.Entities.Add(new Entity { GroupName = "door", X = 3, Y = 4 });
        state.Map.Entities.Add(new Entity { GroupName = "door", X = 5, Y = 6 });

        state.RenameGroup("door", "iron_door");

        Assert.All(state.Map.Entities, e => Assert.Equal("iron_door", e.GroupName));
    }

    [Fact]
    public void RenameGroup_DoesNotAffectOtherGroupCellsOrEntities()
    {
        var state = CreateEditorWithMap();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        var wall = new TileGroup { Name = "wall", Type = GroupType.Tile };
        state.AddGroup(grass);
        state.AddGroup(wall);

        var layer = state.Map.GetLayer("Ground");
        layer.SetCell(0, 0, state.Map.Width, "grass");
        layer.SetCell(1, 1, state.Map.Width, "wall");

        state.RenameGroup("grass", "tall_grass");

        Assert.Equal("wall", layer.GetCell(1, 1, state.Map.Width));
    }

    [Fact]
    public void RenameGroup_UpdatesSelectedGroupName()
    {
        var state = CreateEditorWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);
        state.SelectedGroupName = "grass";

        state.RenameGroup("grass", "tall_grass");

        Assert.Equal("tall_grass", state.SelectedGroupName);
    }

    [Fact]
    public void RenameGroup_NonExistentGroup_DoesNotThrow()
    {
        var state = CreateEditorWithMap();

        var exception = Record.Exception(() => state.RenameGroup("nonexistent", "new_name"));

        Assert.Null(exception);
    }

    [Fact]
    public void RenameGroup_TargetNameAlreadyExists_DoesNotRename()
    {
        var state = CreateEditorWithMap();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        var wall = new TileGroup { Name = "wall", Type = GroupType.Tile };
        state.AddGroup(grass);
        state.AddGroup(wall);

        state.RenameGroup("grass", "wall");

        // grass should still exist with its original name since "wall" is taken
        Assert.Equal("grass", grass.Name);
        Assert.True(state.GroupsByName.ContainsKey("grass"));
    }

    // --- RebuildGroupIndex ---

    [Fact]
    public void RebuildGroupIndex_RebuildsFromGroupsList()
    {
        var state = CreateEditorWithMap();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        var wall = new TileGroup { Name = "wall", Type = GroupType.Tile };
        state.Groups.Add(grass);
        state.Groups.Add(wall);

        state.RebuildGroupIndex();

        Assert.Equal(2, state.GroupsByName.Count);
        Assert.Same(grass, state.GroupsByName["grass"]);
        Assert.Same(wall, state.GroupsByName["wall"]);
    }

    [Fact]
    public void RebuildGroupIndex_ClearsPreviousIndex()
    {
        var state = CreateEditorWithMap();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(grass);

        // Manually remove from groups list but not from dict
        state.Groups.Clear();

        state.RebuildGroupIndex();

        Assert.Empty(state.GroupsByName);
    }

    [Fact]
    public void RebuildGroupIndex_IsConsistentWithAddGroup()
    {
        var state1 = CreateEditorWithMap();
        var state2 = CreateEditorWithMap();

        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        var wall = new TileGroup { Name = "wall", Type = GroupType.Tile };

        // Method 1: AddGroup
        state1.AddGroup(new TileGroup { Name = "grass", Type = GroupType.Tile });
        state1.AddGroup(new TileGroup { Name = "wall", Type = GroupType.Tile });

        // Method 2: Manual add + rebuild
        state2.Groups.Add(new TileGroup { Name = "grass", Type = GroupType.Tile });
        state2.Groups.Add(new TileGroup { Name = "wall", Type = GroupType.Tile });
        state2.RebuildGroupIndex();

        Assert.Equal(state1.GroupsByName.Count, state2.GroupsByName.Count);
        Assert.Equal(state1.GroupsByName.Keys.OrderBy(k => k), state2.GroupsByName.Keys.OrderBy(k => k));
    }

    // --- SelectedGroup ---

    [Fact]
    public void SelectedGroup_ReturnsCorrectGroup()
    {
        var state = CreateEditorWithMap();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(grass);
        state.SelectedGroupName = "grass";

        Assert.Same(grass, state.SelectedGroup);
    }

    [Fact]
    public void SelectedGroup_ReturnsNullForMissingName()
    {
        var state = CreateEditorWithMap();
        state.SelectedGroupName = "nonexistent";

        Assert.Null(state.SelectedGroup);
    }

    [Fact]
    public void SelectedGroup_ReturnsNullWhenNameIsNull()
    {
        var state = CreateEditorWithMap();
        state.SelectedGroupName = null;

        Assert.Null(state.SelectedGroup);
    }

    // --- ActiveLayer ---

    [Fact]
    public void ActiveLayer_ReturnsGroundByDefault()
    {
        var state = CreateEditorWithMap();

        Assert.NotNull(state.ActiveLayer);
        Assert.Equal("Ground", state.ActiveLayer.Name);
    }

    [Fact]
    public void ActiveLayer_ReturnsNullWhenMapIsNull()
    {
        var state = new EditorState();

        Assert.Null(state.ActiveLayer);
    }

    [Fact]
    public void ActiveLayer_ReturnsNullForNonExistentLayerName()
    {
        var state = CreateEditorWithMap();
        state.ActiveLayerName = "NonExistent";

        Assert.Null(state.ActiveLayer);
    }
}
