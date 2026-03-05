using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Game;
using TileForge.Tests.Helpers;
using Xunit;

namespace TileForge.Tests.Editor;

// ============================================================================
// Helpers shared across multimap tests
// ============================================================================

file class NoOpCommand : ICommand
{
    public void Execute() { }
    public void Undo() { }
}

// ============================================================================
// 1. MapDocumentState basics
// ============================================================================

public class MapDocumentStateTests
{
    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        var doc = new MapDocumentState();

        Assert.Null(doc.Name);
        Assert.Null(doc.Map);
        Assert.Equal(0f, doc.CameraX);
        Assert.Equal(0f, doc.CameraY);
        Assert.Equal(1, doc.ZoomIndex);
        Assert.Equal("Ground", doc.ActiveLayerName);
        Assert.Null(doc.SelectedEntityId);
        Assert.Null(doc.TileSelection);
        Assert.NotNull(doc.CollapsedLayers);
        Assert.Empty(doc.CollapsedLayers);
    }

    [Fact]
    public void UndoStack_IsCreatedOnConstruction()
    {
        var doc = new MapDocumentState();

        Assert.NotNull(doc.UndoStack);
    }

    [Fact]
    public void UndoStack_IsReadOnly_SameInstanceAlways()
    {
        var doc = new MapDocumentState();

        var stack1 = doc.UndoStack;
        var stack2 = doc.UndoStack;

        Assert.Same(stack1, stack2);
    }

    [Fact]
    public void TwoDocuments_HaveIndependentUndoStacks()
    {
        var docA = new MapDocumentState();
        var docB = new MapDocumentState();

        Assert.NotSame(docA.UndoStack, docB.UndoStack);
    }

    [Fact]
    public void TwoDocuments_UndoStacksAreIndependent_PushOnOneDoesNotAffectOther()
    {
        var docA = new MapDocumentState();
        var docB = new MapDocumentState();

        docA.UndoStack.Push(new NoOpCommand());

        Assert.True(docA.UndoStack.CanUndo);
        Assert.False(docB.UndoStack.CanUndo);
    }

    [Fact]
    public void TwoDocuments_UndoStacks_UndoOnOneDoesNotAffectOther()
    {
        var docA = new MapDocumentState();
        var docB = new MapDocumentState();

        docA.UndoStack.Push(new NoOpCommand());
        docB.UndoStack.Push(new NoOpCommand());
        docB.UndoStack.Push(new NoOpCommand());

        docA.UndoStack.Undo();

        Assert.False(docA.UndoStack.CanUndo);
        Assert.True(docB.UndoStack.CanUndo);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var doc = new MapDocumentState
        {
            Name = "dungeon",
            Map = new MapData(20, 15),
            CameraX = 100f,
            CameraY = 200f,
            ZoomIndex = 3,
            ActiveLayerName = "Objects",
            SelectedEntityId = "entity-abc",
            TileSelection = new Rectangle(1, 2, 3, 4),
        };

        Assert.Equal("dungeon", doc.Name);
        Assert.NotNull(doc.Map);
        Assert.Equal(20, doc.Map.Width);
        Assert.Equal(15, doc.Map.Height);
        Assert.Equal(100f, doc.CameraX);
        Assert.Equal(200f, doc.CameraY);
        Assert.Equal(3, doc.ZoomIndex);
        Assert.Equal("Objects", doc.ActiveLayerName);
        Assert.Equal("entity-abc", doc.SelectedEntityId);
        Assert.Equal(new Rectangle(1, 2, 3, 4), doc.TileSelection);
    }

    [Fact]
    public void CollapsedLayers_CanAddAndContain()
    {
        var doc = new MapDocumentState();

        doc.CollapsedLayers.Add("Ground");
        doc.CollapsedLayers.Add("Objects");

        Assert.Contains("Ground", doc.CollapsedLayers);
        Assert.Contains("Objects", doc.CollapsedLayers);
        Assert.Equal(2, doc.CollapsedLayers.Count);
    }

    [Fact]
    public void TwoDocuments_HaveIndependentCollapsedLayers()
    {
        var docA = new MapDocumentState();
        var docB = new MapDocumentState();

        docA.CollapsedLayers.Add("Ground");

        Assert.Contains("Ground", docA.CollapsedLayers);
        Assert.DoesNotContain("Ground", docB.CollapsedLayers);
    }
}

// ============================================================================
// 2. EditorState multimap facade
// ============================================================================

public class EditorStateMultimapTests
{
    private static MapData MakeMap(int w = 10, int h = 10) => new MapData(w, h);

    private static MapDocumentState MakeDoc(string name, MapData map = null) =>
        new MapDocumentState { Name = name, Map = map ?? MakeMap() };

    // --- Map property delegates to ActiveMapDocument ---

    [Fact]
    public void Map_Get_ReturnsNull_WhenNoDocuments()
    {
        var state = new EditorState();

        Assert.Null(state.Map);
    }

    [Fact]
    public void Map_Get_ReturnsActiveDocumentMap()
    {
        var state = new EditorState();
        var map = MakeMap();
        state.MapDocuments.Add(MakeDoc("main", map));
        state.ActiveMapIndex = 0;

        Assert.Same(map, state.Map);
    }

    [Fact]
    public void Map_Set_UpdatesActiveDocumentMap()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("main"));
        state.ActiveMapIndex = 0;

        var newMap = MakeMap(20, 20);
        state.Map = newMap;

        Assert.Same(newMap, state.Map);
        Assert.Same(newMap, state.MapDocuments[0].Map);
    }

    // --- ActiveMapIndex switching changes Map property ---

    [Fact]
    public void ActiveMapIndex_Switch_ChangesMapProperty()
    {
        var state = new EditorState();
        var mapA = MakeMap(10, 10);
        var mapB = MakeMap(20, 20);
        state.MapDocuments.Add(MakeDoc("map-a", mapA));
        state.MapDocuments.Add(MakeDoc("map-b", mapB));

        state.ActiveMapIndex = 0;
        Assert.Same(mapA, state.Map);

        state.ActiveMapIndex = 1;
        Assert.Same(mapB, state.Map);
    }

    [Fact]
    public void ActiveMapIndex_SetToMinusOne_MapReturnsNull()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("main"));
        state.ActiveMapIndex = 0;

        state.ActiveMapIndex = -1;

        Assert.Null(state.Map);
    }

    [Fact]
    public void ActiveMapIndex_SetBeyondRange_ClampedToLastIndex()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("main"));
        state.ActiveMapIndex = 0;

        state.ActiveMapIndex = 999;

        Assert.Equal(0, state.ActiveMapIndex);
    }

    [Fact]
    public void ActiveMapIndex_SetBelowMinusOne_ClampedToMinusOne()
    {
        var state = new EditorState();

        state.ActiveMapIndex = -5;

        Assert.Equal(-1, state.ActiveMapIndex);
    }

    [Fact]
    public void ActiveMapDocument_ReturnsCorrectDoc_AfterSwitch()
    {
        var state = new EditorState();
        var docA = MakeDoc("alpha");
        var docB = MakeDoc("beta");
        state.MapDocuments.Add(docA);
        state.MapDocuments.Add(docB);

        state.ActiveMapIndex = 1;

        Assert.Same(docB, state.ActiveMapDocument);
    }

    [Fact]
    public void ActiveMapDocument_ReturnsNull_WhenIndexIsMinusOne()
    {
        var state = new EditorState();

        Assert.Null(state.ActiveMapDocument);
    }

    // --- Auto-create MapDocumentState when Map setter is called with no existing docs ---

    [Fact]
    public void Map_Set_WithNoDocuments_AutoCreatesDocument()
    {
        var state = new EditorState();
        var map = MakeMap();

        state.Map = map;

        Assert.Single(state.MapDocuments);
    }

    [Fact]
    public void Map_Set_WithNoDocuments_AutoCreatedDoc_HasNameMain()
    {
        var state = new EditorState();

        state.Map = MakeMap();

        Assert.Equal("main", state.MapDocuments[0].Name);
    }

    [Fact]
    public void Map_Set_WithNoDocuments_SetsActiveMapIndexToZero()
    {
        var state = new EditorState();

        state.Map = MakeMap();

        Assert.Equal(0, state.ActiveMapIndex);
    }

    [Fact]
    public void Map_Set_WithNoDocuments_MapIsAccessibleViaProperty()
    {
        var state = new EditorState();
        var map = MakeMap(5, 5);

        state.Map = map;

        Assert.Same(map, state.Map);
    }

    [Fact]
    public void Map_Set_Null_WithNoDocuments_DoesNotCreateDocument()
    {
        var state = new EditorState();

        state.Map = null;

        Assert.Empty(state.MapDocuments);
    }

    // --- UndoStack delegates to active document ---

    [Fact]
    public void UndoStack_ReturnsActiveDocumentStack()
    {
        var state = new EditorState();
        var doc = MakeDoc("main");
        state.MapDocuments.Add(doc);
        state.ActiveMapIndex = 0;

        Assert.Same(doc.UndoStack, state.UndoStack);
    }

    [Fact]
    public void UndoStack_ReturnsFallback_WhenNoActiveDocument()
    {
        var state = new EditorState();

        // Should not throw and should return a valid UndoStack
        var stack = state.UndoStack;
        Assert.NotNull(stack);
    }

    [Fact]
    public void UndoStack_SwitchMap_ReturnsDifferentStack()
    {
        var state = new EditorState();
        var docA = MakeDoc("alpha");
        var docB = MakeDoc("beta");
        state.MapDocuments.Add(docA);
        state.MapDocuments.Add(docB);

        state.ActiveMapIndex = 0;
        var stackA = state.UndoStack;

        state.ActiveMapIndex = 1;
        var stackB = state.UndoStack;

        Assert.NotSame(stackA, stackB);
        Assert.Same(docA.UndoStack, stackA);
        Assert.Same(docB.UndoStack, stackB);
    }

    [Fact]
    public void UndoStack_PushOnActiveDoc_DoesNotAffectOtherDoc()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("alpha"));
        state.MapDocuments.Add(MakeDoc("beta"));

        state.ActiveMapIndex = 0;
        state.UndoStack.Push(new NoOpCommand());

        state.ActiveMapIndex = 1;
        Assert.False(state.UndoStack.CanUndo);
    }

    // --- ActiveLayerName delegates to active document ---

    [Fact]
    public void ActiveLayerName_Get_ReturnsActiveDocumentLayer()
    {
        var state = new EditorState();
        var doc = MakeDoc("main");
        doc.ActiveLayerName = "Objects";
        state.MapDocuments.Add(doc);
        state.ActiveMapIndex = 0;

        Assert.Equal("Objects", state.ActiveLayerName);
    }

    [Fact]
    public void ActiveLayerName_Set_UpdatesActiveDocument()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("main"));
        state.ActiveMapIndex = 0;

        state.ActiveLayerName = "Objects";

        Assert.Equal("Objects", state.MapDocuments[0].ActiveLayerName);
    }

    [Fact]
    public void ActiveLayerName_PerDoc_IsIndependent()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("alpha"));
        state.MapDocuments.Add(MakeDoc("beta"));

        state.ActiveMapIndex = 0;
        state.ActiveLayerName = "Objects";

        state.ActiveMapIndex = 1;
        // beta doc defaults to "Ground"
        Assert.Equal("Ground", state.ActiveLayerName);
    }

    [Fact]
    public void ActiveLayerName_SwitchMap_RestoresDocLayer()
    {
        var state = new EditorState();
        var docA = MakeDoc("alpha");
        docA.ActiveLayerName = "Objects";
        var docB = MakeDoc("beta");
        docB.ActiveLayerName = "Ground";
        state.MapDocuments.Add(docA);
        state.MapDocuments.Add(docB);

        state.ActiveMapIndex = 0;
        Assert.Equal("Objects", state.ActiveLayerName);

        state.ActiveMapIndex = 1;
        Assert.Equal("Ground", state.ActiveLayerName);
    }

    // --- SelectedEntityId delegates to active document ---

    [Fact]
    public void SelectedEntityId_Get_ReturnsActiveDocumentEntityId()
    {
        var state = new EditorState();
        var doc = MakeDoc("main");
        doc.SelectedEntityId = "ent-001";
        state.MapDocuments.Add(doc);
        state.ActiveMapIndex = 0;

        Assert.Equal("ent-001", state.SelectedEntityId);
    }

    [Fact]
    public void SelectedEntityId_Set_UpdatesActiveDocument()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("main"));
        state.ActiveMapIndex = 0;

        state.SelectedEntityId = "ent-xyz";

        Assert.Equal("ent-xyz", state.MapDocuments[0].SelectedEntityId);
    }

    [Fact]
    public void SelectedEntityId_PerDoc_IsIndependent()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("alpha"));
        state.MapDocuments.Add(MakeDoc("beta"));

        state.ActiveMapIndex = 0;
        state.SelectedEntityId = "ent-alpha";

        state.ActiveMapIndex = 1;
        Assert.Null(state.SelectedEntityId);
    }

    [Fact]
    public void SelectedEntityId_SwitchMap_RestoresDocEntityId()
    {
        var state = new EditorState();
        var docA = MakeDoc("alpha");
        docA.SelectedEntityId = "ent-a";
        var docB = MakeDoc("beta");
        docB.SelectedEntityId = "ent-b";
        state.MapDocuments.Add(docA);
        state.MapDocuments.Add(docB);

        state.ActiveMapIndex = 0;
        Assert.Equal("ent-a", state.SelectedEntityId);

        state.ActiveMapIndex = 1;
        Assert.Equal("ent-b", state.SelectedEntityId);
    }

    // --- Group rename propagates across all maps ---

    [Fact]
    public void RenameGroup_PropagatesAcrossAllMaps()
    {
        var state = new EditorState();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);

        var mapA = MakeMap();
        var mapB = MakeMap();
        mapA.GetLayer("Ground").SetCell(0, 0, mapA.Width, "grass");
        mapB.GetLayer("Ground").SetCell(1, 1, mapB.Width, "grass");

        state.MapDocuments.Add(new MapDocumentState { Name = "map-a", Map = mapA });
        state.MapDocuments.Add(new MapDocumentState { Name = "map-b", Map = mapB });
        state.ActiveMapIndex = 0;

        state.RenameGroup("grass", "tall_grass");

        Assert.Equal("tall_grass", mapA.GetLayer("Ground").GetCell(0, 0, mapA.Width));
        Assert.Equal("tall_grass", mapB.GetLayer("Ground").GetCell(1, 1, mapB.Width));
    }

    [Fact]
    public void RenameGroup_PropagatesEntityReferencesAcrossAllMaps()
    {
        var state = new EditorState();
        var group = new TileGroup { Name = "door", Type = GroupType.Entity };
        state.AddGroup(group);

        var mapA = MakeMap();
        var mapB = MakeMap();
        mapA.Entities.Add(new Entity { GroupName = "door", X = 1, Y = 1 });
        mapB.Entities.Add(new Entity { GroupName = "door", X = 2, Y = 2 });

        state.MapDocuments.Add(new MapDocumentState { Name = "map-a", Map = mapA });
        state.MapDocuments.Add(new MapDocumentState { Name = "map-b", Map = mapB });
        state.ActiveMapIndex = 0;

        state.RenameGroup("door", "iron_door");

        Assert.Equal("iron_door", mapA.Entities[0].GroupName);
        Assert.Equal("iron_door", mapB.Entities[0].GroupName);
    }

    [Fact]
    public void RenameGroup_DoesNotRenameUnrelatedGroupsInOtherMaps()
    {
        var state = new EditorState();
        var grass = new TileGroup { Name = "grass", Type = GroupType.Tile };
        var wall = new TileGroup { Name = "wall", Type = GroupType.Tile };
        state.AddGroup(grass);
        state.AddGroup(wall);

        var mapA = MakeMap();
        var mapB = MakeMap();
        mapA.GetLayer("Ground").SetCell(0, 0, mapA.Width, "grass");
        mapB.GetLayer("Ground").SetCell(0, 0, mapB.Width, "wall");

        state.MapDocuments.Add(new MapDocumentState { Name = "map-a", Map = mapA });
        state.MapDocuments.Add(new MapDocumentState { Name = "map-b", Map = mapB });
        state.ActiveMapIndex = 0;

        state.RenameGroup("grass", "tall_grass");

        // wall reference in map-b should be unchanged
        Assert.Equal("wall", mapB.GetLayer("Ground").GetCell(0, 0, mapB.Width));
    }

    // --- Group delete propagates across all maps ---

    [Fact]
    public void RemoveGroup_ClearsCellsAcrossAllMaps()
    {
        var state = new EditorState();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);

        var mapA = MakeMap();
        var mapB = MakeMap();
        mapA.GetLayer("Ground").SetCell(0, 0, mapA.Width, "grass");
        mapB.GetLayer("Objects").SetCell(2, 2, mapB.Width, "grass");

        state.MapDocuments.Add(new MapDocumentState { Name = "map-a", Map = mapA });
        state.MapDocuments.Add(new MapDocumentState { Name = "map-b", Map = mapB });
        state.ActiveMapIndex = 0;

        state.RemoveGroup("grass");

        Assert.Null(mapA.GetLayer("Ground").GetCell(0, 0, mapA.Width));
        Assert.Null(mapB.GetLayer("Objects").GetCell(2, 2, mapB.Width));
    }

    [Fact]
    public void RemoveGroup_RemovesEntitiesAcrossAllMaps()
    {
        var state = new EditorState();
        var door = new TileGroup { Name = "door", Type = GroupType.Entity };
        state.AddGroup(door);

        var mapA = MakeMap();
        var mapB = MakeMap();
        mapA.Entities.Add(new Entity { GroupName = "door", X = 1, Y = 1 });
        mapB.Entities.Add(new Entity { GroupName = "door", X = 3, Y = 3 });

        state.MapDocuments.Add(new MapDocumentState { Name = "map-a", Map = mapA });
        state.MapDocuments.Add(new MapDocumentState { Name = "map-b", Map = mapB });
        state.ActiveMapIndex = 0;

        state.RemoveGroup("door");

        Assert.Empty(mapA.Entities);
        Assert.Empty(mapB.Entities);
    }

    [Fact]
    public void RemoveGroup_DoesNotRemoveEntitiesOfOtherGroupsInOtherMaps()
    {
        var state = new EditorState();
        var door = new TileGroup { Name = "door", Type = GroupType.Entity };
        var chest = new TileGroup { Name = "chest", Type = GroupType.Entity };
        state.AddGroup(door);
        state.AddGroup(chest);

        var mapA = MakeMap();
        var mapB = MakeMap();
        mapA.Entities.Add(new Entity { GroupName = "door", X = 1, Y = 1 });
        mapB.Entities.Add(new Entity { GroupName = "chest", X = 2, Y = 2 });

        state.MapDocuments.Add(new MapDocumentState { Name = "map-a", Map = mapA });
        state.MapDocuments.Add(new MapDocumentState { Name = "map-b", Map = mapB });
        state.ActiveMapIndex = 0;

        state.RemoveGroup("door");

        Assert.Empty(mapA.Entities);
        Assert.Single(mapB.Entities);
        Assert.Equal("chest", mapB.Entities[0].GroupName);
    }

    // --- ActiveMapChanged event fires on tab switch ---

    [Fact]
    public void ActiveMapChanged_Fires_WhenActiveMapIndexChanges()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("alpha"));
        state.MapDocuments.Add(MakeDoc("beta"));

        MapDocumentState received = null;
        state.ActiveMapChanged += doc => received = doc;

        state.ActiveMapIndex = 1;

        Assert.NotNull(received);
        Assert.Same(state.MapDocuments[1], received);
    }

    [Fact]
    public void ActiveMapChanged_DoesNotFire_WhenIndexSetToSameValue()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("alpha"));
        state.ActiveMapIndex = 0;

        bool fired = false;
        state.ActiveMapChanged += _ => fired = true;

        state.ActiveMapIndex = 0;

        Assert.False(fired);
    }

    [Fact]
    public void ActiveMapChanged_Fires_WithNullDoc_WhenSwitchedToMinusOne()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("alpha"));
        state.ActiveMapIndex = 0;

        MapDocumentState received = new MapDocumentState(); // non-null sentinel
        bool fired = false;
        state.ActiveMapChanged += doc =>
        {
            fired = true;
            received = doc;
        };

        state.ActiveMapIndex = -1;

        Assert.True(fired);
        Assert.Null(received);
    }

    [Fact]
    public void ActiveMapChanged_Fires_WhenMapIsAutoCreatedViaSetter()
    {
        var state = new EditorState();

        bool fired = false;
        state.ActiveMapChanged += _ => fired = true;

        state.Map = MakeMap();

        Assert.True(fired);
    }

    [Fact]
    public void ActiveMapChanged_MultipleListeners_AllReceiveEvent()
    {
        var state = new EditorState();
        state.MapDocuments.Add(MakeDoc("alpha"));
        state.MapDocuments.Add(MakeDoc("beta"));

        int count = 0;
        state.ActiveMapChanged += _ => count++;
        state.ActiveMapChanged += _ => count++;

        state.ActiveMapIndex = 1;

        Assert.Equal(2, count);
    }
}

// ============================================================================
// 3. ProjectFile V2 roundtrip tests
// ============================================================================

public class ProjectFileMultimapTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectFileMultimapTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TFMultimapTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string TempProjectPath() =>
        Path.Combine(_tempDir, Guid.NewGuid().ToString("N")[..8] + ".tfp");

    private string TempSheetPath() =>
        Path.Combine(Path.GetTempPath(), "test.png");

    private MockSpriteSheet MakeSheet() => new MockSpriteSheet(16, 16, 10, 10, 0);

    private static MapData MakeMap(int w = 10, int h = 8)
    {
        var map = new MapData(w, h);
        return map;
    }

    private static MapDocumentState MakeDoc(string name, MapData map = null) =>
        new MapDocumentState { Name = name, Map = map ?? MakeMap() };

    private static List<TileGroup> MakeGroups() => new List<TileGroup>
    {
        new TileGroup
        {
            Name = "grass",
            Type = GroupType.Tile,
            IsSolid = false,
            IsPassable = true,
            LayerName = "Ground",
        },
        new TileGroup
        {
            Name = "wall",
            Type = GroupType.Tile,
            IsSolid = true,
            IsPassable = false,
            LayerName = "Ground",
        },
    };

    // --- V2 save/load roundtrip with 3 maps ---

    [Fact]
    public void V2_Save_ProducesVersion2InFile()
    {
        var projectPath = TempProjectPath();
        var docs = new List<MapDocumentState>
        {
            MakeDoc("town"),
            MakeDoc("dungeon"),
            MakeDoc("cave"),
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), MakeGroups(), docs, null);

        var loaded = ProjectFile.Load(projectPath);
        Assert.Equal(2, loaded.Version);
    }

    [Fact]
    public void V2_Roundtrip_MapCount_Matches()
    {
        var projectPath = TempProjectPath();
        var docs = new List<MapDocumentState>
        {
            MakeDoc("town"),
            MakeDoc("dungeon"),
            MakeDoc("cave"),
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), MakeGroups(), docs, null);

        var loaded = ProjectFile.Load(projectPath);
        Assert.NotNull(loaded.Maps);
        Assert.Equal(3, loaded.Maps.Count);
    }

    [Fact]
    public void V2_Roundtrip_MapNames_RoundTrip()
    {
        var projectPath = TempProjectPath();
        var docs = new List<MapDocumentState>
        {
            MakeDoc("town"),
            MakeDoc("dungeon"),
            MakeDoc("cave"),
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), MakeGroups(), docs, null);

        var loaded = ProjectFile.Load(projectPath);
        var names = loaded.Maps.Select(m => m.Name).ToList();
        Assert.Contains("town", names);
        Assert.Contains("dungeon", names);
        Assert.Contains("cave", names);
    }

    [Fact]
    public void V2_Roundtrip_MapDimensions_RoundTrip()
    {
        var projectPath = TempProjectPath();
        var docs = new List<MapDocumentState>
        {
            new MapDocumentState { Name = "town",    Map = new MapData(20, 15) },
            new MapDocumentState { Name = "dungeon", Map = new MapData(10,  8) },
            new MapDocumentState { Name = "cave",    Map = new MapData( 5, 12) },
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), MakeGroups(), docs, null);

        var loaded = ProjectFile.Load(projectPath);
        var byName = loaded.Maps.ToDictionary(m => m.Name);

        Assert.Equal(20, byName["town"].Map.Width);
        Assert.Equal(15, byName["town"].Map.Height);
        Assert.Equal(10, byName["dungeon"].Map.Width);
        Assert.Equal(8,  byName["dungeon"].Map.Height);
        Assert.Equal(5,  byName["cave"].Map.Width);
        Assert.Equal(12, byName["cave"].Map.Height);
    }

    [Fact]
    public void V2_Roundtrip_Entities_RoundTrip()
    {
        var projectPath = TempProjectPath();
        var townMap = new MapData(10, 10);
        townMap.Entities.Add(new Entity { Id = "ent-1", GroupName = "npc", X = 3, Y = 4 });
        townMap.Entities.Add(new Entity { Id = "ent-2", GroupName = "chest", X = 7, Y = 2 });

        var dungeonMap = new MapData(10, 10);
        dungeonMap.Entities.Add(new Entity { Id = "ent-3", GroupName = "enemy", X = 5, Y = 5 });

        var docs = new List<MapDocumentState>
        {
            new MapDocumentState { Name = "town",    Map = townMap },
            new MapDocumentState { Name = "dungeon", Map = dungeonMap },
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), new List<TileGroup>(), docs, null);

        var loaded = ProjectFile.Load(projectPath);
        var byName = loaded.Maps.ToDictionary(m => m.Name);

        Assert.Equal(2, byName["town"].Entities.Count);
        Assert.Single(byName["dungeon"].Entities);

        var ent1 = byName["town"].Entities.First(e => e.Id == "ent-1");
        Assert.Equal("npc", ent1.GroupName);
        Assert.Equal(3, ent1.X);
        Assert.Equal(4, ent1.Y);

        var ent3 = byName["dungeon"].Entities[0];
        Assert.Equal("ent-3", ent3.Id);
        Assert.Equal("enemy", ent3.GroupName);
    }

    [Fact]
    public void V2_Roundtrip_EntityProperties_RoundTrip()
    {
        var projectPath = TempProjectPath();
        var map = new MapData(10, 10);
        var entity = new Entity
        {
            Id = "ent-prop",
            GroupName = "npc",
            X = 1,
            Y = 1,
            Properties = new Dictionary<string, string>
            {
                { "dialogue_id", "intro" },
                { "behavior", "patrol" },
            }
        };
        map.Entities.Add(entity);

        var docs = new List<MapDocumentState>
        {
            new MapDocumentState { Name = "town", Map = map },
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), new List<TileGroup>(), docs, null);

        var loaded = ProjectFile.Load(projectPath);
        var ent = loaded.Maps[0].Entities[0];

        Assert.NotNull(ent.Properties);
        Assert.Equal("intro",   ent.Properties["dialogue_id"]);
        Assert.Equal("patrol",  ent.Properties["behavior"]);
    }

    [Fact]
    public void V2_Roundtrip_Layers_RoundTrip()
    {
        var projectPath = TempProjectPath();
        var map = new MapData(4, 4);
        map.GetLayer("Ground").SetCell(0, 0, 4, "grass");
        map.GetLayer("Ground").SetCell(2, 1, 4, "grass");
        map.GetLayer("Objects").SetCell(1, 1, 4, "wall");

        var docs = new List<MapDocumentState>
        {
            new MapDocumentState { Name = "test", Map = map },
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), MakeGroups(), docs, null);

        var loaded = ProjectFile.Load(projectPath);
        var restored = ProjectFile.RestoreMapDocument(loaded.Maps[0]);

        Assert.Equal("grass", restored.GetLayer("Ground").GetCell(0, 0, 4));
        Assert.Equal("grass", restored.GetLayer("Ground").GetCell(2, 1, 4));
        Assert.Equal("wall",  restored.GetLayer("Objects").GetCell(1, 1, 4));
        Assert.Null(restored.GetLayer("Ground").GetCell(3, 3, 4));
    }

    [Fact]
    public void V2_Roundtrip_V1FieldsNull_NotPresent()
    {
        var projectPath = TempProjectPath();
        var docs = new List<MapDocumentState>
        {
            MakeDoc("town"),
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), MakeGroups(), docs, null);

        var loaded = ProjectFile.Load(projectPath);

        // V2 file should have null V1 map/entities fields
        Assert.Null(loaded.Map);
        Assert.Null(loaded.Entities);
    }

    [Fact]
    public void V2_Roundtrip_Groups_RoundTrip()
    {
        var projectPath = TempProjectPath();
        var groups = MakeGroups();
        var docs = new List<MapDocumentState> { MakeDoc("town") };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), groups, docs, null);

        var loaded = ProjectFile.Load(projectPath);
        var restored = ProjectFile.RestoreGroups(loaded);

        Assert.Equal(2, restored.Count);
        var grass = restored.First(g => g.Name == "grass");
        var wall  = restored.First(g => g.Name == "wall");

        Assert.Equal(GroupType.Tile, grass.Type);
        Assert.True(grass.IsPassable);
        Assert.False(grass.IsSolid);

        Assert.Equal(GroupType.Tile, wall.Type);
        Assert.False(wall.IsPassable);
        Assert.True(wall.IsSolid);
    }

    // --- V1 backward compatibility ---

    [Fact]
    public void V1_Load_VersionIs1()
    {
        var projectPath = TempProjectPath();
        var map = MakeMap();

        // Save in V1 format
        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), MakeGroups(), map, null);

        var loaded = ProjectFile.Load(projectPath);
        Assert.Equal(1, loaded.Version);
    }

    [Fact]
    public void V1_Load_HasV1MapField()
    {
        var projectPath = TempProjectPath();
        var map = new MapData(12, 8);

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), new List<TileGroup>(), map, null);

        var loaded = ProjectFile.Load(projectPath);
        Assert.NotNull(loaded.Map);
        Assert.Equal(12, loaded.Map.Width);
        Assert.Equal(8,  loaded.Map.Height);
    }

    [Fact]
    public void V1_Load_MapsFieldIsNull()
    {
        var projectPath = TempProjectPath();
        var map = MakeMap();

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), new List<TileGroup>(), map, null);

        var loaded = ProjectFile.Load(projectPath);
        Assert.Null(loaded.Maps);
    }

    [Fact]
    public void V1_Load_RestoreMap_CreatesCorrectMapData()
    {
        var projectPath = TempProjectPath();
        var map = new MapData(6, 4);
        map.GetLayer("Ground").SetCell(0, 0, 6, "grass");

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), MakeGroups(), map, null);

        var loaded = ProjectFile.Load(projectPath);
        var restored = ProjectFile.RestoreMap(loaded);

        Assert.Equal(6, restored.Width);
        Assert.Equal(4, restored.Height);
        Assert.Equal("grass", restored.GetLayer("Ground").GetCell(0, 0, 6));
    }

    [Fact]
    public void V1_Load_Entities_RoundTrip()
    {
        var projectPath = TempProjectPath();
        var map = MakeMap();
        map.Entities.Add(new Entity { Id = "v1-ent", GroupName = "hero", X = 2, Y = 3 });

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), new List<TileGroup>(), map, null);

        var loaded = ProjectFile.Load(projectPath);
        var restored = ProjectFile.RestoreMap(loaded);

        Assert.Single(restored.Entities);
        Assert.Equal("v1-ent", restored.Entities[0].Id);
        Assert.Equal("hero",   restored.Entities[0].GroupName);
        Assert.Equal(2, restored.Entities[0].X);
        Assert.Equal(3, restored.Entities[0].Y);
    }

    [Fact]
    public void V1_Load_CanCreateSingleMapDocumentState()
    {
        var projectPath = TempProjectPath();
        var map = new MapData(8, 6);
        map.GetLayer("Ground").SetCell(1, 1, 8, "grass");

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), new List<TileGroup>(), map, null);

        var loaded = ProjectFile.Load(projectPath);

        // Caller would detect V1 (Maps == null) and create a single MapDocumentState from RestoreMap
        Assert.Null(loaded.Maps);
        var restoredMap = ProjectFile.RestoreMap(loaded);
        var singleDoc = new MapDocumentState { Name = "main", Map = restoredMap };

        Assert.Single(new[] { singleDoc });
        Assert.Equal("main", singleDoc.Name);
        Assert.Equal(8, singleDoc.Map.Width);
        Assert.Equal(6, singleDoc.Map.Height);
        Assert.Equal("grass", singleDoc.Map.GetLayer("Ground").GetCell(1, 1, 8));
    }

    // --- MapEditorStateData roundtrip ---

    [Fact]
    public void MapEditorStateData_Roundtrip_PerMapCameraAndZoom()
    {
        var projectPath = TempProjectPath();
        var docs = new List<MapDocumentState>
        {
            MakeDoc("town"),
            MakeDoc("dungeon"),
        };

        var editorState = new ProjectFile.EditorStateData
        {
            ActiveMapName = "dungeon",
            MapStates = new List<ProjectFile.MapEditorStateData>
            {
                new ProjectFile.MapEditorStateData
                {
                    MapName   = "town",
                    CameraX   = 100f,
                    CameraY   = 200f,
                    ZoomIndex = 2,
                    ActiveLayer = "Objects",
                },
                new ProjectFile.MapEditorStateData
                {
                    MapName   = "dungeon",
                    CameraX   = 50f,
                    CameraY   = 75f,
                    ZoomIndex = 0,
                    ActiveLayer = "Ground",
                },
            },
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), new List<TileGroup>(), docs, editorState);

        var loaded = ProjectFile.Load(projectPath);

        Assert.NotNull(loaded.EditorState);
        Assert.Equal("dungeon", loaded.EditorState.ActiveMapName);
        Assert.NotNull(loaded.EditorState.MapStates);
        Assert.Equal(2, loaded.EditorState.MapStates.Count);

        var townState    = loaded.EditorState.MapStates.First(s => s.MapName == "town");
        var dungeonState = loaded.EditorState.MapStates.First(s => s.MapName == "dungeon");

        Assert.Equal(100f,      townState.CameraX);
        Assert.Equal(200f,      townState.CameraY);
        Assert.Equal(2,         townState.ZoomIndex);
        Assert.Equal("Objects", townState.ActiveLayer);

        Assert.Equal(50f,       dungeonState.CameraX);
        Assert.Equal(75f,       dungeonState.CameraY);
        Assert.Equal(0,         dungeonState.ZoomIndex);
        Assert.Equal("Ground",  dungeonState.ActiveLayer);
    }

    [Fact]
    public void MapEditorStateData_Roundtrip_CollapsedLayers()
    {
        var projectPath = TempProjectPath();
        var docs = new List<MapDocumentState> { MakeDoc("town") };

        var editorState = new ProjectFile.EditorStateData
        {
            MapStates = new List<ProjectFile.MapEditorStateData>
            {
                new ProjectFile.MapEditorStateData
                {
                    MapName = "town",
                    CollapsedLayers = new List<string> { "Ground", "Objects" },
                },
            },
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), new List<TileGroup>(), docs, editorState);

        var loaded = ProjectFile.Load(projectPath);
        var townState = loaded.EditorState.MapStates[0];

        Assert.NotNull(townState.CollapsedLayers);
        Assert.Contains("Ground",  townState.CollapsedLayers);
        Assert.Contains("Objects", townState.CollapsedLayers);
        Assert.Equal(2, townState.CollapsedLayers.Count);
    }

    [Fact]
    public void MapEditorStateData_Roundtrip_ActiveMapName()
    {
        var projectPath = TempProjectPath();
        var docs = new List<MapDocumentState>
        {
            MakeDoc("world"),
            MakeDoc("shop"),
        };

        var editorState = new ProjectFile.EditorStateData
        {
            ActiveMapName = "shop",
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), new List<TileGroup>(), docs, editorState);

        var loaded = ProjectFile.Load(projectPath);

        Assert.Equal("shop", loaded.EditorState.ActiveMapName);
    }

    [Fact]
    public void MapEditorStateData_NoMapStates_NullOrEmptyRoundtrips()
    {
        var projectPath = TempProjectPath();
        var docs = new List<MapDocumentState> { MakeDoc("town") };

        var editorState = new ProjectFile.EditorStateData
        {
            ActiveMapName = "town",
            MapStates = null,
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), new List<TileGroup>(), docs, editorState);

        var loaded = ProjectFile.Load(projectPath);
        // Null MapStates is omitted (WhenWritingNull), so it comes back null
        Assert.Null(loaded.EditorState.MapStates);
    }

    [Fact]
    public void V2_Roundtrip_Spritesheet_RoundTrips()
    {
        var projectPath = TempProjectPath();
        var sheet = new MockSpriteSheet(32, 32, 8, 8, 2);
        var docs = new List<MapDocumentState> { MakeDoc("main") };

        ProjectFile.Save(projectPath, TempSheetPath(), sheet, new List<TileGroup>(), docs, null);

        var loaded = ProjectFile.Load(projectPath);

        Assert.NotNull(loaded.Spritesheet);
        Assert.Equal(32, loaded.Spritesheet.TileWidth);
        Assert.Equal(32, loaded.Spritesheet.TileHeight);
        Assert.Equal(2,  loaded.Spritesheet.Padding);
    }

    [Fact]
    public void V2_Roundtrip_RestoreMapDocument_ReturnsCorrectMap()
    {
        var projectPath = TempProjectPath();
        var map = new MapData(5, 5);
        map.GetLayer("Ground").SetCell(0, 0, 5, "grass");
        map.GetLayer("Objects").SetCell(3, 3, 5, "wall");
        map.Entities.Add(new Entity { Id = "e1", GroupName = "npc", X = 2, Y = 2 });

        var docs = new List<MapDocumentState>
        {
            new MapDocumentState { Name = "alpha", Map = map },
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), MakeGroups(), docs, null);

        var loaded = ProjectFile.Load(projectPath);
        var restored = ProjectFile.RestoreMapDocument(loaded.Maps[0]);

        Assert.Equal(5, restored.Width);
        Assert.Equal(5, restored.Height);
        Assert.Equal("grass", restored.GetLayer("Ground").GetCell(0, 0, 5));
        Assert.Equal("wall",  restored.GetLayer("Objects").GetCell(3, 3, 5));
        Assert.Single(restored.Entities);
        Assert.Equal("npc", restored.Entities[0].GroupName);
    }

    [Fact]
    public void V2_Roundtrip_ThreeMaps_AllRestoreIndependently()
    {
        var projectPath = TempProjectPath();
        var mapTown = new MapData(8, 6);
        mapTown.GetLayer("Ground").SetCell(1, 1, 8, "grass");

        var mapDungeon = new MapData(10, 10);
        mapDungeon.GetLayer("Objects").SetCell(5, 5, 10, "wall");

        var mapCave = new MapData(6, 6);
        mapCave.Entities.Add(new Entity { Id = "cave-e", GroupName = "bat", X = 3, Y = 3 });

        var docs = new List<MapDocumentState>
        {
            new MapDocumentState { Name = "town",    Map = mapTown },
            new MapDocumentState { Name = "dungeon", Map = mapDungeon },
            new MapDocumentState { Name = "cave",    Map = mapCave },
        };

        ProjectFile.Save(projectPath, TempSheetPath(), MakeSheet(), MakeGroups(), docs, null);

        var loaded = ProjectFile.Load(projectPath);
        Assert.Equal(3, loaded.Maps.Count);

        var byName = loaded.Maps.ToDictionary(m => m.Name);

        var rTown    = ProjectFile.RestoreMapDocument(byName["town"]);
        var rDungeon = ProjectFile.RestoreMapDocument(byName["dungeon"]);
        var rCave    = ProjectFile.RestoreMapDocument(byName["cave"]);

        Assert.Equal("grass", rTown.GetLayer("Ground").GetCell(1, 1, 8));
        Assert.Equal("wall",  rDungeon.GetLayer("Objects").GetCell(5, 5, 10));
        Assert.Single(rCave.Entities);
        Assert.Equal("bat", rCave.Entities[0].GroupName);

        // Cross-contamination check: town entities should be empty
        Assert.Empty(rTown.Entities);
        Assert.Empty(rDungeon.Entities);
    }
}
