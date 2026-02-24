using System.Collections.Generic;
using System.Text.Json;
using TileForge.Data;
using TileForge.Export;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Export;

public class MapExporterTests
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void ExportJson_EmptyMap_ProducesValidJson()
    {
        var map = new MapData(5, 5);
        var groups = new List<TileGroup>();

        string json = MapExporter.ExportJson(map, groups);
        var result = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        Assert.NotNull(result);
        Assert.Equal(5, result.Width);
        Assert.Equal(5, result.Height);
    }

    [Fact]
    public void ExportJson_IncludesAllLayers()
    {
        var map = new MapData(3, 3);
        map.AddLayer("Extra");
        var groups = new List<TileGroup>();

        string json = MapExporter.ExportJson(map, groups);
        var result = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        Assert.Equal(3, result.Layers.Count);
        Assert.Equal("Ground", result.Layers[0].Name);
        Assert.Equal("Objects", result.Layers[1].Name);
        Assert.Equal("Extra", result.Layers[2].Name);
    }

    [Fact]
    public void ExportJson_IncludesGroups()
    {
        var map = new MapData(3, 3);
        var groups = new List<TileGroup>
        {
            new() { Name = "grass", Type = GroupType.Tile, Sprites = new() { new SpriteRef { Col = 0, Row = 0 } } },
            new() { Name = "wall", Type = GroupType.Tile, IsSolid = true, Sprites = new() { new SpriteRef { Col = 1, Row = 0 } } },
        };

        string json = MapExporter.ExportJson(map, groups);
        var result = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        Assert.Equal(2, result.Groups.Count);
        Assert.Equal("grass", result.Groups[0].Name);
        Assert.Equal("Tile", result.Groups[0].Type);
        Assert.Equal("wall", result.Groups[1].Name);
        Assert.True(result.Groups[1].IsSolid);
    }

    [Fact]
    public void ExportJson_IsSolidFalse_OmittedFromOutput()
    {
        var map = new MapData(3, 3);
        var groups = new List<TileGroup>
        {
            new() { Name = "grass", Type = GroupType.Tile, IsSolid = false },
        };

        string json = MapExporter.ExportJson(map, groups);

        // isSolid should be null (omitted) when false
        Assert.DoesNotContain("\"isSolid\"", json);
    }

    [Fact]
    public void ExportJson_IncludesEntities()
    {
        var map = new MapData(5, 5);
        map.Entities.Add(new Entity { GroupName = "door", X = 2, Y = 3 });
        var groups = new List<TileGroup>();

        string json = MapExporter.ExportJson(map, groups);
        var result = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        Assert.Single(result.Entities);
        Assert.Equal("door", result.Entities[0].GroupName);
        Assert.Equal(2, result.Entities[0].X);
        Assert.Equal(3, result.Entities[0].Y);
    }

    [Fact]
    public void ExportJson_EntityWithProperties_Preserved()
    {
        var map = new MapData(5, 5);
        var entity = new Entity { GroupName = "npc", X = 1, Y = 1 };
        entity.Properties["dialogue"] = "Hello!";
        map.Entities.Add(entity);

        string json = MapExporter.ExportJson(map, new List<TileGroup>());
        var result = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        Assert.NotNull(result.Entities[0].Properties);
        Assert.Equal("Hello!", result.Entities[0].Properties["dialogue"]);
    }

    [Fact]
    public void ExportJson_EntityWithEmptyProperties_PropertiesOmitted()
    {
        var map = new MapData(5, 5);
        map.Entities.Add(new Entity { GroupName = "rock", X = 0, Y = 0 });

        string json = MapExporter.ExportJson(map, new List<TileGroup>());

        // Properties should be null (omitted) when empty dict
        Assert.DoesNotContain("\"properties\"", json);
    }

    [Fact]
    public void ExportJson_NoEditorState()
    {
        var map = new MapData(5, 5);
        var groups = new List<TileGroup>();

        string json = MapExporter.ExportJson(map, groups);

        // Ensure no editor-state fields appear
        Assert.DoesNotContain("\"cameraX\"", json);
        Assert.DoesNotContain("\"zoomIndex\"", json);
        Assert.DoesNotContain("\"panelOrder\"", json);
        Assert.DoesNotContain("\"collapsedPanels\"", json);
        Assert.DoesNotContain("\"activeLayer\"", json);
    }

    [Fact]
    public void ExportJson_LayerCellsPreserved()
    {
        var map = new MapData(3, 3);
        map.GetLayer("Ground").SetCell(0, 0, 3, "grass");
        map.GetLayer("Ground").SetCell(1, 1, 3, "wall");
        var groups = new List<TileGroup>();

        string json = MapExporter.ExportJson(map, groups);
        var result = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        var groundCells = result.Layers[0].Cells;
        Assert.Equal("grass", groundCells[0]); // (0,0)
        Assert.Equal("wall", groundCells[4]); // (1,1) = 1 + 1*3
    }

    [Fact]
    public void ExportJson_RoundtripDeserialization()
    {
        var map = new MapData(4, 4);
        map.GetLayer("Ground").SetCell(2, 2, 4, "stone");
        var entity = new Entity { GroupName = "chest", X = 1, Y = 1 };
        entity.Properties["locked"] = "true";
        map.Entities.Add(entity);

        var groups = new List<TileGroup>
        {
            new() { Name = "stone", Type = GroupType.Tile, IsSolid = true,
                    Sprites = new() { new SpriteRef { Col = 3, Row = 1 } } },
            new() { Name = "chest", Type = GroupType.Entity,
                    Sprites = new() { new SpriteRef { Col = 5, Row = 2 } } },
        };

        string json = MapExporter.ExportJson(map, groups);
        var result = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        Assert.Equal(4, result.Width);
        Assert.Equal(4, result.Height);
        Assert.Equal(2, result.Layers.Count);
        Assert.Equal(2, result.Groups.Count);
        Assert.Single(result.Entities);
        Assert.Equal("true", result.Entities[0].Properties["locked"]);
        Assert.True(result.Groups[0].IsSolid);
    }

    [Fact]
    public void ExportJson_HazardousTile_IncludesGameplayProperties()
    {
        var map = new MapData(3, 3);
        var groups = new List<TileGroup>
        {
            new() { Name = "lava", Type = GroupType.Tile, IsSolid = true,
                    IsPassable = false, IsHazardous = true, MovementCost = 1.0f,
                    DamageType = "fire", DamagePerTick = 5,
                    Sprites = new() { new SpriteRef { Col = 3, Row = 2 } } },
        };

        string json = MapExporter.ExportJson(map, groups);
        var result = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        var lava = result.Groups[0];
        Assert.True(lava.IsSolid);
        Assert.False(lava.IsPassable);
        Assert.True(lava.IsHazardous);
        Assert.Equal("fire", lava.DamageType);
        Assert.Equal(5, lava.DamagePerTick);
    }

    [Fact]
    public void ExportJson_DefaultTile_OmitsGameplayProperties()
    {
        var map = new MapData(3, 3);
        var groups = new List<TileGroup>
        {
            new() { Name = "grass", Type = GroupType.Tile },
        };

        string json = MapExporter.ExportJson(map, groups);

        // Default values should be omitted
        Assert.DoesNotContain("\"isPassable\"", json);
        Assert.DoesNotContain("\"isHazardous\"", json);
        Assert.DoesNotContain("\"movementCost\"", json);
        Assert.DoesNotContain("\"damageType\"", json);
        Assert.DoesNotContain("\"damagePerTick\"", json);
        Assert.DoesNotContain("\"entityType\"", json);
    }

    [Fact]
    public void ExportJson_SlowTile_IncludesMovementCost()
    {
        var map = new MapData(3, 3);
        var groups = new List<TileGroup>
        {
            new() { Name = "swamp", Type = GroupType.Tile, MovementCost = 2.0f },
        };

        string json = MapExporter.ExportJson(map, groups);
        var result = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        Assert.Equal(2.0f, result.Groups[0].MovementCost);
    }

    [Fact]
    public void ExportJson_EntityGroup_IncludesEntityType()
    {
        var map = new MapData(3, 3);
        var groups = new List<TileGroup>
        {
            new() { Name = "npc_elder", Type = GroupType.Entity, EntityType = EntityType.NPC },
        };

        string json = MapExporter.ExportJson(map, groups);
        var result = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        Assert.Equal("NPC", result.Groups[0].EntityType);
    }

    [Fact]
    public void ExportJson_TileGroup_OmitsEntityType()
    {
        var map = new MapData(3, 3);
        var groups = new List<TileGroup>
        {
            new() { Name = "grass", Type = GroupType.Tile },
        };

        string json = MapExporter.ExportJson(map, groups);

        Assert.DoesNotContain("\"entityType\"", json);
    }
}
