using System.Text.Json;
using TileForge.Data;
using TileForge.Game;
using TileForge.Tests.Helpers;
using Xunit;

namespace TileForge.Tests.Data;

public class ProjectFileTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectFileTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TileForgeTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // --- JSON serialization options (mirror the private ones in ProjectFile) ---

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Creates a fully populated ProjectData for testing roundtrips.
    /// </summary>
    private ProjectFile.ProjectData CreateSampleProjectData()
    {
        return new ProjectFile.ProjectData
        {
            Version = 1,
            Spritesheet = new ProjectFile.SpritesheetInfo
            {
                Path = "sprites/sheet.png",
                TileWidth = 16,
                TileHeight = 16,
                Padding = 1,
            },
            Groups = new List<ProjectFile.GroupData>
            {
                new()
                {
                    Name = "grass",
                    Type = "Tile",
                    IsSolid = false,
                    IsPlayer = null,
                    Layer = "Ground",
                    Sprites = new List<ProjectFile.SpriteRefData>
                    {
                        new() { Col = 0, Row = 0 },
                        new() { Col = 1, Row = 0 },
                    }
                },
                new()
                {
                    Name = "wall",
                    Type = "Tile",
                    IsSolid = true,
                    IsPlayer = null,
                    Layer = "Objects",
                    Sprites = new List<ProjectFile.SpriteRefData>
                    {
                        new() { Col = 2, Row = 3 },
                    }
                },
                new()
                {
                    Name = "player",
                    Type = "Entity",
                    IsSolid = null,
                    IsPlayer = true,
                    Layer = "Objects",
                    Sprites = new List<ProjectFile.SpriteRefData>
                    {
                        new() { Col = 4, Row = 5 },
                    }
                },
            },
            Map = new ProjectFile.MapInfo
            {
                Width = 10,
                Height = 8,
                EntityRenderOrder = 1,
                Layers = new List<ProjectFile.LayerData>
                {
                    new()
                    {
                        Name = "Ground",
                        Visible = true,
                        Cells = CreateCellsArray(10, 8, ("grass", 0, 0), ("grass", 1, 1)),
                    },
                    new()
                    {
                        Name = "Objects",
                        Visible = false,
                        Cells = CreateCellsArray(10, 8, ("wall", 5, 3)),
                    },
                },
            },
            Entities = new List<ProjectFile.EntityData>
            {
                new()
                {
                    Id = "abc12345",
                    GroupName = "player",
                    X = 2,
                    Y = 3,
                    Properties = new Dictionary<string, string>
                    {
                        { "health", "100" },
                        { "name", "Hero" },
                    },
                },
            },
            EditorState = new ProjectFile.EditorStateData
            {
                ActiveLayer = "Ground",
                CameraX = 100f,
                CameraY = 200f,
                ZoomIndex = 3,
                PanelOrder = new List<string> { "Tools", "Map" },
                CollapsedPanels = new List<string> { "Tools" },
                CollapsedLayers = new List<string>(),
            },
        };
    }

    private static string[] CreateCellsArray(int width, int height, params (string name, int x, int y)[] placements)
    {
        var cells = new string[width * height];
        foreach (var (name, x, y) in placements)
        {
            cells[x + y * width] = name;
        }
        return cells;
    }

    // --- Save/Load roundtrip via JSON serialization ---

    [Fact]
    public void JsonRoundtrip_PreservesAllProjectData()
    {
        var original = CreateSampleProjectData();

        string json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProjectFile.ProjectData>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Version, deserialized.Version);

        // Spritesheet
        Assert.Equal(original.Spritesheet.Path, deserialized.Spritesheet.Path);
        Assert.Equal(original.Spritesheet.TileWidth, deserialized.Spritesheet.TileWidth);
        Assert.Equal(original.Spritesheet.TileHeight, deserialized.Spritesheet.TileHeight);
        Assert.Equal(original.Spritesheet.Padding, deserialized.Spritesheet.Padding);

        // Groups
        Assert.Equal(original.Groups.Count, deserialized.Groups.Count);
        for (int i = 0; i < original.Groups.Count; i++)
        {
            Assert.Equal(original.Groups[i].Name, deserialized.Groups[i].Name);
            Assert.Equal(original.Groups[i].Type, deserialized.Groups[i].Type);
            Assert.Equal(original.Groups[i].IsSolid, deserialized.Groups[i].IsSolid);
            Assert.Equal(original.Groups[i].IsPlayer, deserialized.Groups[i].IsPlayer);
            Assert.Equal(original.Groups[i].Layer, deserialized.Groups[i].Layer);
            Assert.Equal(original.Groups[i].Sprites.Count, deserialized.Groups[i].Sprites.Count);
        }

        // Map
        Assert.Equal(original.Map.Width, deserialized.Map.Width);
        Assert.Equal(original.Map.Height, deserialized.Map.Height);
        Assert.Equal(original.Map.EntityRenderOrder, deserialized.Map.EntityRenderOrder);
        Assert.Equal(original.Map.Layers.Count, deserialized.Map.Layers.Count);

        // Entities
        Assert.Equal(original.Entities.Count, deserialized.Entities.Count);
        Assert.Equal(original.Entities[0].Id, deserialized.Entities[0].Id);
        Assert.Equal(original.Entities[0].GroupName, deserialized.Entities[0].GroupName);
        Assert.Equal(original.Entities[0].X, deserialized.Entities[0].X);
        Assert.Equal(original.Entities[0].Y, deserialized.Entities[0].Y);

        // Editor state
        Assert.Equal(original.EditorState.ActiveLayer, deserialized.EditorState.ActiveLayer);
        Assert.Equal(original.EditorState.CameraX, deserialized.EditorState.CameraX);
        Assert.Equal(original.EditorState.CameraY, deserialized.EditorState.CameraY);
        Assert.Equal(original.EditorState.ZoomIndex, deserialized.EditorState.ZoomIndex);
    }

    [Fact]
    public void JsonRoundtrip_PreservesEntityProperties()
    {
        var original = CreateSampleProjectData();

        string json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProjectFile.ProjectData>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Entities[0].Properties);
        Assert.Equal("100", deserialized.Entities[0].Properties["health"]);
        Assert.Equal("Hero", deserialized.Entities[0].Properties["name"]);
    }

    [Fact]
    public void JsonRoundtrip_PreservesSpriteRefs()
    {
        var original = CreateSampleProjectData();

        string json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProjectFile.ProjectData>(json, JsonOptions);

        Assert.NotNull(deserialized);
        var grassSprites = deserialized.Groups[0].Sprites;
        Assert.Equal(2, grassSprites.Count);
        Assert.Equal(0, grassSprites[0].Col);
        Assert.Equal(0, grassSprites[0].Row);
        Assert.Equal(1, grassSprites[1].Col);
        Assert.Equal(0, grassSprites[1].Row);
    }

    [Fact]
    public void JsonRoundtrip_PreservesLayerCells()
    {
        var original = CreateSampleProjectData();

        string json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProjectFile.ProjectData>(json, JsonOptions);

        Assert.NotNull(deserialized);
        var groundCells = deserialized.Map.Layers[0].Cells;
        Assert.Equal("grass", groundCells[0]); // (0,0)
        Assert.Equal("grass", groundCells[1 + 1 * 10]); // (1,1)
        Assert.Null(groundCells[2]); // unset cell
    }

    [Fact]
    public void JsonRoundtrip_PreservesLayerVisibility()
    {
        var original = CreateSampleProjectData();

        string json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProjectFile.ProjectData>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Map.Layers[0].Visible);
        Assert.False(deserialized.Map.Layers[1].Visible);
    }

    // --- Save and Load roundtrip using temp files ---

    [Fact]
    public void SaveLoad_Roundtrip_PreservesProjectData()
    {
        // This test exercises ProjectFile.Load (which reads from disk and resolves paths).
        // We cannot call ProjectFile.Save because it requires a DojoUI.SpriteSheet instance.
        // Instead, we serialize directly and then use Load to deserialize.

        var original = CreateSampleProjectData();

        // Create a fake spritesheet file so the path resolution doesn't fail
        string spritePath = Path.Combine(_tempDir, "sprites", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(spritePath)!);
        File.WriteAllText(spritePath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        string json = JsonSerializer.Serialize(original, JsonOptions);
        File.WriteAllText(projectPath, json);

        var loaded = ProjectFile.Load(projectPath);

        Assert.NotNull(loaded);
        Assert.Equal(original.Version, loaded.Version);
        Assert.Equal(original.Map.Width, loaded.Map.Width);
        Assert.Equal(original.Map.Height, loaded.Map.Height);
        Assert.Equal(original.Groups.Count, loaded.Groups.Count);
        Assert.Equal(original.Entities.Count, loaded.Entities.Count);
    }

    [Fact]
    public void Load_ResolvesSpritesheetPathRelativeToProjectFile()
    {
        var data = CreateSampleProjectData();

        // Create a fake spritesheet file
        string spritePath = Path.Combine(_tempDir, "sprites", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(spritePath)!);
        File.WriteAllText(spritePath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        string json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(projectPath, json);

        var loaded = ProjectFile.Load(projectPath);

        // The spritesheet path should now be an absolute path
        Assert.True(Path.IsPathRooted(loaded.Spritesheet.Path));
        // It should resolve to the correct location
        string expectedPath = Path.GetFullPath(Path.Combine(_tempDir, "sprites", "sheet.png"));
        Assert.Equal(expectedPath, loaded.Spritesheet.Path);
    }

    // --- RestoreGroups ---

    [Fact]
    public void RestoreGroups_MapsAllFields()
    {
        var data = CreateSampleProjectData();

        var groups = ProjectFile.RestoreGroups(data);

        Assert.Equal(3, groups.Count);

        // grass
        var grass = groups[0];
        Assert.Equal("grass", grass.Name);
        Assert.Equal(GroupType.Tile, grass.Type);
        Assert.False(grass.IsSolid);
        Assert.False(grass.IsPlayer);
        Assert.Equal("Ground", grass.LayerName);
        Assert.Equal(2, grass.Sprites.Count);
        Assert.Equal(0, grass.Sprites[0].Col);
        Assert.Equal(0, grass.Sprites[0].Row);
        Assert.Equal(1, grass.Sprites[1].Col);
        Assert.Equal(0, grass.Sprites[1].Row);

        // wall
        var wall = groups[1];
        Assert.Equal("wall", wall.Name);
        Assert.Equal(GroupType.Tile, wall.Type);
        Assert.True(wall.IsSolid);
        Assert.False(wall.IsPlayer);
        Assert.Equal("Objects", wall.LayerName);
        Assert.Single(wall.Sprites);

        // player
        var player = groups[2];
        Assert.Equal("player", player.Name);
        Assert.Equal(GroupType.Entity, player.Type);
        Assert.False(player.IsSolid);
        Assert.True(player.IsPlayer);
    }

    [Fact]
    public void RestoreGroups_InvalidType_DefaultsToTile()
    {
        var data = new ProjectFile.ProjectData
        {
            Groups = new List<ProjectFile.GroupData>
            {
                new()
                {
                    Name = "unknown",
                    Type = "InvalidType",
                    Sprites = new List<ProjectFile.SpriteRefData>(),
                },
            },
        };

        var groups = ProjectFile.RestoreGroups(data);

        Assert.Equal(GroupType.Tile, groups[0].Type);
    }

    [Fact]
    public void RestoreGroups_NullBooleans_DefaultToFalse()
    {
        var data = new ProjectFile.ProjectData
        {
            Groups = new List<ProjectFile.GroupData>
            {
                new()
                {
                    Name = "test",
                    Type = "Tile",
                    IsSolid = null,
                    IsPlayer = null,
                    Sprites = new List<ProjectFile.SpriteRefData>(),
                },
            },
        };

        var groups = ProjectFile.RestoreGroups(data);

        Assert.False(groups[0].IsSolid);
        Assert.False(groups[0].IsPlayer);
    }

    [Fact]
    public void RestoreGroups_EmptyGroups_ReturnsEmptyList()
    {
        var data = new ProjectFile.ProjectData
        {
            Groups = new List<ProjectFile.GroupData>(),
        };

        var groups = ProjectFile.RestoreGroups(data);

        Assert.Empty(groups);
    }

    // --- RestoreMap ---

    [Fact]
    public void RestoreMap_PreservesWidthAndHeight()
    {
        var data = CreateSampleProjectData();

        var map = ProjectFile.RestoreMap(data);

        Assert.Equal(10, map.Width);
        Assert.Equal(8, map.Height);
    }

    [Fact]
    public void RestoreMap_PreservesEntityRenderOrder()
    {
        var data = CreateSampleProjectData();

        var map = ProjectFile.RestoreMap(data);

        Assert.Equal(1, map.EntityRenderOrder);
    }

    [Fact]
    public void RestoreMap_PreservesLayers()
    {
        var data = CreateSampleProjectData();

        var map = ProjectFile.RestoreMap(data);

        Assert.Equal(2, map.Layers.Count);
        Assert.Equal("Ground", map.Layers[0].Name);
        Assert.Equal("Objects", map.Layers[1].Name);
    }

    [Fact]
    public void RestoreMap_PreservesLayerVisibility()
    {
        var data = CreateSampleProjectData();

        var map = ProjectFile.RestoreMap(data);

        Assert.True(map.Layers[0].Visible);
        Assert.False(map.Layers[1].Visible);
    }

    [Fact]
    public void RestoreMap_PreservesCellData()
    {
        var data = CreateSampleProjectData();

        var map = ProjectFile.RestoreMap(data);

        var ground = map.GetLayer("Ground");
        Assert.Equal("grass", ground.GetCell(0, 0, map.Width));
        Assert.Equal("grass", ground.GetCell(1, 1, map.Width));
        Assert.Null(ground.GetCell(2, 2, map.Width));

        var objects = map.GetLayer("Objects");
        Assert.Equal("wall", objects.GetCell(5, 3, map.Width));
    }

    [Fact]
    public void RestoreMap_PreservesEntities()
    {
        var data = CreateSampleProjectData();

        var map = ProjectFile.RestoreMap(data);

        Assert.Single(map.Entities);
        var entity = map.Entities[0];
        Assert.Equal("abc12345", entity.Id);
        Assert.Equal("player", entity.GroupName);
        Assert.Equal(2, entity.X);
        Assert.Equal(3, entity.Y);
    }

    [Fact]
    public void RestoreMap_PreservesEntityProperties()
    {
        var data = CreateSampleProjectData();

        var map = ProjectFile.RestoreMap(data);

        var entity = map.Entities[0];
        Assert.Equal(2, entity.Properties.Count);
        Assert.Equal("100", entity.Properties["health"]);
        Assert.Equal("Hero", entity.Properties["name"]);
    }

    [Fact]
    public void RestoreMap_NullEntityProperties_DefaultsToEmptyDictionary()
    {
        var data = new ProjectFile.ProjectData
        {
            Map = new ProjectFile.MapInfo
            {
                Width = 5,
                Height = 5,
                Layers = new List<ProjectFile.LayerData>(),
            },
            Entities = new List<ProjectFile.EntityData>
            {
                new()
                {
                    Id = "test1234",
                    GroupName = "npc",
                    X = 1,
                    Y = 2,
                    Properties = null,
                },
            },
        };

        var map = ProjectFile.RestoreMap(data);

        Assert.NotNull(map.Entities[0].Properties);
        Assert.Empty(map.Entities[0].Properties);
    }

    [Fact]
    public void RestoreMap_ClearsDefaultLayers_UsesDataLayers()
    {
        // MapData constructor creates Ground and Objects by default.
        // RestoreMap should clear those and use only the layers from the data.
        var data = new ProjectFile.ProjectData
        {
            Map = new ProjectFile.MapInfo
            {
                Width = 5,
                Height = 5,
                Layers = new List<ProjectFile.LayerData>
                {
                    new()
                    {
                        Name = "CustomLayer",
                        Visible = true,
                        Cells = new string[25],
                    },
                },
            },
            Entities = new List<ProjectFile.EntityData>(),
        };

        var map = ProjectFile.RestoreMap(data);

        Assert.Single(map.Layers);
        Assert.Equal("CustomLayer", map.Layers[0].Name);
    }

    [Fact]
    public void RestoreMap_CellArraySizeMismatch_DoesNotCopy()
    {
        // If the saved cells array has a different length than expected,
        // the cells should remain empty (not copied).
        var data = new ProjectFile.ProjectData
        {
            Map = new ProjectFile.MapInfo
            {
                Width = 5,
                Height = 5,
                Layers = new List<ProjectFile.LayerData>
                {
                    new()
                    {
                        Name = "Ground",
                        Visible = true,
                        Cells = new string[10], // Wrong size: should be 25
                    },
                },
            },
            Entities = new List<ProjectFile.EntityData>(),
        };

        var map = ProjectFile.RestoreMap(data);

        // Since the cells array size doesn't match, copy should be skipped
        var layer = map.GetLayer("Ground");
        Assert.Equal(25, layer.Cells.Length); // Layer created with correct size
        // All cells should be null (default)
        Assert.All(layer.Cells, c => Assert.Null(c));
    }

    [Fact]
    public void RestoreMap_NullCellsArray_HandledGracefully()
    {
        var data = new ProjectFile.ProjectData
        {
            Map = new ProjectFile.MapInfo
            {
                Width = 5,
                Height = 5,
                Layers = new List<ProjectFile.LayerData>
                {
                    new()
                    {
                        Name = "Ground",
                        Visible = true,
                        Cells = null,
                    },
                },
            },
            Entities = new List<ProjectFile.EntityData>(),
        };

        var map = ProjectFile.RestoreMap(data);

        var layer = map.GetLayer("Ground");
        Assert.Equal(25, layer.Cells.Length);
        Assert.All(layer.Cells, c => Assert.Null(c));
    }

    // --- EditorStateData roundtrip ---

    [Fact]
    public void JsonRoundtrip_PreservesEditorState()
    {
        var original = CreateSampleProjectData();

        string json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProjectFile.ProjectData>(json, JsonOptions);

        Assert.NotNull(deserialized?.EditorState);
        Assert.Equal("Ground", deserialized.EditorState.ActiveLayer);
        Assert.Equal(100f, deserialized.EditorState.CameraX);
        Assert.Equal(200f, deserialized.EditorState.CameraY);
        Assert.Equal(3, deserialized.EditorState.ZoomIndex);
        Assert.Equal(new List<string> { "Tools", "Map" }, deserialized.EditorState.PanelOrder);
        Assert.Equal(new List<string> { "Tools" }, deserialized.EditorState.CollapsedPanels);
        Assert.Empty(deserialized.EditorState.CollapsedLayers);
    }

    // =========================================================================
    // ProjectFile.Save() + Load() roundtrip tests using MockSpriteSheet
    //
    // With ISpriteSheet extracted, we can now call ProjectFile.Save() directly
    // by injecting a MockSpriteSheet instead of requiring a real GraphicsDevice.
    // =========================================================================

    /// <summary>
    /// Creates domain objects (groups, map, entities) matching the sample ProjectData,
    /// suitable for passing to ProjectFile.Save().
    /// </summary>
    private static (List<TileGroup> groups, MapData map) CreateSampleDomainObjects()
    {
        var groups = new List<TileGroup>
        {
            new TileGroup
            {
                Name = "grass",
                Type = GroupType.Tile,
                IsSolid = false,
                IsPlayer = false,
                LayerName = "Ground",
                Sprites = { new SpriteRef { Col = 0, Row = 0 }, new SpriteRef { Col = 1, Row = 0 } },
            },
            new TileGroup
            {
                Name = "wall",
                Type = GroupType.Tile,
                IsSolid = true,
                IsPlayer = false,
                LayerName = "Objects",
                Sprites = { new SpriteRef { Col = 2, Row = 3 } },
            },
            new TileGroup
            {
                Name = "player",
                Type = GroupType.Entity,
                IsSolid = false,
                IsPlayer = true,
                LayerName = "Objects",
                Sprites = { new SpriteRef { Col = 4, Row = 5 } },
            },
        };

        var map = new MapData(10, 8);
        map.EntityRenderOrder = 1;

        // Clear default layers and set up custom ones
        map.Layers.Clear();
        var ground = new MapLayer("Ground", 10, 8) { Visible = true };
        ground.SetCell(0, 0, 10, "grass");
        ground.SetCell(1, 1, 10, "grass");
        map.Layers.Add(ground);

        var objects = new MapLayer("Objects", 10, 8) { Visible = false };
        objects.SetCell(5, 3, 10, "wall");
        map.Layers.Add(objects);

        // Entities
        map.Entities.Add(new Entity
        {
            Id = "abc12345",
            GroupName = "player",
            X = 2,
            Y = 3,
            Properties = new Dictionary<string, string>
            {
                { "health", "100" },
                { "name", "Hero" },
            },
        });

        return (groups, map);
    }

    [Fact]
    public void Save_WritesValidJsonThatLoadCanParse()
    {
        var sheet = new MockSpriteSheet(16, 16, padding: 1);
        var (groups, map) = CreateSampleDomainObjects();
        var editorState = new ProjectFile.EditorStateData
        {
            ActiveLayer = "Ground",
            CameraX = 100f,
            CameraY = 200f,
            ZoomIndex = 3,
        };

        string sheetPath = Path.Combine(_tempDir, "sprites", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(sheetPath)!);
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");

        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        // File should exist and be parseable by Load
        Assert.True(File.Exists(projectPath));
        var loaded = ProjectFile.Load(projectPath);
        Assert.NotNull(loaded);
        Assert.Equal(1, loaded.Version);
    }

    [Fact]
    public void SaveLoad_Roundtrip_PreservesSpritesheetInfo()
    {
        var sheet = new MockSpriteSheet(16, 16, padding: 1);
        var (groups, map) = CreateSampleDomainObjects();
        var editorState = new ProjectFile.EditorStateData();

        string sheetPath = Path.Combine(_tempDir, "sprites", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(sheetPath)!);
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        var loaded = ProjectFile.Load(projectPath);

        Assert.Equal(16, loaded.Spritesheet.TileWidth);
        Assert.Equal(16, loaded.Spritesheet.TileHeight);
        Assert.Equal(1, loaded.Spritesheet.Padding);
    }

    [Fact]
    public void SaveLoad_Roundtrip_UsesRelativeSpritesheetPath()
    {
        var sheet = new MockSpriteSheet(16, 16);
        var (groups, map) = CreateSampleDomainObjects();
        var editorState = new ProjectFile.EditorStateData();

        string sheetPath = Path.Combine(_tempDir, "sprites", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(sheetPath)!);
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        // Read the raw JSON to verify the path is stored as relative
        string rawJson = File.ReadAllText(projectPath);
        var rawData = JsonSerializer.Deserialize<ProjectFile.ProjectData>(rawJson, JsonOptions);
        Assert.Equal(Path.Combine("sprites", "sheet.png"), rawData.Spritesheet.Path);

        // After Load, it should be resolved to absolute
        var loaded = ProjectFile.Load(projectPath);
        Assert.True(Path.IsPathRooted(loaded.Spritesheet.Path));
        Assert.Equal(Path.GetFullPath(sheetPath), loaded.Spritesheet.Path);
    }

    [Fact]
    public void SaveLoad_Roundtrip_PreservesGroups()
    {
        var sheet = new MockSpriteSheet(16, 16, padding: 1);
        var (groups, map) = CreateSampleDomainObjects();
        var editorState = new ProjectFile.EditorStateData();

        string sheetPath = Path.Combine(_tempDir, "sprites", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(sheetPath)!);
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        var loaded = ProjectFile.Load(projectPath);
        var restoredGroups = ProjectFile.RestoreGroups(loaded);

        Assert.Equal(3, restoredGroups.Count);

        // grass
        Assert.Equal("grass", restoredGroups[0].Name);
        Assert.Equal(GroupType.Tile, restoredGroups[0].Type);
        Assert.False(restoredGroups[0].IsSolid);
        Assert.False(restoredGroups[0].IsPlayer);
        Assert.Equal("Ground", restoredGroups[0].LayerName);
        Assert.Equal(2, restoredGroups[0].Sprites.Count);
        Assert.Equal(0, restoredGroups[0].Sprites[0].Col);
        Assert.Equal(0, restoredGroups[0].Sprites[0].Row);
        Assert.Equal(1, restoredGroups[0].Sprites[1].Col);
        Assert.Equal(0, restoredGroups[0].Sprites[1].Row);

        // wall
        Assert.Equal("wall", restoredGroups[1].Name);
        Assert.True(restoredGroups[1].IsSolid);
        Assert.Single(restoredGroups[1].Sprites);

        // player
        Assert.Equal("player", restoredGroups[2].Name);
        Assert.Equal(GroupType.Entity, restoredGroups[2].Type);
        Assert.True(restoredGroups[2].IsPlayer);
    }

    [Fact]
    public void SaveLoad_Roundtrip_PreservesMapAndLayers()
    {
        var sheet = new MockSpriteSheet(16, 16);
        var (groups, map) = CreateSampleDomainObjects();
        var editorState = new ProjectFile.EditorStateData();

        string sheetPath = Path.Combine(_tempDir, "sprites", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(sheetPath)!);
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        var loaded = ProjectFile.Load(projectPath);
        var restoredMap = ProjectFile.RestoreMap(loaded);

        Assert.Equal(10, restoredMap.Width);
        Assert.Equal(8, restoredMap.Height);
        Assert.Equal(1, restoredMap.EntityRenderOrder);

        Assert.Equal(2, restoredMap.Layers.Count);
        Assert.Equal("Ground", restoredMap.Layers[0].Name);
        Assert.Equal("Objects", restoredMap.Layers[1].Name);

        // Ground cells
        var ground = restoredMap.GetLayer("Ground");
        Assert.Equal("grass", ground.GetCell(0, 0, restoredMap.Width));
        Assert.Equal("grass", ground.GetCell(1, 1, restoredMap.Width));
        Assert.Null(ground.GetCell(5, 5, restoredMap.Width));

        // Objects cells
        var objects = restoredMap.GetLayer("Objects");
        Assert.Equal("wall", objects.GetCell(5, 3, restoredMap.Width));
    }

    [Fact]
    public void SaveLoad_Roundtrip_PreservesLayerVisibility()
    {
        var sheet = new MockSpriteSheet(16, 16);
        var (groups, map) = CreateSampleDomainObjects();
        var editorState = new ProjectFile.EditorStateData();

        string sheetPath = Path.Combine(_tempDir, "sprites", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(sheetPath)!);
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        var loaded = ProjectFile.Load(projectPath);
        var restoredMap = ProjectFile.RestoreMap(loaded);

        Assert.True(restoredMap.Layers[0].Visible);   // Ground
        Assert.False(restoredMap.Layers[1].Visible);  // Objects
    }

    [Fact]
    public void SaveLoad_Roundtrip_PreservesEntities()
    {
        var sheet = new MockSpriteSheet(16, 16);
        var (groups, map) = CreateSampleDomainObjects();
        var editorState = new ProjectFile.EditorStateData();

        string sheetPath = Path.Combine(_tempDir, "sprites", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(sheetPath)!);
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        var loaded = ProjectFile.Load(projectPath);
        var restoredMap = ProjectFile.RestoreMap(loaded);

        Assert.Single(restoredMap.Entities);
        var entity = restoredMap.Entities[0];
        Assert.Equal("abc12345", entity.Id);
        Assert.Equal("player", entity.GroupName);
        Assert.Equal(2, entity.X);
        Assert.Equal(3, entity.Y);
    }

    [Fact]
    public void SaveLoad_Roundtrip_PreservesEntityProperties()
    {
        var sheet = new MockSpriteSheet(16, 16);
        var (groups, map) = CreateSampleDomainObjects();
        var editorState = new ProjectFile.EditorStateData();

        string sheetPath = Path.Combine(_tempDir, "sprites", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(sheetPath)!);
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        var loaded = ProjectFile.Load(projectPath);
        var restoredMap = ProjectFile.RestoreMap(loaded);

        var entity = restoredMap.Entities[0];
        Assert.Equal(2, entity.Properties.Count);
        Assert.Equal("100", entity.Properties["health"]);
        Assert.Equal("Hero", entity.Properties["name"]);
    }

    [Fact]
    public void SaveLoad_Roundtrip_PreservesEditorState()
    {
        var sheet = new MockSpriteSheet(16, 16);
        var (groups, map) = CreateSampleDomainObjects();
        var editorState = new ProjectFile.EditorStateData
        {
            ActiveLayer = "Objects",
            CameraX = 300f,
            CameraY = 400f,
            ZoomIndex = 5,
            PanelOrder = new List<string> { "Map", "Tools" },
            CollapsedPanels = new List<string> { "Map" },
            CollapsedLayers = new List<string> { "Ground" },
        };

        string sheetPath = Path.Combine(_tempDir, "sprites", "sheet.png");
        Directory.CreateDirectory(Path.GetDirectoryName(sheetPath)!);
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        var loaded = ProjectFile.Load(projectPath);

        Assert.NotNull(loaded.EditorState);
        Assert.Equal("Objects", loaded.EditorState.ActiveLayer);
        Assert.Equal(300f, loaded.EditorState.CameraX);
        Assert.Equal(400f, loaded.EditorState.CameraY);
        Assert.Equal(5, loaded.EditorState.ZoomIndex);
        Assert.Equal(new List<string> { "Map", "Tools" }, loaded.EditorState.PanelOrder);
        Assert.Equal(new List<string> { "Map" }, loaded.EditorState.CollapsedPanels);
        Assert.Equal(new List<string> { "Ground" }, loaded.EditorState.CollapsedLayers);
    }

    [Fact]
    public void SaveLoad_Roundtrip_EmptyMap_PreservesStructure()
    {
        var sheet = new MockSpriteSheet(32, 32, padding: 2);
        var groups = new List<TileGroup>();
        var map = new MapData(5, 5);
        map.Layers.Clear();
        map.Layers.Add(new MapLayer("Default", 5, 5));
        var editorState = new ProjectFile.EditorStateData();

        string sheetPath = Path.Combine(_tempDir, "empty_sheet.png");
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "empty.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        var loaded = ProjectFile.Load(projectPath);

        Assert.Empty(loaded.Groups);
        Assert.Empty(loaded.Entities);
        Assert.Equal(5, loaded.Map.Width);
        Assert.Equal(5, loaded.Map.Height);
        Assert.Single(loaded.Map.Layers);
        Assert.Equal("Default", loaded.Map.Layers[0].Name);
        Assert.Equal(32, loaded.Spritesheet.TileWidth);
        Assert.Equal(32, loaded.Spritesheet.TileHeight);
        Assert.Equal(2, loaded.Spritesheet.Padding);
    }

    [Fact]
    public void SaveLoad_Roundtrip_EntityWithEmptyProperties_OmitsProperties()
    {
        var sheet = new MockSpriteSheet(16, 16);
        var groups = new List<TileGroup>
        {
            new TileGroup
            {
                Name = "npc",
                Type = GroupType.Entity,
                Sprites = { new SpriteRef { Col = 0, Row = 0 } },
            },
        };
        var map = new MapData(5, 5);
        map.Entities.Add(new Entity
        {
            Id = "npc01",
            GroupName = "npc",
            X = 1,
            Y = 2,
            Properties = new Dictionary<string, string>(), // empty
        });
        var editorState = new ProjectFile.EditorStateData();

        string sheetPath = Path.Combine(_tempDir, "sheet.png");
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        var loaded = ProjectFile.Load(projectPath);
        var restoredMap = ProjectFile.RestoreMap(loaded);

        // Entity with empty properties should restore with empty dictionary
        var entity = restoredMap.Entities[0];
        Assert.NotNull(entity.Properties);
        Assert.Empty(entity.Properties);
    }

    [Fact]
    public void Save_NonSolidGroup_DoesNotWriteIsSolid()
    {
        var sheet = new MockSpriteSheet(16, 16);
        var groups = new List<TileGroup>
        {
            new TileGroup
            {
                Name = "grass",
                Type = GroupType.Tile,
                IsSolid = false,
                IsPlayer = false,
                Sprites = { new SpriteRef { Col = 0, Row = 0 } },
            },
        };
        var map = new MapData(5, 5);
        var editorState = new ProjectFile.EditorStateData();

        string sheetPath = Path.Combine(_tempDir, "sheet.png");
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        // Read raw JSON and verify that IsSolid is not present (WhenWritingNull + only set when true)
        string rawJson = File.ReadAllText(projectPath);
        var rawData = JsonSerializer.Deserialize<ProjectFile.ProjectData>(rawJson, JsonOptions);

        // The grass group should not have IsSolid or IsPlayer set (they default to null in GroupData)
        // since the source values are false and Save only writes true values
        Assert.Null(rawData.Groups[0].IsSolid);
        Assert.Null(rawData.Groups[0].IsPlayer);
    }

    [Fact]
    public void Save_SolidPlayerGroup_WritesFlags()
    {
        var sheet = new MockSpriteSheet(16, 16);
        var groups = new List<TileGroup>
        {
            new TileGroup
            {
                Name = "hero",
                Type = GroupType.Entity,
                IsSolid = true,
                IsPlayer = true,
                Sprites = { new SpriteRef { Col = 0, Row = 0 } },
            },
        };
        var map = new MapData(5, 5);
        var editorState = new ProjectFile.EditorStateData();

        string sheetPath = Path.Combine(_tempDir, "sheet.png");
        File.WriteAllText(sheetPath, "fake png");

        string projectPath = Path.Combine(_tempDir, "test.tileforge");
        ProjectFile.Save(projectPath, sheetPath, sheet, groups, map, editorState);

        string rawJson = File.ReadAllText(projectPath);
        var rawData = JsonSerializer.Deserialize<ProjectFile.ProjectData>(rawJson, JsonOptions);

        Assert.True(rawData.Groups[0].IsSolid);
        Assert.True(rawData.Groups[0].IsPlayer);
    }

    // --- G1 gameplay properties tests ---

    [Fact]
    public void GroupData_GameplayProperties_DefaultsToNull()
    {
        var gd = new ProjectFile.GroupData();
        Assert.Null(gd.IsPassable);
        Assert.Null(gd.IsHazardous);
        Assert.Null(gd.MovementCost);
        Assert.Null(gd.DamageType);
        Assert.Null(gd.DamagePerTick);
        Assert.Null(gd.EntityType);
    }

    [Fact]
    public void RestoreGroups_MissingGameplayFields_DefaultsToSafeValues()
    {
        // Simulates loading an old .tileforge file without gameplay fields
        var data = new ProjectFile.ProjectData
        {
            Groups = new()
            {
                new ProjectFile.GroupData
                {
                    Name = "grass",
                    Type = "Tile",
                    // No gameplay properties set — all null
                }
            }
        };

        var groups = ProjectFile.RestoreGroups(data);
        var grass = groups[0];

        Assert.True(grass.IsPassable);
        Assert.False(grass.IsHazardous);
        Assert.Equal(1.0f, grass.MovementCost);
        Assert.Null(grass.DamageType);
        Assert.Equal(0, grass.DamagePerTick);
        Assert.Equal(EntityType.Interactable, grass.EntityType);
    }

    [Fact]
    public void RestoreGroups_WithGameplayFields_RestoresCorrectly()
    {
        var data = new ProjectFile.ProjectData
        {
            Groups = new()
            {
                new ProjectFile.GroupData
                {
                    Name = "lava",
                    Type = "Tile",
                    IsSolid = true,
                    IsPassable = false,
                    IsHazardous = true,
                    MovementCost = 2.0f,
                    DamageType = "fire",
                    DamagePerTick = 5,
                }
            }
        };

        var groups = ProjectFile.RestoreGroups(data);
        var lava = groups[0];

        Assert.True(lava.IsSolid);
        Assert.False(lava.IsPassable);
        Assert.True(lava.IsHazardous);
        Assert.Equal(2.0f, lava.MovementCost);
        Assert.Equal("fire", lava.DamageType);
        Assert.Equal(5, lava.DamagePerTick);
    }

    [Fact]
    public void RestoreGroups_EntityTypeField_RestoresCorrectly()
    {
        var data = new ProjectFile.ProjectData
        {
            Groups = new()
            {
                new ProjectFile.GroupData { Name = "npc", Type = "Entity", EntityType = "NPC" },
                new ProjectFile.GroupData { Name = "door", Type = "Entity", EntityType = "Trigger" },
                new ProjectFile.GroupData { Name = "chest", Type = "Entity" },  // missing = Interactable
            }
        };

        var groups = ProjectFile.RestoreGroups(data);

        Assert.Equal(EntityType.NPC, groups[0].EntityType);
        Assert.Equal(EntityType.Trigger, groups[1].EntityType);
        Assert.Equal(EntityType.Interactable, groups[2].EntityType);
    }

    [Fact]
    public void JsonRoundtrip_GameplayProperties_Preserved()
    {
        var original = new ProjectFile.GroupData
        {
            Name = "lava",
            Type = "Tile",
            IsSolid = true,
            IsPassable = false,
            IsHazardous = true,
            MovementCost = 2.5f,
            DamageType = "fire",
            DamagePerTick = 10,
        };

        var projectData = new ProjectFile.ProjectData
        {
            Groups = new() { original }
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        string json = System.Text.Json.JsonSerializer.Serialize(projectData, options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<ProjectFile.ProjectData>(json, options);

        var g = deserialized.Groups[0];
        Assert.False(g.IsPassable);
        Assert.True(g.IsHazardous);
        Assert.Equal(2.5f, g.MovementCost);
        Assert.Equal("fire", g.DamageType);
        Assert.Equal(10, g.DamagePerTick);
    }

    [Fact]
    public void JsonRoundtrip_DefaultGameplayProperties_OmittedFromJson()
    {
        var projectData = new ProjectFile.ProjectData
        {
            Groups = new()
            {
                new ProjectFile.GroupData { Name = "grass", Type = "Tile" }
            }
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        string json = System.Text.Json.JsonSerializer.Serialize(projectData, options);

        Assert.DoesNotContain("\"isPassable\"", json);
        Assert.DoesNotContain("\"isHazardous\"", json);
        Assert.DoesNotContain("\"movementCost\"", json);
        Assert.DoesNotContain("\"damageType\"", json);
        Assert.DoesNotContain("\"damagePerTick\"", json);
        Assert.DoesNotContain("\"entityType\"", json);
    }
}
