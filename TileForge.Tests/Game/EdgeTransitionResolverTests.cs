using System.Collections.Generic;
using Xunit;
using TileForge.Data;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class EdgeTransitionResolverTests
{
    // ========== Helpers ==========

    private static LoadedMap CreateLoadedMap(string id, int width, int height) =>
        new() { Id = id, Width = width, Height = height };

    /// <summary>
    /// Builds a WorldLayout with Map A at (0,0) and Map B at (1,0) — B is east of A.
    /// </summary>
    private static (WorldLayout layout, Dictionary<string, LoadedMap> maps) BuildEastWestPair(
        int mapAWidth = 10, int mapAHeight = 10,
        int mapBWidth = 10, int mapBHeight = 10,
        MapPlacement placementB = null)
    {
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["MapB"] = placementB ?? new MapPlacement { GridX = 1, GridY = 0 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", mapAWidth, mapAHeight),
            ["MapB"] = CreateLoadedMap("MapB", mapBWidth, mapBHeight),
        };
        return (layout, maps);
    }

    /// <summary>
    /// Builds a WorldLayout with Map A at (0,0) and Map C at (0,1) — C is south of A.
    /// </summary>
    private static (WorldLayout layout, Dictionary<string, LoadedMap> maps) BuildNorthSouthPair(
        int mapAWidth = 10, int mapAHeight = 10,
        int mapCWidth = 10, int mapCHeight = 10,
        MapPlacement placementC = null)
    {
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["MapC"] = placementC ?? new MapPlacement { GridX = 0, GridY = 1 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", mapAWidth, mapAHeight),
            ["MapC"] = CreateLoadedMap("MapC", mapCWidth, mapCHeight),
        };
        return (layout, maps);
    }

    // ========== 1. In-bounds movement returns null ==========

    [Fact]
    public void Resolve_InBoundsTarget_ReturnsNull()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Player at (5,5) on a 10x10 map — still in bounds
        var result = resolver.Resolve("MapA", 5, 5, 4, 5, 10, 10);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_TargetAtMapCorner_InBounds_ReturnsNull()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Corners of a 10x10 map are tiles 0–9, so (9,9) is still in bounds
        var result = resolver.Resolve("MapA", 9, 9, 8, 9, 10, 10);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_TargetAtOrigin_InBounds_ReturnsNull()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 0, 0, 1, 0, 10, 10);

        Assert.Null(result);
    }

    // ========== 2. Exit east with neighbor ==========

    [Fact]
    public void Resolve_ExitEast_WithNeighbor_ReturnsTransitionToMapB()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        // targetX = 10 is off the east edge of a width-10 map
        var result = resolver.Resolve("MapA", 10, 5, 9, 5, 10, 10);

        Assert.NotNull(result);
        Assert.Equal("MapB", result.TargetMap);
    }

    // ========== 3. Exit west with neighbor ==========

    [Fact]
    public void Resolve_ExitWest_WithNeighbor_ReturnsTransitionToMapA()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        // targetX = -1 is off the west edge
        var result = resolver.Resolve("MapB", -1, 5, 0, 5, 10, 10);

        Assert.NotNull(result);
        Assert.Equal("MapA", result.TargetMap);
    }

    // ========== 4. Exit north with neighbor ==========

    [Fact]
    public void Resolve_ExitNorth_WithNeighbor_ReturnsTransitionToNorthMap()
    {
        // MapA at (0,0), MapD at (0,-1) — north of A
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["MapD"] = new MapPlacement { GridX = 0, GridY = -1 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
            ["MapD"] = CreateLoadedMap("MapD", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        // targetY = -1 is off the north edge
        var result = resolver.Resolve("MapA", 5, -1, 5, 0, 10, 10);

        Assert.NotNull(result);
        Assert.Equal("MapD", result.TargetMap);
    }

    // ========== 5. Exit south with neighbor ==========

    [Fact]
    public void Resolve_ExitSouth_WithNeighbor_ReturnsTransitionToMapC()
    {
        var (layout, maps) = BuildNorthSouthPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        // targetY = 10 is off the south edge of a height-10 map
        var result = resolver.Resolve("MapA", 5, 10, 5, 9, 10, 10);

        Assert.NotNull(result);
        Assert.Equal("MapC", result.TargetMap);
    }

    // ========== 6. No neighbor returns null ==========

    [Fact]
    public void Resolve_ExitEast_NoNeighbor_ReturnsNull()
    {
        // Only MapA, no map to the east
        var layout = new WorldLayout
        {
            Maps = { ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 } }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 10, 5, 9, 5, 10, 10);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_ExitWest_NoNeighbor_ReturnsNull()
    {
        var layout = new WorldLayout
        {
            Maps = { ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 } }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", -1, 5, 0, 5, 10, 10);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_ExitNorth_NoNeighbor_ReturnsNull()
    {
        var layout = new WorldLayout
        {
            Maps = { ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 } }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 5, -1, 5, 0, 10, 10);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_ExitSouth_NoNeighbor_ReturnsNull()
    {
        var layout = new WorldLayout
        {
            Maps = { ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 } }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 5, 10, 5, 9, 10, 10);

        Assert.Null(result);
    }

    // ========== 7. Default spawn position east ==========

    [Fact]
    public void Resolve_ExitEast_DefaultSpawn_SpawnsAtWestEdgeOfTarget()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Player at (9,5) exits east; should spawn at (0,5) on MapB
        var result = resolver.Resolve("MapA", 10, 5, 9, 5, 10, 10);

        Assert.NotNull(result);
        Assert.Equal(0, result.TargetX);
        Assert.Equal(5, result.TargetY);
    }

    [Fact]
    public void Resolve_ExitEast_DefaultSpawn_PreservesYCoordinate()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Exit east at Y=5 (as specified in the task description)
        var result = resolver.Resolve("MapA", 10, 5, 9, 5, 10, 10);

        Assert.Equal(0, result.TargetX);
        Assert.Equal(5, result.TargetY);
    }

    // ========== 8. Default spawn position west ==========

    [Fact]
    public void Resolve_ExitWest_DefaultSpawn_SpawnsAtEastEdgeOfTarget()
    {
        var (layout, maps) = BuildEastWestPair(mapAWidth: 10, mapAHeight: 10, mapBWidth: 12, mapBHeight: 10);
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Player at (0,3) exits west; should spawn at (mapBWidth-1, 3) = (11, 3) on MapA
        // But wait — MapA is at GridX=0, MapB at GridX=1. Exiting west from MapB lands on MapA.
        // MapA width=10, so spawn at (9, 3).
        var result = resolver.Resolve("MapB", -1, 3, 0, 3, 12, 10);

        Assert.NotNull(result);
        Assert.Equal(9, result.TargetX); // MapA width - 1
        Assert.Equal(3, result.TargetY);
    }

    [Fact]
    public void Resolve_ExitWest_DefaultSpawn_PreservesYCoordinate()
    {
        // Simpler: single pair, exit west from MapB to MapA at Y=3
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapB", -1, 3, 0, 3, 10, 10);

        Assert.Equal(9, result.TargetX); // targetWidth - 1 = 10 - 1
        Assert.Equal(3, result.TargetY);
    }

    // ========== 9. Default spawn position south ==========

    [Fact]
    public void Resolve_ExitSouth_DefaultSpawn_SpawnsAtNorthEdgeOfTarget()
    {
        var (layout, maps) = BuildNorthSouthPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Player at (4,9) exits south; should spawn at (4,0) on MapC
        var result = resolver.Resolve("MapA", 4, 10, 4, 9, 10, 10);

        Assert.NotNull(result);
        Assert.Equal(4, result.TargetX);
        Assert.Equal(0, result.TargetY);
    }

    [Fact]
    public void Resolve_ExitSouth_DefaultSpawn_PreservesXCoordinate()
    {
        var (layout, maps) = BuildNorthSouthPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 4, 10, 4, 9, 10, 10);

        Assert.Equal(4, result.TargetX);
        Assert.Equal(0, result.TargetY);
    }

    // ========== 10. Default spawn position north ==========

    [Fact]
    public void Resolve_ExitNorth_DefaultSpawn_SpawnsAtSouthEdgeOfTarget()
    {
        // MapA at (0,0), MapD at (0,-1) — MapD is north of MapA
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["MapD"] = new MapPlacement { GridX = 0, GridY = -1 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
            ["MapD"] = CreateLoadedMap("MapD", 10, 8),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Player at (2,0) exits north; should spawn at (2, mapDHeight-1) = (2, 7)
        var result = resolver.Resolve("MapA", 2, -1, 2, 0, 10, 10);

        Assert.NotNull(result);
        Assert.Equal(2, result.TargetX);
        Assert.Equal(7, result.TargetY); // MapD height - 1
    }

    [Fact]
    public void Resolve_ExitNorth_DefaultSpawn_PreservesXCoordinate()
    {
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["MapD"] = new MapPlacement { GridX = 0, GridY = -1 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
            ["MapD"] = CreateLoadedMap("MapD", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Exit north at X=2 (as specified in the task description)
        var result = resolver.Resolve("MapA", 2, -1, 2, 0, 10, 10);

        Assert.Equal(2, result.TargetX);
        Assert.Equal(9, result.TargetY); // MapD height - 1
    }

    // ========== 11. Custom spawn via EdgeSpawn ==========

    [Fact]
    public void Resolve_ExitEast_CustomWestEntry_UsesEdgeSpawnCoordinates()
    {
        // MapB has a WestEntry override: spawn at (3, 7) when player enters from the west
        var placementB = new MapPlacement
        {
            GridX = 1,
            GridY = 0,
            WestEntry = new EdgeSpawn { X = 3, Y = 7 },
        };
        var (layout, maps) = BuildEastWestPair(placementB: placementB);
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 10, 5, 9, 5, 10, 10);

        Assert.NotNull(result);
        Assert.Equal("MapB", result.TargetMap);
        Assert.Equal(3, result.TargetX);
        Assert.Equal(7, result.TargetY);
    }

    [Fact]
    public void Resolve_ExitWest_CustomEastEntry_UsesEdgeSpawnCoordinates()
    {
        // MapA has an EastEntry override: spawn at (8, 2) when player enters from the east
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement
                {
                    GridX = 0,
                    GridY = 0,
                    EastEntry = new EdgeSpawn { X = 8, Y = 2 },
                },
                ["MapB"] = new MapPlacement { GridX = 1, GridY = 0 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
            ["MapB"] = CreateLoadedMap("MapB", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Exiting west from MapB — enters MapA from the east side
        var result = resolver.Resolve("MapB", -1, 4, 0, 4, 10, 10);

        Assert.NotNull(result);
        Assert.Equal("MapA", result.TargetMap);
        Assert.Equal(8, result.TargetX);
        Assert.Equal(2, result.TargetY);
    }

    [Fact]
    public void Resolve_ExitSouth_CustomNorthEntry_UsesEdgeSpawnCoordinates()
    {
        // MapC has a NorthEntry override: spawn at (6, 1)
        var placementC = new MapPlacement
        {
            GridX = 0,
            GridY = 1,
            NorthEntry = new EdgeSpawn { X = 6, Y = 1 },
        };
        var (layout, maps) = BuildNorthSouthPair(placementC: placementC);
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 3, 10, 3, 9, 10, 10);

        Assert.NotNull(result);
        Assert.Equal("MapC", result.TargetMap);
        Assert.Equal(6, result.TargetX);
        Assert.Equal(1, result.TargetY);
    }

    // ========== 12. Coordinate clamped for smaller target ==========

    [Fact]
    public void Resolve_ExitEast_PlayerYExceedsTargetHeight_ClampsY()
    {
        // Source map 20 tall, target map 10 tall; player exits at Y=15 → clamped to 9
        var (layout, maps) = BuildEastWestPair(mapAWidth: 20, mapAHeight: 20, mapBWidth: 10, mapBHeight: 10);
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Player is at (19, 15) and exits east
        var result = resolver.Resolve("MapA", 20, 15, 19, 15, 20, 20);

        Assert.NotNull(result);
        Assert.Equal(0, result.TargetX);
        Assert.Equal(9, result.TargetY); // clamped from 15 to targetHeight-1=9
    }

    [Fact]
    public void Resolve_ExitWest_PlayerYExceedsTargetHeight_ClampsY()
    {
        // MapA is 10 tall, MapB is 20 tall; player exits west from MapB at Y=17 → no clamp needed
        // Let's test: MapA=10 tall, MapB=20 tall, exit west from MapB at Y=17 → clamp to MapA height-1=9
        var (layout, maps) = BuildEastWestPair(mapAWidth: 10, mapAHeight: 10, mapBWidth: 10, mapBHeight: 20);
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapB", -1, 17, 0, 17, 10, 20);

        Assert.NotNull(result);
        Assert.Equal(9, result.TargetX); // MapA width - 1
        Assert.Equal(9, result.TargetY); // clamped from 17 to MapA height-1=9
    }

    [Fact]
    public void Resolve_ExitSouth_PlayerXExceedsTargetWidth_ClampsX()
    {
        // Source map 20 wide, target map 10 wide; exit south at X=15 → clamped to 9
        var (layout, maps) = BuildNorthSouthPair(mapAWidth: 20, mapAHeight: 10, mapCWidth: 10, mapCHeight: 10);
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 15, 10, 15, 9, 20, 10);

        Assert.NotNull(result);
        Assert.Equal(9, result.TargetX); // clamped from 15 to targetWidth-1=9
        Assert.Equal(0, result.TargetY);
    }

    [Fact]
    public void Resolve_ExitNorth_PlayerXExceedsTargetWidth_ClampsX()
    {
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["MapD"] = new MapPlacement { GridX = 0, GridY = -1 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 20, 10),
            ["MapD"] = CreateLoadedMap("MapD", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Player exits north at X=18, target only 10 wide → clamp to 9
        var result = resolver.Resolve("MapA", 18, -1, 18, 0, 20, 10);

        Assert.NotNull(result);
        Assert.Equal(9, result.TargetX); // clamped from 18 to MapD width-1=9
        Assert.Equal(9, result.TargetY); // MapD height - 1
    }

    // ========== 13. Null layout returns null ==========

    [Fact]
    public void Resolve_NullLayout_ReturnsNull()
    {
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(null, maps);

        var result = resolver.Resolve("MapA", 10, 5, 9, 5, 10, 10);

        Assert.Null(result);
    }

    // ========== 14. Null project maps returns null ==========

    [Fact]
    public void Resolve_NullProjectMaps_ReturnsNull()
    {
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["MapB"] = new MapPlacement { GridX = 1, GridY = 0 },
            }
        };
        var resolver = new EdgeTransitionResolver(layout, null);

        var result = resolver.Resolve("MapA", 10, 5, 9, 5, 10, 10);

        Assert.Null(result);
    }

    // ========== 15. Empty current map name returns null ==========

    [Fact]
    public void Resolve_EmptyCurrentMapName_ReturnsNull()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("", 10, 5, 9, 5, 10, 10);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_NullCurrentMapName_ReturnsNull()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve(null, 10, 5, 9, 5, 10, 10);

        Assert.Null(result);
    }

    // ========== 16. Target map not in project maps returns null ==========

    [Fact]
    public void Resolve_NeighborExistsInLayoutButNotInProjectMaps_ReturnsNull()
    {
        // WorldLayout knows about MapB at (1,0), but it has not been exported to a LoadedMap
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["MapB"] = new MapPlacement { GridX = 1, GridY = 0 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            // MapB intentionally absent — not yet exported
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 10, 5, 9, 5, 10, 10);

        Assert.Null(result);
    }

    // ========== 17. Chained transitions ==========

    [Fact]
    public void Resolve_ChainedTransitions_ExitEastFromAToB_ThenBToC()
    {
        // A at (0,0), B at (1,0), C at (2,0)
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["MapB"] = new MapPlacement { GridX = 1, GridY = 0 },
                ["MapC"] = new MapPlacement { GridX = 2, GridY = 0 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
            ["MapB"] = CreateLoadedMap("MapB", 10, 10),
            ["MapC"] = CreateLoadedMap("MapC", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        // First: exit east from MapA → MapB
        var firstTransition = resolver.Resolve("MapA", 10, 4, 9, 4, 10, 10);

        Assert.NotNull(firstTransition);
        Assert.Equal("MapB", firstTransition.TargetMap);
        Assert.Equal(0, firstTransition.TargetX);
        Assert.Equal(4, firstTransition.TargetY);

        // Second: player is now on MapB at (0,4); exit east from MapB → MapC
        var secondTransition = resolver.Resolve("MapB", 10, 4, 9, 4, 10, 10);

        Assert.NotNull(secondTransition);
        Assert.Equal("MapC", secondTransition.TargetMap);
        Assert.Equal(0, secondTransition.TargetX);
        Assert.Equal(4, secondTransition.TargetY);
    }

    [Fact]
    public void Resolve_ChainedTransitions_ExitEastThenWest_ReturnToOrigin()
    {
        // A at (0,0), B at (1,0) — exit east from A, then exit west from B to return to A
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Exit east from MapA at Y=6
        var eastResult = resolver.Resolve("MapA", 10, 6, 9, 6, 10, 10);

        Assert.NotNull(eastResult);
        Assert.Equal("MapB", eastResult.TargetMap);
        Assert.Equal(0, eastResult.TargetX);
        Assert.Equal(6, eastResult.TargetY);

        // Now exit west from MapB at Y=6 — should land back on MapA east edge
        var westResult = resolver.Resolve("MapB", -1, 6, 0, 6, 10, 10);

        Assert.NotNull(westResult);
        Assert.Equal("MapA", westResult.TargetMap);
        Assert.Equal(9, westResult.TargetX); // MapA width - 1
        Assert.Equal(6, westResult.TargetY);
    }

    // ========== Additional edge cases ==========

    [Fact]
    public void Resolve_CurrentMapNotInLayout_ReturnsNull()
    {
        // MapA is not in the layout's map dictionary
        var layout = new WorldLayout
        {
            Maps = { ["MapB"] = new MapPlacement { GridX = 1, GridY = 0 } }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
            ["MapB"] = CreateLoadedMap("MapB", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 10, 5, 9, 5, 10, 10);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_ExitEast_ReturnsCorrectTargetMapName()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 10, 0, 9, 0, 10, 10);

        Assert.Equal("MapB", result.TargetMap);
    }

    [Fact]
    public void Resolve_ExitSouth_ReturnsCorrectTargetMapName()
    {
        var (layout, maps) = BuildNorthSouthPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 0, 10, 0, 9, 10, 10);

        Assert.Equal("MapC", result.TargetMap);
    }

    [Fact]
    public void Resolve_ExitEast_AtYZero_SpawnsAtY0OnTarget()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.Resolve("MapA", 10, 0, 9, 0, 10, 10);

        Assert.Equal(0, result.TargetX);
        Assert.Equal(0, result.TargetY);
    }

    [Fact]
    public void Resolve_ExitEast_AtMaxY_SpawnsAtMaxYOnTarget()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Player at (9,9) — bottom-right corner — exits east
        var result = resolver.Resolve("MapA", 10, 9, 9, 9, 10, 10);

        Assert.Equal(0, result.TargetX);
        Assert.Equal(9, result.TargetY);
    }

    [Fact]
    public void Resolve_MultipleNeighbors_CorrectNeighborSelected()
    {
        // A at (0,0) with B to the east at (1,0) and C to the south at (0,1)
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["MapB"] = new MapPlacement { GridX = 1, GridY = 0 },
                ["MapC"] = new MapPlacement { GridX = 0, GridY = 1 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 10, 10),
            ["MapB"] = CreateLoadedMap("MapB", 10, 10),
            ["MapC"] = CreateLoadedMap("MapC", 10, 10),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var eastResult = resolver.Resolve("MapA", 10, 5, 9, 5, 10, 10);
        var southResult = resolver.Resolve("MapA", 5, 10, 5, 9, 10, 10);

        Assert.Equal("MapB", eastResult.TargetMap);
        Assert.Equal("MapC", southResult.TargetMap);
    }

    // ========== ResolveExitPoint ==========

    [Fact]
    public void ResolveExitPoint_NoExitPointsDefined_ReturnsNull()
    {
        // MapA has no exit points configured at all
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0 },
                ["MapB"] = new MapPlacement { GridX = 1, GridY = 0 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 20, 15),
            ["MapB"] = CreateLoadedMap("MapB", 20, 15),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.ResolveExitPoint("MapA", 5, 10);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveExitPoint_TargetDoesNotMatchAnyExitPoint_ReturnsNull()
    {
        // EastExit is at (5, 10); querying (6, 10) should not match
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0, EastExit = new EdgeSpawn { X = 5, Y = 10 } },
                ["MapB"] = new MapPlacement { GridX = 1, GridY = 0 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 20, 15),
            ["MapB"] = CreateLoadedMap("MapB", 20, 15),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.ResolveExitPoint("MapA", 6, 10);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveExitPoint_TargetMatchesEastExit_ReturnsTransitionToEastNeighbor()
    {
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0, EastExit = new EdgeSpawn { X = 5, Y = 10 } },
                ["MapB"] = new MapPlacement { GridX = 1, GridY = 0 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 20, 15),
            ["MapB"] = CreateLoadedMap("MapB", 20, 15),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.ResolveExitPoint("MapA", 5, 10);

        Assert.NotNull(result);
        Assert.Equal("MapB", result.TargetMap);
    }

    [Fact]
    public void ResolveExitPoint_TargetMatchesNorthExit_ReturnsTransitionToNorthNeighbor()
    {
        // MapD is north of MapA
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0, NorthExit = new EdgeSpawn { X = 8, Y = 3 } },
                ["MapD"] = new MapPlacement { GridX = 0, GridY = -1 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 20, 15),
            ["MapD"] = CreateLoadedMap("MapD", 20, 15),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.ResolveExitPoint("MapA", 8, 3);

        Assert.NotNull(result);
        Assert.Equal("MapD", result.TargetMap);
    }

    [Fact]
    public void ResolveExitPoint_DefaultSpawn_UsesOppositeEdgeOfTarget()
    {
        // Player exits east via an interior exit point; default spawn = west edge of MapB
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0, EastExit = new EdgeSpawn { X = 5, Y = 10 } },
                ["MapB"] = new MapPlacement { GridX = 1, GridY = 0 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 20, 15),
            ["MapB"] = CreateLoadedMap("MapB", 20, 15),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.ResolveExitPoint("MapA", 5, 10);

        Assert.NotNull(result);
        // Exiting east -> spawns at x=0 (west edge) on MapB; y clamped from exit Y=10 to MapB height-1=14 (no clamp needed)
        Assert.Equal(0, result.TargetX);
        Assert.Equal(10, result.TargetY);
    }

    [Fact]
    public void ResolveExitPoint_NeighborHasEntryOverride_UsesEntryOverrideCoordinates()
    {
        // MapB has a WestEntry override; entering from the west side uses this instead of default
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0, EastExit = new EdgeSpawn { X = 5, Y = 10 } },
                ["MapB"] = new MapPlacement { GridX = 1, GridY = 0, WestEntry = new EdgeSpawn { X = 2, Y = 7 } },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 20, 15),
            ["MapB"] = CreateLoadedMap("MapB", 20, 15),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.ResolveExitPoint("MapA", 5, 10);

        Assert.NotNull(result);
        Assert.Equal("MapB", result.TargetMap);
        Assert.Equal(2, result.TargetX);
        Assert.Equal(7, result.TargetY);
    }

    [Fact]
    public void ResolveExitPoint_NoNeighborInExitDirection_ReturnsNull()
    {
        // EastExit is set but there is no map east of MapA
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0, EastExit = new EdgeSpawn { X = 5, Y = 10 } },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 20, 15),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.ResolveExitPoint("MapA", 5, 10);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveExitPoint_NullMapName_ReturnsNull()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.ResolveExitPoint(null, 5, 5);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveExitPoint_EmptyMapName_ReturnsNull()
    {
        var (layout, maps) = BuildEastWestPair();
        var resolver = new EdgeTransitionResolver(layout, maps);

        var result = resolver.ResolveExitPoint("", 5, 5);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveExitPoint_CoexistsWithEdgeTransitions_BothWorkIndependently()
    {
        // MapA has an EastExit interior portal at (5, 7) AND a neighbor to the east for normal edge transitions
        var layout = new WorldLayout
        {
            Maps =
            {
                ["MapA"] = new MapPlacement { GridX = 0, GridY = 0, EastExit = new EdgeSpawn { X = 5, Y = 7 } },
                ["MapB"] = new MapPlacement { GridX = 1, GridY = 0 },
            }
        };
        var maps = new Dictionary<string, LoadedMap>
        {
            ["MapA"] = CreateLoadedMap("MapA", 20, 15),
            ["MapB"] = CreateLoadedMap("MapB", 20, 15),
        };
        var resolver = new EdgeTransitionResolver(layout, maps);

        // Interior exit point at (5, 7) resolves to MapB
        var exitPointResult = resolver.ResolveExitPoint("MapA", 5, 7);

        // Edge transition off the east boundary also resolves to MapB
        var edgeResult = resolver.Resolve("MapA", 20, 7, 19, 7, 20, 15);

        Assert.NotNull(exitPointResult);
        Assert.Equal("MapB", exitPointResult.TargetMap);

        Assert.NotNull(edgeResult);
        Assert.Equal("MapB", edgeResult.TargetMap);
    }
}
