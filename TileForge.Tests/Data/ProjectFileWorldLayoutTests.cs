using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using TileForge.Data;
using Xunit;

namespace TileForge.Tests.Data;

public class ProjectFileWorldLayoutTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // -------------------------------------------------------------------------
    // 1. WorldLayout serializes to JSON
    // -------------------------------------------------------------------------

    [Fact]
    public void WorldLayout_SerializesToJson_ContainsExpectedKeys()
    {
        var layout = new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                ["overworld"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["dungeon"]   = new MapPlacement { GridX = 1, GridY = 0 },
            }
        };

        string json = JsonSerializer.Serialize(layout, JsonOptions);

        Assert.Contains("\"maps\"", json);
        Assert.Contains("\"overworld\"", json);
        Assert.Contains("\"dungeon\"", json);
        Assert.Contains("\"gridX\"", json);
        Assert.Contains("\"gridY\"", json);
    }

    // -------------------------------------------------------------------------
    // 2. WorldLayout deserializes from JSON
    // -------------------------------------------------------------------------

    [Fact]
    public void WorldLayout_DeserializesFromJson_RestoresMapsAndGridPositions()
    {
        string json = """
            {
              "maps": {
                "overworld": { "gridX": 0, "gridY": 0 },
                "dungeon":   { "gridX": 1, "gridY": 2 }
              }
            }
            """;

        var layout = JsonSerializer.Deserialize<WorldLayout>(json, JsonOptions);

        Assert.NotNull(layout);
        Assert.Equal(2, layout.Maps.Count);
        Assert.True(layout.Maps.ContainsKey("overworld"));
        Assert.True(layout.Maps.ContainsKey("dungeon"));
        Assert.Equal(0, layout.Maps["overworld"].GridX);
        Assert.Equal(0, layout.Maps["overworld"].GridY);
        Assert.Equal(1, layout.Maps["dungeon"].GridX);
        Assert.Equal(2, layout.Maps["dungeon"].GridY);
    }

    // -------------------------------------------------------------------------
    // 3. Round-trip with EdgeSpawn
    // -------------------------------------------------------------------------

    [Fact]
    public void WorldLayout_RoundTrip_WithEdgeSpawns_PreservesAllFields()
    {
        var layout = new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                ["map_a"] = new MapPlacement
                {
                    GridX = 2,
                    GridY = 3,
                    NorthEntry = new EdgeSpawn { X = 5, Y = 0 },
                    SouthEntry = new EdgeSpawn { X = 5, Y = 19 },
                    EastEntry  = new EdgeSpawn { X = 0, Y = 7 },
                    WestEntry  = new EdgeSpawn { X = 29, Y = 7 },
                }
            }
        };

        string json = JsonSerializer.Serialize(layout, JsonOptions);
        var result = JsonSerializer.Deserialize<WorldLayout>(json, JsonOptions);

        Assert.NotNull(result);
        var placement = result.Maps["map_a"];
        Assert.Equal(2,  placement.GridX);
        Assert.Equal(3,  placement.GridY);

        Assert.NotNull(placement.NorthEntry);
        Assert.Equal(5, placement.NorthEntry.X);
        Assert.Equal(0, placement.NorthEntry.Y);

        Assert.NotNull(placement.SouthEntry);
        Assert.Equal(5,  placement.SouthEntry.X);
        Assert.Equal(19, placement.SouthEntry.Y);

        Assert.NotNull(placement.EastEntry);
        Assert.Equal(0, placement.EastEntry.X);
        Assert.Equal(7, placement.EastEntry.Y);

        Assert.NotNull(placement.WestEntry);
        Assert.Equal(29, placement.WestEntry.X);
        Assert.Equal(7,  placement.WestEntry.Y);
    }

    // -------------------------------------------------------------------------
    // 4. Null EdgeSpawn fields omitted from JSON
    // -------------------------------------------------------------------------

    [Fact]
    public void MapPlacement_NullNorthEntry_OmittedFromJson()
    {
        var layout = new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                ["cave"] = new MapPlacement { GridX = 0, GridY = 0, NorthEntry = null }
            }
        };

        string json = JsonSerializer.Serialize(layout, JsonOptions);

        Assert.DoesNotContain("\"northEntry\"", json);
        Assert.DoesNotContain("\"southEntry\"", json);
        Assert.DoesNotContain("\"eastEntry\"", json);
        Assert.DoesNotContain("\"westEntry\"", json);
    }

    // -------------------------------------------------------------------------
    // 5. Non-null EdgeSpawn preserved through round-trip
    // -------------------------------------------------------------------------

    [Fact]
    public void MapPlacement_NonNullNorthEntry_RoundTrips()
    {
        var layout = new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                ["forest"] = new MapPlacement
                {
                    GridX      = 0,
                    GridY      = 0,
                    NorthEntry = new EdgeSpawn { X = 3, Y = 7 },
                }
            }
        };

        string json = JsonSerializer.Serialize(layout, JsonOptions);
        var result = JsonSerializer.Deserialize<WorldLayout>(json, JsonOptions);

        Assert.NotNull(result);
        var placement = result.Maps["forest"];
        Assert.NotNull(placement.NorthEntry);
        Assert.Equal(3, placement.NorthEntry.X);
        Assert.Equal(7, placement.NorthEntry.Y);
        // Other entries remain null
        Assert.Null(placement.SouthEntry);
        Assert.Null(placement.EastEntry);
        Assert.Null(placement.WestEntry);
    }

    // -------------------------------------------------------------------------
    // 6. Null WorldLayout omitted from ProjectData JSON
    // -------------------------------------------------------------------------

    [Fact]
    public void ProjectData_NullWorldLayout_OmittedFromJson()
    {
        var data = new ProjectFile.ProjectData
        {
            Version     = 2,
            WorldLayout = null,
        };

        string json = JsonSerializer.Serialize(data, JsonOptions);

        Assert.DoesNotContain("\"worldLayout\"", json);
    }

    // -------------------------------------------------------------------------
    // 7. ProjectData with WorldLayout round-trips
    // -------------------------------------------------------------------------

    [Fact]
    public void ProjectData_WithWorldLayout_RoundTrips()
    {
        var data = new ProjectFile.ProjectData
        {
            Version = 2,
            WorldLayout = new WorldLayout
            {
                Maps = new Dictionary<string, MapPlacement>
                {
                    ["map1"] = new MapPlacement { GridX = 0, GridY = 0 },
                    ["map2"] = new MapPlacement
                    {
                        GridX      = 1,
                        GridY      = 0,
                        WestEntry  = new EdgeSpawn { X = 0, Y = 5 },
                    },
                }
            }
        };

        string json = JsonSerializer.Serialize(data, JsonOptions);
        var result = JsonSerializer.Deserialize<ProjectFile.ProjectData>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.WorldLayout);
        Assert.Equal(2, result.WorldLayout.Maps.Count);

        var map1 = result.WorldLayout.Maps["map1"];
        Assert.Equal(0, map1.GridX);
        Assert.Equal(0, map1.GridY);
        Assert.Null(map1.WestEntry);

        var map2 = result.WorldLayout.Maps["map2"];
        Assert.Equal(1, map2.GridX);
        Assert.Equal(0, map2.GridY);
        Assert.NotNull(map2.WestEntry);
        Assert.Equal(0, map2.WestEntry.X);
        Assert.Equal(5, map2.WestEntry.Y);
    }

    // -------------------------------------------------------------------------
    // 8. Backward compat: old JSON without worldLayout field -> WorldLayout is null
    // -------------------------------------------------------------------------

    [Fact]
    public void ProjectData_OldJsonWithoutWorldLayout_DeserializesToNull()
    {
        // JSON that pre-dates the WorldLayout feature (V1 format, no worldLayout key)
        string json = """
            {
              "version": 1,
              "spritesheet": {
                "path": "sprites/sheet.png",
                "tileWidth": 16,
                "tileHeight": 16,
                "padding": 0
              },
              "groups": []
            }
            """;

        var result = JsonSerializer.Deserialize<ProjectFile.ProjectData>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.Null(result.WorldLayout);
    }

    // -------------------------------------------------------------------------
    // 9. Exit point fields round-trip through save/load
    // -------------------------------------------------------------------------

    [Fact]
    public void MapPlacement_NorthExit_RoundTripsThroughSaveLoad()
    {
        var layout = new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                ["meadow"] = new MapPlacement
                {
                    GridX     = 0,
                    GridY     = 0,
                    NorthExit = new EdgeSpawn { X = 4, Y = 2 },
                }
            }
        };

        string json = JsonSerializer.Serialize(layout, JsonOptions);
        var result = JsonSerializer.Deserialize<WorldLayout>(json, JsonOptions);

        Assert.NotNull(result);
        var placement = result.Maps["meadow"];
        Assert.NotNull(placement.NorthExit);
        Assert.Equal(4, placement.NorthExit.X);
        Assert.Equal(2, placement.NorthExit.Y);
    }

    // -------------------------------------------------------------------------
    // 10. Null exit points are omitted from JSON (backward compat)
    // -------------------------------------------------------------------------

    [Fact]
    public void MapPlacement_NullExitPoints_OmittedFromJson()
    {
        // A placement with no exit points set — none of the Exit fields should appear in JSON
        var layout = new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                ["plains"] = new MapPlacement { GridX = 1, GridY = 1 }
            }
        };

        string json = JsonSerializer.Serialize(layout, JsonOptions);

        Assert.DoesNotContain("\"northExit\"", json);
        Assert.DoesNotContain("\"southExit\"", json);
        Assert.DoesNotContain("\"eastExit\"", json);
        Assert.DoesNotContain("\"westExit\"", json);
    }
}
