using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Data;

public class WorldLayoutTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static WorldLayout TwoMapLayout(
        string nameA, int axA, int ayA,
        string nameB, int axB, int ayB)
    {
        return new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                [nameA] = new MapPlacement { GridX = axA, GridY = ayA },
                [nameB] = new MapPlacement { GridX = axB, GridY = ayB },
            }
        };
    }

    // -------------------------------------------------------------------------
    // GetExitDirection
    // -------------------------------------------------------------------------

    [Fact]
    public void GetExitDirection_TargetYNegative_ReturnsUp()
    {
        var result = WorldLayoutHelper.GetExitDirection(5, -1, 20, 15);

        Assert.Equal(Direction.Up, result);
    }

    [Fact]
    public void GetExitDirection_TargetYEqualsMapHeight_ReturnsDown()
    {
        var result = WorldLayoutHelper.GetExitDirection(5, 15, 20, 15);

        Assert.Equal(Direction.Down, result);
    }

    [Fact]
    public void GetExitDirection_TargetYExceedsMapHeight_ReturnsDown()
    {
        var result = WorldLayoutHelper.GetExitDirection(5, 20, 20, 15);

        Assert.Equal(Direction.Down, result);
    }

    [Fact]
    public void GetExitDirection_TargetXNegative_ReturnsLeft()
    {
        var result = WorldLayoutHelper.GetExitDirection(-1, 5, 20, 15);

        Assert.Equal(Direction.Left, result);
    }

    [Fact]
    public void GetExitDirection_TargetXEqualsMapWidth_ReturnsRight()
    {
        var result = WorldLayoutHelper.GetExitDirection(20, 5, 20, 15);

        Assert.Equal(Direction.Right, result);
    }

    [Fact]
    public void GetExitDirection_TargetXExceedsMapWidth_ReturnsRight()
    {
        var result = WorldLayoutHelper.GetExitDirection(25, 5, 20, 15);

        Assert.Equal(Direction.Right, result);
    }

    [Fact]
    public void GetExitDirection_InBoundsPosition_ReturnsNull()
    {
        var result = WorldLayoutHelper.GetExitDirection(5, 5, 20, 15);

        Assert.Null(result);
    }

    [Fact]
    public void GetExitDirection_VerticalExitCheckedBeforeHorizontal_NegativeYTakesPrecedence()
    {
        // targetY < 0 and targetX < 0 simultaneously — Up takes priority per implementation order
        var result = WorldLayoutHelper.GetExitDirection(-1, -1, 20, 15);

        Assert.Equal(Direction.Up, result);
    }

    // -------------------------------------------------------------------------
    // GetNeighbor
    // -------------------------------------------------------------------------

    [Fact]
    public void GetNeighbor_EastNeighborExists_ReturnsNeighborName()
    {
        var layout = TwoMapLayout("A", 0, 0, "B", 1, 0);

        var result = WorldLayoutHelper.GetNeighbor(layout, "A", Direction.Right);

        Assert.Equal("B", result);
    }

    [Fact]
    public void GetNeighbor_WestNeighborExists_ReturnsNeighborName()
    {
        var layout = TwoMapLayout("A", 0, 0, "B", -1, 0);

        var result = WorldLayoutHelper.GetNeighbor(layout, "A", Direction.Left);

        Assert.Equal("B", result);
    }

    [Fact]
    public void GetNeighbor_NorthNeighborExists_ReturnsNeighborName()
    {
        var layout = TwoMapLayout("A", 0, 0, "B", 0, -1);

        var result = WorldLayoutHelper.GetNeighbor(layout, "A", Direction.Up);

        Assert.Equal("B", result);
    }

    [Fact]
    public void GetNeighbor_SouthNeighborExists_ReturnsNeighborName()
    {
        var layout = TwoMapLayout("A", 0, 0, "B", 0, 1);

        var result = WorldLayoutHelper.GetNeighbor(layout, "A", Direction.Down);

        Assert.Equal("B", result);
    }

    [Fact]
    public void GetNeighbor_NoNeighborInDirection_ReturnsNull()
    {
        var layout = TwoMapLayout("A", 0, 0, "B", 1, 0);

        var result = WorldLayoutHelper.GetNeighbor(layout, "A", Direction.Up);

        Assert.Null(result);
    }

    [Fact]
    public void GetNeighbor_NullLayout_ReturnsNull()
    {
        var result = WorldLayoutHelper.GetNeighbor(null, "A", Direction.Right);

        Assert.Null(result);
    }

    [Fact]
    public void GetNeighbor_MapNotInLayout_ReturnsNull()
    {
        var layout = TwoMapLayout("A", 0, 0, "B", 1, 0);

        var result = WorldLayoutHelper.GetNeighbor(layout, "Missing", Direction.Right);

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // ComputeSpawnPosition
    // -------------------------------------------------------------------------

    [Fact]
    public void ComputeSpawnPosition_ExitRight_SpawnsAtWestEdgeSameY()
    {
        var result = WorldLayoutHelper.ComputeSpawnPosition(
            Direction.Right, 20, 15, 5, 7, 20, 15, null);

        Assert.Equal(0, result.X);
        Assert.Equal(7, result.Y);
    }

    [Fact]
    public void ComputeSpawnPosition_ExitLeft_SpawnsAtEastEdgeSameY()
    {
        var result = WorldLayoutHelper.ComputeSpawnPosition(
            Direction.Left, 20, 15, 0, 7, 20, 15, null);

        Assert.Equal(19, result.X);
        Assert.Equal(7, result.Y);
    }

    [Fact]
    public void ComputeSpawnPosition_ExitDown_SpawnsAtNorthEdgeSameX()
    {
        var result = WorldLayoutHelper.ComputeSpawnPosition(
            Direction.Down, 20, 15, 5, 14, 20, 15, null);

        Assert.Equal(5, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void ComputeSpawnPosition_ExitUp_SpawnsAtSouthEdgeSameX()
    {
        var result = WorldLayoutHelper.ComputeSpawnPosition(
            Direction.Up, 20, 15, 5, 0, 20, 15, null);

        Assert.Equal(5, result.X);
        Assert.Equal(14, result.Y);
    }

    [Fact]
    public void ComputeSpawnPosition_ExitRight_YClampedWhenTargetMapIsShorter()
    {
        // Source map height 15, player at Y=12, target map height 10 — Y must clamp to 9
        var result = WorldLayoutHelper.ComputeSpawnPosition(
            Direction.Right, 20, 15, 5, 12, 20, 10, null);

        Assert.Equal(0, result.X);
        Assert.Equal(9, result.Y);
    }

    [Fact]
    public void ComputeSpawnPosition_ExitDown_XClampedWhenTargetMapIsNarrower()
    {
        // Source map width 20, player at X=18, target map width 10 — X must clamp to 9
        var result = WorldLayoutHelper.ComputeSpawnPosition(
            Direction.Down, 20, 15, 18, 14, 10, 15, null);

        Assert.Equal(9, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void ComputeSpawnPosition_WithEdgeSpawnOverride_ReturnsOverrideCoordinates()
    {
        var entryOverride = new EdgeSpawn { X = 3, Y = 8 };

        var result = WorldLayoutHelper.ComputeSpawnPosition(
            Direction.Right, 20, 15, 5, 7, 20, 15, entryOverride);

        Assert.Equal(3, result.X);
        Assert.Equal(8, result.Y);
    }

    // -------------------------------------------------------------------------
    // GetEntrySpawn
    // -------------------------------------------------------------------------

    [Fact]
    public void GetEntrySpawn_ExitRight_ReturnsWestEntry()
    {
        var placement = new MapPlacement
        {
            WestEntry = new EdgeSpawn { X = 0, Y = 5 }
        };

        var result = WorldLayoutHelper.GetEntrySpawn(placement, Direction.Right);

        Assert.Equal(placement.WestEntry, result);
    }

    [Fact]
    public void GetEntrySpawn_ExitLeft_ReturnsEastEntry()
    {
        var placement = new MapPlacement
        {
            EastEntry = new EdgeSpawn { X = 19, Y = 5 }
        };

        var result = WorldLayoutHelper.GetEntrySpawn(placement, Direction.Left);

        Assert.Equal(placement.EastEntry, result);
    }

    [Fact]
    public void GetEntrySpawn_ExitDown_ReturnsNorthEntry()
    {
        var placement = new MapPlacement
        {
            NorthEntry = new EdgeSpawn { X = 5, Y = 0 }
        };

        var result = WorldLayoutHelper.GetEntrySpawn(placement, Direction.Down);

        Assert.Equal(placement.NorthEntry, result);
    }

    [Fact]
    public void GetEntrySpawn_ExitUp_ReturnsSouthEntry()
    {
        var placement = new MapPlacement
        {
            SouthEntry = new EdgeSpawn { X = 5, Y = 14 }
        };

        var result = WorldLayoutHelper.GetEntrySpawn(placement, Direction.Up);

        Assert.Equal(placement.SouthEntry, result);
    }

    [Fact]
    public void GetEntrySpawn_NullPlacement_ReturnsNull()
    {
        var result = WorldLayoutHelper.GetEntrySpawn(null, Direction.Right);

        Assert.Null(result);
    }

    [Fact]
    public void GetEntrySpawn_EntryNotSet_ReturnsNull()
    {
        var placement = new MapPlacement(); // all entries null

        var result = WorldLayoutHelper.GetEntrySpawn(placement, Direction.Right);

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // IsCellOccupied / GetMapAtCell
    // -------------------------------------------------------------------------

    [Fact]
    public void IsCellOccupied_OccupiedCell_ReturnsTrue()
    {
        var layout = new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                ["Forest"] = new MapPlacement { GridX = 2, GridY = 3 }
            }
        };

        Assert.True(WorldLayoutHelper.IsCellOccupied(layout, 2, 3));
    }

    [Fact]
    public void IsCellOccupied_EmptyCell_ReturnsFalse()
    {
        var layout = new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                ["Forest"] = new MapPlacement { GridX = 2, GridY = 3 }
            }
        };

        Assert.False(WorldLayoutHelper.IsCellOccupied(layout, 0, 0));
    }

    [Fact]
    public void GetMapAtCell_OccupiedCell_ReturnsMapName()
    {
        var layout = new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                ["Desert"] = new MapPlacement { GridX = 1, GridY = 2 }
            }
        };

        var result = WorldLayoutHelper.GetMapAtCell(layout, 1, 2);

        Assert.Equal("Desert", result);
    }

    [Fact]
    public void GetMapAtCell_EmptyCell_ReturnsNull()
    {
        var layout = new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                ["Desert"] = new MapPlacement { GridX = 1, GridY = 2 }
            }
        };

        var result = WorldLayoutHelper.GetMapAtCell(layout, 0, 0);

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // GetUnplacedMaps
    // -------------------------------------------------------------------------

    [Fact]
    public void GetUnplacedMaps_SomeMapsPlaced_ReturnsUnplacedOnly()
    {
        var layout = new WorldLayout
        {
            Maps = new Dictionary<string, MapPlacement>
            {
                ["A"] = new MapPlacement { GridX = 0, GridY = 0 }
            }
        };
        var allMaps = new[] { "A", "B", "C" };

        var result = WorldLayoutHelper.GetUnplacedMaps(layout, allMaps);

        Assert.Equal(2, result.Count);
        Assert.Contains("B", result);
        Assert.Contains("C", result);
        Assert.DoesNotContain("A", result);
    }

    [Fact]
    public void GetUnplacedMaps_AllMapsPlaced_ReturnsEmptyList()
    {
        var layout = TwoMapLayout("A", 0, 0, "B", 1, 0);
        var allMaps = new[] { "A", "B" };

        var result = WorldLayoutHelper.GetUnplacedMaps(layout, allMaps);

        Assert.Empty(result);
    }

    [Fact]
    public void GetUnplacedMaps_NoMapsPlaced_ReturnsAllMaps()
    {
        var layout = new WorldLayout(); // empty Maps dict
        var allMaps = new[] { "X", "Y", "Z" };

        var result = WorldLayoutHelper.GetUnplacedMaps(layout, allMaps);

        Assert.Equal(3, result.Count);
        Assert.Contains("X", result);
        Assert.Contains("Y", result);
        Assert.Contains("Z", result);
    }

    // -------------------------------------------------------------------------
    // GetPlacedMaps
    // -------------------------------------------------------------------------

    [Fact]
    public void GetPlacedMaps_TwoPlacedMaps_ReturnsBothTuples()
    {
        var layout = TwoMapLayout("A", 0, 0, "B", 1, 0);

        var result = WorldLayoutHelper.GetPlacedMaps(layout);

        Assert.Equal(2, result.Count);
        Assert.Contains(("A", 0, 0), result);
        Assert.Contains(("B", 1, 0), result);
    }

    [Fact]
    public void GetPlacedMaps_EmptyLayout_ReturnsEmptyList()
    {
        var layout = new WorldLayout();

        var result = WorldLayoutHelper.GetPlacedMaps(layout);

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // Auto-bidirectional adjacency verification
    // -------------------------------------------------------------------------

    [Fact]
    public void Bidirectional_EastWest_AEastNeighborIsB_AndBWestNeighborIsA()
    {
        // A at (0,0), B at (1,0) — they share an east/west border
        var layout = TwoMapLayout("A", 0, 0, "B", 1, 0);

        var aEast = WorldLayoutHelper.GetNeighbor(layout, "A", Direction.Right);
        var bWest = WorldLayoutHelper.GetNeighbor(layout, "B", Direction.Left);

        Assert.Equal("B", aEast);
        Assert.Equal("A", bWest);
    }

    [Fact]
    public void Bidirectional_NorthSouth_ANorthNeighborIsB_AndBSouthNeighborIsA()
    {
        // A at (0,0), B at (0,-1) — B is directly above A
        var layout = TwoMapLayout("A", 0, 0, "B", 0, -1);

        var aNorth = WorldLayoutHelper.GetNeighbor(layout, "A", Direction.Up);
        var bSouth = WorldLayoutHelper.GetNeighbor(layout, "B", Direction.Down);

        Assert.Equal("B", aNorth);
        Assert.Equal("A", bSouth);
    }

    // -------------------------------------------------------------------------
    // GetExitPoint
    // -------------------------------------------------------------------------

    [Fact]
    public void GetExitPoint_NullPlacement_ReturnsNull()
    {
        var result = WorldLayoutHelper.GetExitPoint(null, Direction.Right);

        Assert.Null(result);
    }

    [Fact]
    public void GetExitPoint_DirectionUp_ReturnsNorthExit()
    {
        var placement = new MapPlacement
        {
            NorthExit = new EdgeSpawn { X = 7, Y = 0 }
        };

        var result = WorldLayoutHelper.GetExitPoint(placement, Direction.Up);

        Assert.NotNull(result);
        Assert.Equal(7, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void GetExitPoint_DirectionDown_ReturnsSouthExit()
    {
        var placement = new MapPlacement
        {
            SouthExit = new EdgeSpawn { X = 3, Y = 14 }
        };

        var result = WorldLayoutHelper.GetExitPoint(placement, Direction.Down);

        Assert.NotNull(result);
        Assert.Equal(3, result.X);
        Assert.Equal(14, result.Y);
    }

    [Fact]
    public void GetExitPoint_DirectionLeft_ReturnsWestExit()
    {
        var placement = new MapPlacement
        {
            WestExit = new EdgeSpawn { X = 0, Y = 5 }
        };

        var result = WorldLayoutHelper.GetExitPoint(placement, Direction.Left);

        Assert.NotNull(result);
        Assert.Equal(0, result.X);
        Assert.Equal(5, result.Y);
    }

    [Fact]
    public void GetExitPoint_DirectionRight_ReturnsEastExit()
    {
        var placement = new MapPlacement
        {
            EastExit = new EdgeSpawn { X = 19, Y = 8 }
        };

        var result = WorldLayoutHelper.GetExitPoint(placement, Direction.Right);

        Assert.NotNull(result);
        Assert.Equal(19, result.X);
        Assert.Equal(8, result.Y);
    }

    [Fact]
    public void GetExitPoint_ExitNotSetForDirection_ReturnsNull()
    {
        // Only EastExit is set; querying Up should return null
        var placement = new MapPlacement
        {
            EastExit = new EdgeSpawn { X = 19, Y = 5 }
        };

        var result = WorldLayoutHelper.GetExitPoint(placement, Direction.Up);

        Assert.Null(result);
    }
}
