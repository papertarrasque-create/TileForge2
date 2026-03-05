using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using TileForge.Data;
using TileForge.Export;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class EquipmentExportTests
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static string BuildExportJsonWithEquipGroup(
        int width = 5, int height = 5,
        Dictionary<string, string> defaultProperties = null)
    {
        var data = new ExportData
        {
            Width = width,
            Height = height,
            Layers = new List<ExportLayer>
            {
                new() { Name = "Ground", Cells = new string[width * height] },
            },
            Groups = new List<ExportGroup>
            {
                new()
                {
                    Name = "iron_sword",
                    Type = "Entity",
                    EntityType = "Item",
                    DefaultProperties = defaultProperties,
                },
            },
            Entities = new List<ExportEntity>(),
        };
        return JsonSerializer.Serialize(data, ExportJsonOptions);
    }

    [Fact]
    public void MapExporter_IncludesEquipmentProperties()
    {
        var map = new MapData(5, 5);
        var group = new TileGroup
        {
            Name = "iron_sword",
            Type = GroupType.Entity,
            EntityType = EntityType.Item,
            DefaultProperties = new Dictionary<string, string>
            {
                ["equip_slot"] = "weapon",
                ["equip_attack"] = "15",
                ["equip_defense"] = "0",
            },
            Sprites = new List<SpriteRef> { new() { Col = 0, Row = 0 } },
        };
        var groups = new List<TileGroup> { group };

        string json = MapExporter.ExportJson(map, groups);

        Assert.Contains("equip_slot", json);
        Assert.Contains("equip_attack", json);
        Assert.Contains("equip_defense", json);
    }

    [Fact]
    public void MapLoader_ReadsEquipmentProperties()
    {
        var defaultProps = new Dictionary<string, string>
        {
            ["equip_slot"] = "armor",
            ["equip_attack"] = "2",
            ["equip_defense"] = "10",
        };
        string json = BuildExportJsonWithEquipGroup(defaultProperties: defaultProps);
        var loader = new MapLoader();

        var map = loader.Load(json);

        Assert.Single(map.Groups);
        var ironSword = map.Groups[0];
        Assert.NotNull(ironSword.DefaultProperties);
        Assert.True(ironSword.DefaultProperties.ContainsKey("equip_slot"));
        Assert.True(ironSword.DefaultProperties.ContainsKey("equip_attack"));
        Assert.True(ironSword.DefaultProperties.ContainsKey("equip_defense"));
    }

    [Fact]
    public void ExportRoundtrip_PreservesEquipmentProperties()
    {
        var map = new MapData(5, 5);
        var group = new TileGroup
        {
            Name = "chainmail",
            Type = GroupType.Entity,
            EntityType = EntityType.Item,
            DefaultProperties = new Dictionary<string, string>
            {
                ["equip_slot"] = "armor",
                ["equip_attack"] = "0",
                ["equip_defense"] = "20",
            },
            Sprites = new List<SpriteRef> { new() { Col = 1, Row = 0 } },
        };
        var groups = new List<TileGroup> { group };

        string json = MapExporter.ExportJson(map, groups);
        var loader = new MapLoader();
        var loadedMap = loader.Load(json);

        Assert.Single(loadedMap.Groups);
        var loadedGroup = loadedMap.Groups[0];
        Assert.Equal("armor", loadedGroup.DefaultProperties["equip_slot"]);
        Assert.Equal("0", loadedGroup.DefaultProperties["equip_attack"]);
        Assert.Equal("20", loadedGroup.DefaultProperties["equip_defense"]);
    }

    [Fact]
    public void CollectItem_CachesEquipmentProperties()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState
        {
            X = 0, Y = 0, Health = 100, MaxHealth = 100,
        };

        var entity = new EntityInstance
        {
            Id = "item_01",
            DefinitionName = "iron_sword",
            X = 1,
            Y = 1,
            IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["equip_slot"] = "weapon",
                ["equip_attack"] = "15",
                ["equip_defense"] = "0",
            },
        };
        gsm.State.ActiveEntities.Add(entity);

        gsm.CollectItem(entity);

        Assert.True(gsm.State.ItemPropertyCache.ContainsKey("iron_sword"));
        var cached = gsm.State.ItemPropertyCache["iron_sword"];
        Assert.Equal("weapon", cached["equip_slot"]);
        Assert.Equal("15", cached["equip_attack"]);
        Assert.Equal("0", cached["equip_defense"]);
    }
}
