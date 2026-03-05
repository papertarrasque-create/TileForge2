using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using TileForge.Data;
using TileForge.Export;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class MapLoaderTests
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static string BuildExportJson(
        int width = 10, int height = 10,
        List<ExportGroup> groups = null,
        List<ExportLayer> layers = null,
        List<ExportEntity> entities = null)
    {
        var data = new ExportData
        {
            Width = width,
            Height = height,
            Layers = layers ?? new List<ExportLayer>
            {
                new() { Name = "Ground", Cells = new string[width * height] }
            },
            Groups = groups ?? new List<ExportGroup>
            {
                new() { Name = "grass", Type = "Tile" },
                new() { Name = "wall", Type = "Tile", IsSolid = true, IsPassable = false },
                new() { Name = "npc", Type = "Entity", EntityType = "NPC" },
            },
            Entities = entities ?? new List<ExportEntity>
            {
                new() { Id = "e1", GroupName = "npc", X = 5, Y = 3,
                    Properties = new Dictionary<string, string> { ["dialogue"] = "hello" } },
            },
        };
        return JsonSerializer.Serialize(data, ExportJsonOptions);
    }

    // ========== Dimension parsing ==========

    [Fact]
    public void Load_ParsesDimensions()
    {
        var json = BuildExportJson(20, 15);
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Equal(20, map.Width);
        Assert.Equal(15, map.Height);
    }

    [Fact]
    public void Load_SetsMapId()
    {
        var json = BuildExportJson();
        var loader = new MapLoader();

        var map = loader.Load(json, "dungeon_01");

        Assert.Equal("dungeon_01", map.Id);
    }

    [Fact]
    public void Load_MapIdDefaultsToNull()
    {
        var json = BuildExportJson();
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Null(map.Id);
    }

    // ========== Layer parsing ==========

    [Fact]
    public void Load_ParsesLayers()
    {
        var json = BuildExportJson();
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Single(map.Layers);
        Assert.Equal("Ground", map.Layers[0].Name);
    }

    [Fact]
    public void Load_MultipleLayers()
    {
        var layers = new List<ExportLayer>
        {
            new() { Name = "Ground", Cells = new string[100] },
            new() { Name = "Objects", Cells = new string[100] },
        };
        var json = BuildExportJson(layers: layers);
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Equal(2, map.Layers.Count);
        Assert.Equal("Ground", map.Layers[0].Name);
        Assert.Equal("Objects", map.Layers[1].Name);
    }

    [Fact]
    public void Load_LayerCellsPreserved()
    {
        var cells = new string[100];
        cells[0] = "grass";
        cells[5] = "wall";
        var layers = new List<ExportLayer>
        {
            new() { Name = "Ground", Cells = cells },
        };
        var json = BuildExportJson(layers: layers);
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Equal("grass", map.Layers[0].GetCell(0, 0, 10));
        Assert.Equal("wall", map.Layers[0].GetCell(5, 0, 10));
        Assert.Null(map.Layers[0].GetCell(1, 0, 10));
    }

    // ========== Group parsing ==========

    [Fact]
    public void Load_ParsesGroups()
    {
        var json = BuildExportJson();
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Equal(3, map.Groups.Count);
        Assert.Equal("grass", map.Groups[0].Name);
        Assert.Equal("wall", map.Groups[1].Name);
        Assert.Equal("npc", map.Groups[2].Name);
    }

    [Fact]
    public void Load_TileGroupType()
    {
        var json = BuildExportJson();
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Equal(GroupType.Tile, map.Groups[0].Type);
        Assert.Equal(GroupType.Entity, map.Groups[2].Type);
    }

    [Fact]
    public void Load_GroupSolidProperty()
    {
        var json = BuildExportJson();
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.False(map.Groups[0].IsSolid); // grass
        Assert.True(map.Groups[1].IsSolid);  // wall
    }

    [Fact]
    public void Load_GroupPassableProperty()
    {
        var json = BuildExportJson();
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.True(map.Groups[0].IsPassable);  // grass (default)
        Assert.False(map.Groups[1].IsPassable); // wall
    }

    [Fact]
    public void Load_GroupGameplayProperties()
    {
        var groups = new List<ExportGroup>
        {
            new()
            {
                Name = "lava", Type = "Tile",
                IsHazardous = true, DamagePerTick = 5,
                DamageType = "fire", MovementCost = 2.0f,
            },
        };
        var json = BuildExportJson(groups: groups);
        var loader = new MapLoader();

        var map = loader.Load(json);

        var lava = map.Groups[0];
        Assert.True(lava.IsHazardous);
        Assert.Equal(5, lava.DamagePerTick);
        Assert.Equal("fire", lava.DamageType);
        Assert.Equal(2.0f, lava.MovementCost);
    }

    [Fact]
    public void Load_GroupGameplayDefaults()
    {
        var groups = new List<ExportGroup>
        {
            new() { Name = "grass", Type = "Tile" },
        };
        var json = BuildExportJson(groups: groups);
        var loader = new MapLoader();

        var map = loader.Load(json);

        var grass = map.Groups[0];
        Assert.True(grass.IsPassable);
        Assert.False(grass.IsHazardous);
        Assert.Equal(1.0f, grass.MovementCost);
        Assert.Equal(0, grass.DamagePerTick);
        Assert.Null(grass.DamageType);
    }

    [Fact]
    public void Load_EntityTypeProperty()
    {
        var groups = new List<ExportGroup>
        {
            new() { Name = "npc", Type = "Entity", EntityType = "NPC" },
            new() { Name = "chest", Type = "Entity", EntityType = "Item" },
            new() { Name = "spike", Type = "Entity", EntityType = "Trap" },
            new() { Name = "door", Type = "Entity", EntityType = "Trigger" },
            new() { Name = "sign", Type = "Entity", EntityType = "Interactable" },
        };
        var json = BuildExportJson(groups: groups);
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Equal(EntityType.NPC, map.Groups[0].EntityType);
        Assert.Equal(EntityType.Item, map.Groups[1].EntityType);
        Assert.Equal(EntityType.Trap, map.Groups[2].EntityType);
        Assert.Equal(EntityType.Trigger, map.Groups[3].EntityType);
        Assert.Equal(EntityType.Interactable, map.Groups[4].EntityType);
    }

    [Fact]
    public void Load_EntityTypeDefaultsToInteractable()
    {
        var groups = new List<ExportGroup>
        {
            new() { Name = "thing", Type = "Entity" },
        };
        var json = BuildExportJson(groups: groups);
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Equal(EntityType.Interactable, map.Groups[0].EntityType);
    }

    [Fact]
    public void Load_IsPlayerProperty()
    {
        var groups = new List<ExportGroup>
        {
            new() { Name = "hero", Type = "Entity", IsPlayer = true },
            new() { Name = "npc", Type = "Entity" },
        };
        var json = BuildExportJson(groups: groups);
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.True(map.Groups[0].IsPlayer);
        Assert.False(map.Groups[1].IsPlayer);
    }

    [Fact]
    public void Load_GroupSprites()
    {
        var groups = new List<ExportGroup>
        {
            new()
            {
                Name = "grass", Type = "Tile",
                Sprites = new List<ExportSpriteRef>
                {
                    new() { Col = 0, Row = 1 },
                    new() { Col = 2, Row = 3 },
                },
            },
        };
        var json = BuildExportJson(groups: groups);
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Equal(2, map.Groups[0].Sprites.Count);
        Assert.Equal(0, map.Groups[0].Sprites[0].Col);
        Assert.Equal(1, map.Groups[0].Sprites[0].Row);
        Assert.Equal(2, map.Groups[0].Sprites[1].Col);
        Assert.Equal(3, map.Groups[0].Sprites[1].Row);
    }

    // ========== Entity parsing ==========

    [Fact]
    public void Load_ParsesEntities()
    {
        var json = BuildExportJson();
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Single(map.Entities);
        var entity = map.Entities[0];
        Assert.Equal("e1", entity.Id);
        Assert.Equal("npc", entity.DefinitionName);
        Assert.Equal(5, entity.X);
        Assert.Equal(3, entity.Y);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Load_EntityProperties()
    {
        var json = BuildExportJson();
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Equal("hello", map.Entities[0].Properties["dialogue"]);
    }

    [Fact]
    public void Load_EntityPropertiesCopied()
    {
        var entities = new List<ExportEntity>
        {
            new() { Id = "e1", GroupName = "npc", X = 0, Y = 0,
                Properties = new Dictionary<string, string> { ["key"] = "value" } },
        };
        var json = BuildExportJson(entities: entities);
        var loader = new MapLoader();

        var map = loader.Load(json);

        // Modifying the loaded entity's properties shouldn't affect a second load
        map.Entities[0].Properties["key"] = "modified";
        var map2 = loader.Load(json);
        Assert.Equal("value", map2.Entities[0].Properties["key"]);
    }

    [Fact]
    public void Load_EntityWithEmptyProperties()
    {
        var entities = new List<ExportEntity>
        {
            new() { Id = "e1", GroupName = "npc", X = 0, Y = 0 },
        };
        var json = BuildExportJson(entities: entities);
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.NotNull(map.Entities[0].Properties);
        Assert.Empty(map.Entities[0].Properties);
    }

    [Fact]
    public void Load_MultipleEntities()
    {
        var entities = new List<ExportEntity>
        {
            new() { Id = "e1", GroupName = "npc", X = 1, Y = 2 },
            new() { Id = "e2", GroupName = "npc", X = 3, Y = 4 },
            new() { Id = "e3", GroupName = "npc", X = 5, Y = 6 },
        };
        var json = BuildExportJson(entities: entities);
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Equal(3, map.Entities.Count);
        Assert.Equal("e1", map.Entities[0].Id);
        Assert.Equal("e2", map.Entities[1].Id);
        Assert.Equal("e3", map.Entities[2].Id);
    }

    // ========== LoadFromExportData ==========

    [Fact]
    public void LoadFromExportData_WorksDirectly()
    {
        var data = new ExportData
        {
            Width = 5,
            Height = 5,
            Layers = new List<ExportLayer>
            {
                new() { Name = "Ground", Cells = new string[25] },
            },
            Groups = new List<ExportGroup>
            {
                new() { Name = "grass", Type = "Tile" },
            },
            Entities = new List<ExportEntity>(),
        };
        var loader = new MapLoader();

        var map = loader.LoadFromExportData(data, "test_map");

        Assert.Equal("test_map", map.Id);
        Assert.Equal(5, map.Width);
        Assert.Equal(5, map.Height);
        Assert.Single(map.Layers);
        Assert.Single(map.Groups);
        Assert.Empty(map.Entities);
    }

    // ========== Roundtrip: Export → Load ==========

    [Fact]
    public void ExportThenLoad_Roundtrip()
    {
        // Build editor data
        var map = new MapData(8, 8);
        var grassGroup = new TileGroup
        {
            Name = "grass", Type = GroupType.Tile,
            IsPassable = true, MovementCost = 1.0f,
            Sprites = new List<SpriteRef> { new() { Col = 0, Row = 0 } },
        };
        var npcGroup = new TileGroup
        {
            Name = "villager", Type = GroupType.Entity,
            EntityType = EntityType.NPC,
            Sprites = new List<SpriteRef> { new() { Col = 1, Row = 0 } },
        };
        var playerGroup = new TileGroup
        {
            Name = "hero", Type = GroupType.Entity,
            IsPlayer = true,
            Sprites = new List<SpriteRef> { new() { Col = 2, Row = 0 } },
        };
        map.Layers[0].Cells[0] = "grass";
        map.Entities.Add(new Entity { Id = "n1", GroupName = "villager", X = 3, Y = 4,
            Properties = new Dictionary<string, string> { ["dialogue"] = "Welcome!" } });
        map.Entities.Add(new Entity { Id = "p1", GroupName = "hero", X = 1, Y = 1 });

        var groups = new List<TileGroup> { grassGroup, npcGroup, playerGroup };

        // Export
        string json = MapExporter.ExportJson(map, groups);

        // Load
        var loader = new MapLoader();
        var loaded = loader.Load(json, "test");

        Assert.Equal(8, loaded.Width);
        Assert.Equal(8, loaded.Height);
        Assert.Equal(3, loaded.Groups.Count);
        Assert.Equal(2, loaded.Entities.Count);

        // Verify grass group
        var grass = loaded.Groups.First(g => g.Name == "grass");
        Assert.Equal(GroupType.Tile, grass.Type);
        Assert.True(grass.IsPassable);

        // Verify NPC
        var npc = loaded.Entities.First(e => e.Id == "n1");
        Assert.Equal("villager", npc.DefinitionName);
        Assert.Equal("Welcome!", npc.Properties["dialogue"]);

        // Verify player group
        var player = loaded.Groups.First(g => g.Name == "hero");
        Assert.True(player.IsPlayer);
    }
}
