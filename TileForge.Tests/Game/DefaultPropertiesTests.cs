using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using TileForge.Data;
using TileForge.Export;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class DefaultPropertiesTests
{
    // ========== TileGroup ==========

    [Fact]
    public void TileGroup_DefaultProperties_DefaultsToEmptyDictionary()
    {
        var group = new TileGroup();

        Assert.NotNull(group.DefaultProperties);
        Assert.Empty(group.DefaultProperties);
    }

    [Fact]
    public void TileGroup_DefaultProperties_CanBeSet()
    {
        var group = new TileGroup
        {
            DefaultProperties = new Dictionary<string, string> { ["heal"] = "25" }
        };

        Assert.Single(group.DefaultProperties);
        Assert.Equal("25", group.DefaultProperties["heal"]);
    }

    // ========== ProjectFile.RestoreGroups ==========

    [Fact]
    public void ProjectFile_SaveRestore_PreservesDefaultProperties()
    {
        var groupData = new ProjectFile.GroupData
        {
            Name = "potion",
            Type = "Entity",
            DefaultProperties = new Dictionary<string, string>
            {
                ["heal"] = "25",
                ["color"] = "red",
            },
            Sprites = new List<ProjectFile.SpriteRefData>(),
        };

        var projectData = new ProjectFile.ProjectData
        {
            Groups = new List<ProjectFile.GroupData> { groupData },
            Map = new ProjectFile.MapInfo
            {
                Width = 2, Height = 2,
                Layers = new List<ProjectFile.LayerData>(),
            },
        };

        var groups = ProjectFile.RestoreGroups(projectData);

        Assert.Single(groups);
        var restored = groups[0];
        Assert.Equal(2, restored.DefaultProperties.Count);
        Assert.Equal("25", restored.DefaultProperties["heal"]);
        Assert.Equal("red", restored.DefaultProperties["color"]);
    }

    [Fact]
    public void ProjectFile_Restore_NullDefaultProperties_DefaultsToEmpty()
    {
        var groupData = new ProjectFile.GroupData
        {
            Name = "rock",
            Type = "Tile",
            DefaultProperties = null,
            Sprites = new List<ProjectFile.SpriteRefData>(),
        };

        var projectData = new ProjectFile.ProjectData
        {
            Groups = new List<ProjectFile.GroupData> { groupData },
            Map = new ProjectFile.MapInfo
            {
                Width = 2, Height = 2,
                Layers = new List<ProjectFile.LayerData>(),
            },
        };

        var groups = ProjectFile.RestoreGroups(projectData);

        Assert.Single(groups);
        Assert.NotNull(groups[0].DefaultProperties);
        Assert.Empty(groups[0].DefaultProperties);
    }

    // ========== MapExporter ==========

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact]
    public void MapExporter_IncludesDefaultProperties()
    {
        var group = new TileGroup
        {
            Name = "chest",
            Type = GroupType.Entity,
            EntityType = EntityType.Item,
            DefaultProperties = new Dictionary<string, string>
            {
                ["loot"] = "gold",
                ["quantity"] = "3",
            },
        };
        var map = new MapData(2, 2);
        var groups = new List<TileGroup> { group };

        string json = MapExporter.ExportJson(map, groups);
        var data = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        Assert.NotNull(data);
        Assert.Single(data.Groups);
        Assert.NotNull(data.Groups[0].DefaultProperties);
        Assert.Equal(2, data.Groups[0].DefaultProperties.Count);
        Assert.Equal("gold", data.Groups[0].DefaultProperties["loot"]);
        Assert.Equal("3", data.Groups[0].DefaultProperties["quantity"]);
    }

    [Fact]
    public void MapExporter_OmitsEmptyDefaultProperties()
    {
        var group = new TileGroup
        {
            Name = "grass",
            Type = GroupType.Tile,
            DefaultProperties = new Dictionary<string, string>(),
        };
        var map = new MapData(2, 2);
        var groups = new List<TileGroup> { group };

        string json = MapExporter.ExportJson(map, groups);
        var data = JsonSerializer.Deserialize<ExportData>(json, DeserializeOptions);

        Assert.NotNull(data);
        Assert.Single(data.Groups);
        Assert.Null(data.Groups[0].DefaultProperties);
    }

    // ========== MapLoader ==========

    [Fact]
    public void MapLoader_ReadsDefaultProperties()
    {
        var exportData = new ExportData
        {
            Width = 2,
            Height = 2,
            Groups = new List<ExportGroup>
            {
                new()
                {
                    Name = "shrine",
                    Type = "Entity",
                    EntityType = "Interactable",
                    DefaultProperties = new Dictionary<string, string>
                    {
                        ["effect"] = "heal",
                        ["power"] = "10",
                    },
                },
            },
            Layers = new List<ExportLayer>(),
            Entities = new List<ExportEntity>(),
        };

        var loader = new MapLoader();
        var loadedMap = loader.LoadFromExportData(exportData, "test");

        Assert.Single(loadedMap.Groups);
        var loaded = loadedMap.Groups[0];
        Assert.NotNull(loaded.DefaultProperties);
        Assert.Equal(2, loaded.DefaultProperties.Count);
        Assert.Equal("heal", loaded.DefaultProperties["effect"]);
        Assert.Equal("10", loaded.DefaultProperties["power"]);
    }

    [Fact]
    public void MapLoader_NullDefaultProperties_DefaultsToEmpty()
    {
        var exportData = new ExportData
        {
            Width = 2,
            Height = 2,
            Groups = new List<ExportGroup>
            {
                new()
                {
                    Name = "rock",
                    Type = "Tile",
                    DefaultProperties = null,
                },
            },
            Layers = new List<ExportLayer>(),
            Entities = new List<ExportEntity>(),
        };

        var loader = new MapLoader();
        var loadedMap = loader.LoadFromExportData(exportData, "test");

        Assert.Single(loadedMap.Groups);
        Assert.NotNull(loadedMap.Groups[0].DefaultProperties);
        Assert.Empty(loadedMap.Groups[0].DefaultProperties);
    }

    // ========== Roundtrip: MapExporter → MapLoader ==========

    [Fact]
    public void MapExporter_MapLoader_Roundtrip_PreservesDefaultProperties()
    {
        var group = new TileGroup
        {
            Name = "vendor",
            Type = GroupType.Entity,
            EntityType = EntityType.NPC,
            DefaultProperties = new Dictionary<string, string>
            {
                ["shop"] = "weapons",
                ["greeting"] = "Hello traveler",
            },
        };
        var map = new MapData(2, 2);
        var groups = new List<TileGroup> { group };

        string json = MapExporter.ExportJson(map, groups);

        var loader = new MapLoader();
        var loadedMap = loader.Load(json, "roundtrip_test");

        Assert.Single(loadedMap.Groups);
        var loaded = loadedMap.Groups[0];
        Assert.Equal("vendor", loaded.Name);
        Assert.NotNull(loaded.DefaultProperties);
        Assert.Equal(2, loaded.DefaultProperties.Count);
        Assert.Equal("weapons", loaded.DefaultProperties["shop"]);
        Assert.Equal("Hello traveler", loaded.DefaultProperties["greeting"]);
    }
}
