using System.Collections.Generic;
using Xunit;
using TileForge.Game;
using TileForge.Data;

namespace TileForge.Tests.Game;

public class SimplePathfinderTests
{
    // ==================== Helpers ====================

    private static LoadedMap CreateMap(int width, int height, bool[,] solidMask = null)
    {
        var cells = new string[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                cells[x + y * width] = solidMask != null && solidMask[x, y] ? "wall" : "floor";

        return new LoadedMap
        {
            Width = width,
            Height = height,
            Layers = new() { new LoadedMapLayer { Name = "base", Cells = cells } },
        };
    }

    private static IReadOnlyDictionary<string, TileGroup> DefaultGroups() =>
        new Dictionary<string, TileGroup>
        {
            ["floor"] = new TileGroup { Name = "floor", IsSolid = false },
            ["wall"]  = new TileGroup { Name = "wall",  IsSolid = true  },
        };

    private static SimplePathfinder MakePathfinder(
        LoadedMap map,
        IReadOnlyDictionary<string, TileGroup> groups = null,
        IReadOnlyList<EntityInstance> entities = null,
        PlayerState player = null)
    {
        return new SimplePathfinder(
            map,
            groups ?? DefaultGroups(),
            entities ?? new List<EntityInstance>(),
            player);
    }

    // ==================== GetNextStep ====================

    [Fact]
    public void GetNextStep_HorizontalPath_StepsAlongX()
    {
        var map = CreateMap(5, 5);
        var pf = MakePathfinder(map);

        var step = pf.GetNextStep(0, 0, 3, 0);

        Assert.Equal((1, 0), step);
    }

    [Fact]
    public void GetNextStep_VerticalPath_StepsAlongY()
    {
        var map = CreateMap(5, 5);
        var pf = MakePathfinder(map);

        var step = pf.GetNextStep(0, 0, 0, 3);

        Assert.Equal((0, 1), step);
    }

    [Fact]
    public void GetNextStep_DiagonalEqualDistance_PrefersX()
    {
        // dx == dy == 3, X should be primary (ties go to X)
        var map = CreateMap(5, 5);
        var pf = MakePathfinder(map);

        var step = pf.GetNextStep(0, 0, 3, 3);

        Assert.Equal((1, 0), step);
    }

    [Fact]
    public void GetNextStep_DiagonalLargerDy_PrefersY()
    {
        // dx = 1, dy = 3 → Y is primary
        var map = CreateMap(5, 5);
        var pf = MakePathfinder(map);

        var step = pf.GetNextStep(0, 0, 1, 3);

        Assert.Equal((0, 1), step);
    }

    [Fact]
    public void GetNextStep_AtTarget_ReturnsNull()
    {
        var map = CreateMap(5, 5);
        var pf = MakePathfinder(map);

        var step = pf.GetNextStep(2, 2, 2, 2);

        Assert.Null(step);
    }

    [Fact]
    public void GetNextStep_AdjacentToTarget_ReturnsTarget()
    {
        var map = CreateMap(5, 5);
        var pf = MakePathfinder(map);

        var step = pf.GetNextStep(1, 0, 2, 0);

        Assert.Equal((2, 0), step);
    }

    [Fact]
    public void GetNextStep_PrimaryAxisBlockedByWall_FallsBackToSecondary()
    {
        // Wall at (1,0); target at (3,1) — primary axis X blocked, should try Y
        var solid = new bool[5, 5];
        solid[1, 0] = true;
        var map = CreateMap(5, 5, solid);
        var pf = MakePathfinder(map);

        // From (0,0) to (3,1): dx=3, dy=1 → X primary; X blocked → fall back to Y
        var step = pf.GetNextStep(0, 0, 3, 1);

        Assert.Equal((0, 1), step);
    }

    [Fact]
    public void GetNextStep_BothAxesBlockedByWalls_ReturnsNull()
    {
        var solid = new bool[5, 5];
        solid[1, 0] = true; // wall to the right
        solid[0, 1] = true; // wall below
        var map = CreateMap(5, 5, solid);
        var pf = MakePathfinder(map);

        var step = pf.GetNextStep(0, 0, 3, 3);

        Assert.Null(step);
    }

    [Fact]
    public void GetNextStep_PrimaryAxisStraightAheadBlocked_ReturnsNull()
    {
        // Target is directly right, but wall is in the way and no Y movement possible
        var solid = new bool[5, 5];
        solid[1, 0] = true;
        var map = CreateMap(5, 5, solid);
        var pf = MakePathfinder(map);

        // From (0,0) to (3,0): dx=3, dy=0 → X only; X blocked → Y sign=0 → null
        var step = pf.GetNextStep(0, 0, 3, 0);

        Assert.Null(step);
    }

    [Fact]
    public void GetNextStep_ActiveEntityBlocksPrimaryAxis_FallsBackToSecondary()
    {
        var map = CreateMap(5, 5);
        var blocker = new EntityInstance { Id = "e1", X = 1, Y = 0, IsActive = true };
        var pf = MakePathfinder(map, entities: new List<EntityInstance> { blocker });

        // From (0,0) to (3,1): primary X blocked by entity at (1,0), secondary Y should work
        var step = pf.GetNextStep(0, 0, 3, 1);

        Assert.Equal((0, 1), step);
    }

    [Fact]
    public void GetNextStep_InactiveEntityDoesNotBlock()
    {
        var map = CreateMap(5, 5);
        var blocker = new EntityInstance { Id = "e1", X = 1, Y = 0, IsActive = false };
        var pf = MakePathfinder(map, entities: new List<EntityInstance> { blocker });

        var step = pf.GetNextStep(0, 0, 3, 0);

        Assert.Equal((1, 0), step);
    }

    [Fact]
    public void GetNextStep_PlayerBlocksPrimaryAxis_FallsBackToSecondary()
    {
        var map = CreateMap(5, 5);
        var player = new PlayerState { X = 1, Y = 0 };
        var pf = MakePathfinder(map, player: player);

        // From (0,0) to (3,1): primary X blocked by player at (1,0)
        var step = pf.GetNextStep(0, 0, 3, 1);

        Assert.Equal((0, 1), step);
    }

    [Fact]
    public void GetNextStep_EntityDoesNotBlockItself()
    {
        // The entity at (0,0) is moving; it should not count itself as a blocker
        var map = CreateMap(5, 5);
        var self = new EntityInstance { Id = "self", X = 0, Y = 0, IsActive = true };
        var pf = MakePathfinder(map, entities: new List<EntityInstance> { self });

        // Step from (0,0) toward (3,0); self-entity is at (0,0) — should not block (1,0)
        var step = pf.GetNextStep(0, 0, 3, 0);

        Assert.Equal((1, 0), step);
    }

    [Fact]
    public void GetNextStep_OutOfBoundsTarget_HandledGracefully()
    {
        // The caller is at (0,0), target is at (-5, -5) — movement goes negative; out of bounds
        var map = CreateMap(5, 5);
        var pf = MakePathfinder(map);

        var step = pf.GetNextStep(0, 0, -5, -5);

        // Both axes out of bounds → null
        Assert.Null(step);
    }

    [Fact]
    public void GetNextStep_NullPlayer_DoesNotThrow()
    {
        var map = CreateMap(5, 5);
        var pf = MakePathfinder(map, player: null);

        var step = pf.GetNextStep(0, 0, 3, 0);

        Assert.Equal((1, 0), step);
    }

    // ==================== HasLineOfSight ====================

    [Fact]
    public void HasLineOfSight_SameTile_ReturnsTrue()
    {
        var map = CreateMap(5, 5);
        var pf = MakePathfinder(map);

        Assert.True(pf.HasLineOfSight(2, 2, 2, 2));
    }

    [Fact]
    public void HasLineOfSight_AdjacentTiles_ReturnsTrue()
    {
        var map = CreateMap(5, 5);
        var pf = MakePathfinder(map);

        Assert.True(pf.HasLineOfSight(1, 1, 2, 1));
        Assert.True(pf.HasLineOfSight(1, 1, 1, 2));
    }

    [Fact]
    public void HasLineOfSight_ClearHorizontalLine_ReturnsTrue()
    {
        var map = CreateMap(10, 10);
        var pf = MakePathfinder(map);

        Assert.True(pf.HasLineOfSight(0, 5, 9, 5));
    }

    [Fact]
    public void HasLineOfSight_ClearVerticalLine_ReturnsTrue()
    {
        var map = CreateMap(10, 10);
        var pf = MakePathfinder(map);

        Assert.True(pf.HasLineOfSight(5, 0, 5, 9));
    }

    [Fact]
    public void HasLineOfSight_WallInMiddle_ReturnsFalse()
    {
        // Wall at (5,5), line from (0,5) to (9,5)
        var solid = new bool[10, 10];
        solid[5, 5] = true;
        var map = CreateMap(10, 10, solid);
        var pf = MakePathfinder(map);

        Assert.False(pf.HasLineOfSight(0, 5, 9, 5));
    }

    [Fact]
    public void HasLineOfSight_WallAtStartTile_ReturnsTrue()
    {
        // The start tile itself is a wall — but we don't check start, so LOS is clear
        var solid = new bool[5, 5];
        solid[0, 0] = true;
        var map = CreateMap(5, 5, solid);
        var pf = MakePathfinder(map);

        Assert.True(pf.HasLineOfSight(0, 0, 4, 0));
    }

    [Fact]
    public void HasLineOfSight_WallAtEndTile_ReturnsTrue()
    {
        // The end tile is a wall — but we don't check end tile, so LOS is clear
        var solid = new bool[5, 5];
        solid[4, 0] = true;
        var map = CreateMap(5, 5, solid);
        var pf = MakePathfinder(map);

        Assert.True(pf.HasLineOfSight(0, 0, 4, 0));
    }

    [Fact]
    public void HasLineOfSight_DiagonalClearLine_ReturnsTrue()
    {
        var map = CreateMap(10, 10);
        var pf = MakePathfinder(map);

        Assert.True(pf.HasLineOfSight(0, 0, 9, 9));
    }

    [Fact]
    public void HasLineOfSight_LongHorizontalLineNoObstruction_ReturnsTrue()
    {
        var map = CreateMap(12, 5);
        var pf = MakePathfinder(map);

        Assert.True(pf.HasLineOfSight(0, 2, 11, 2));
    }

    [Fact]
    public void HasLineOfSight_WallBlocksDiagonalLine_ReturnsFalse()
    {
        // Diagonal from (0,0) to (4,4); wall at (2,2) blocks it
        var solid = new bool[5, 5];
        solid[2, 2] = true;
        var map = CreateMap(5, 5, solid);
        var pf = MakePathfinder(map);

        Assert.False(pf.HasLineOfSight(0, 0, 4, 4));
    }

    [Fact]
    public void HasLineOfSight_WallBesideLine_DoesNotBlock()
    {
        // Wall at (5,6), line goes horizontally along y=5; wall is on adjacent row
        var solid = new bool[10, 10];
        solid[5, 6] = true;
        var map = CreateMap(10, 10, solid);
        var pf = MakePathfinder(map);

        Assert.True(pf.HasLineOfSight(0, 5, 9, 5));
    }
}
