using TileForge.Data;
using Xunit;

namespace TileForge.Tests.Data;

public class MapDataTests
{
    [Fact]
    public void Constructor_GivenWidthAndHeight_SetsPropertiesCorrectly()
    {
        var map = new MapData(20, 15);

        Assert.Equal(20, map.Width);
        Assert.Equal(15, map.Height);
    }

    [Fact]
    public void Constructor_CreatesDefaultLayers_GroundAndObjects()
    {
        var map = new MapData(10, 10);

        Assert.Equal(2, map.Layers.Count);
        Assert.Equal("Ground", map.Layers[0].Name);
        Assert.Equal("Objects", map.Layers[1].Name);
    }

    [Fact]
    public void Constructor_DefaultLayers_HaveCorrectDimensions()
    {
        var map = new MapData(8, 6);

        foreach (var layer in map.Layers)
        {
            Assert.Equal(8 * 6, layer.Cells.Length);
        }
    }

    [Fact]
    public void Constructor_SetsEntityRenderOrderToZero()
    {
        var map = new MapData(10, 10);

        Assert.Equal(0, map.EntityRenderOrder);
    }

    [Fact]
    public void Constructor_CreatesEmptyEntitiesList()
    {
        var map = new MapData(10, 10);

        Assert.NotNull(map.Entities);
        Assert.Empty(map.Entities);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(9, 9, true)]
    [InlineData(5, 5, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(10, 0, false)]
    [InlineData(0, 10, false)]
    [InlineData(10, 10, false)]
    [InlineData(-1, -1, false)]
    public void InBounds_ReturnsCorrectResult(int x, int y, bool expected)
    {
        var map = new MapData(10, 10);

        Assert.Equal(expected, map.InBounds(x, y));
    }

    [Fact]
    public void GetLayer_ExistingLayer_ReturnsLayer()
    {
        var map = new MapData(10, 10);

        var ground = map.GetLayer("Ground");

        Assert.NotNull(ground);
        Assert.Equal("Ground", ground.Name);
    }

    [Fact]
    public void GetLayer_NonExistingLayer_ReturnsNull()
    {
        var map = new MapData(10, 10);

        var result = map.GetLayer("NonExistent");

        Assert.Null(result);
    }

    [Fact]
    public void GetLayer_ObjectsLayer_ReturnsCorrectLayer()
    {
        var map = new MapData(10, 10);

        var objects = map.GetLayer("Objects");

        Assert.NotNull(objects);
        Assert.Equal("Objects", objects.Name);
    }

    [Fact]
    public void HasLayer_ExistingLayer_ReturnsTrue()
    {
        var map = new MapData(10, 10);

        Assert.True(map.HasLayer("Ground"));
        Assert.True(map.HasLayer("Objects"));
    }

    [Fact]
    public void HasLayer_NonExistingLayer_ReturnsFalse()
    {
        var map = new MapData(10, 10);

        Assert.False(map.HasLayer("NonExistent"));
    }

    [Fact]
    public void AddLayer_CreatesNewLayerWithCorrectName()
    {
        var map = new MapData(10, 10);

        var layer = map.AddLayer("Overlay");

        Assert.Equal("Overlay", layer.Name);
    }

    [Fact]
    public void AddLayer_CreatesLayerWithCorrectDimensions()
    {
        var map = new MapData(8, 6);

        var layer = map.AddLayer("Overlay");

        Assert.Equal(8 * 6, layer.Cells.Length);
    }

    [Fact]
    public void AddLayer_AddsToLayersList()
    {
        var map = new MapData(10, 10);
        int initialCount = map.Layers.Count;

        map.AddLayer("Overlay");

        Assert.Equal(initialCount + 1, map.Layers.Count);
    }

    [Fact]
    public void AddLayer_NewLayerIsAccessibleByGetLayer()
    {
        var map = new MapData(10, 10);

        map.AddLayer("Overlay");

        Assert.NotNull(map.GetLayer("Overlay"));
        Assert.True(map.HasLayer("Overlay"));
    }

    // --- Resize Tests ---

    [Fact]
    public void Resize_ToLarger_PreservesExistingCellsAtOriginalPositions()
    {
        var map = new MapData(3, 3);
        var ground = map.GetLayer("Ground");
        ground.SetCell(0, 0, 3, "grass");
        ground.SetCell(1, 1, 3, "wall");
        ground.SetCell(2, 2, 3, "water");

        map.Resize(5, 5);

        Assert.Equal("grass", ground.GetCell(0, 0, 5));
        Assert.Equal("wall", ground.GetCell(1, 1, 5));
        Assert.Equal("water", ground.GetCell(2, 2, 5));
    }

    [Fact]
    public void Resize_ToLarger_FillsNewCellsWithNull()
    {
        var map = new MapData(2, 2);

        map.Resize(4, 4);

        var ground = map.GetLayer("Ground");
        // New cells beyond original 2x2 region should be null
        Assert.Null(ground.GetCell(3, 0, 4));
        Assert.Null(ground.GetCell(0, 3, 4));
        Assert.Null(ground.GetCell(3, 3, 4));
    }

    [Fact]
    public void Resize_ToSmaller_CropsCellsBeyondNewBounds()
    {
        var map = new MapData(5, 5);
        var ground = map.GetLayer("Ground");
        ground.SetCell(0, 0, 5, "grass");
        ground.SetCell(4, 4, 5, "wall");

        map.Resize(3, 3);

        Assert.Equal("grass", ground.GetCell(0, 0, 3));
        Assert.Equal(3 * 3, ground.Cells.Length);
    }

    [Fact]
    public void Resize_ToSameDimensions_IsNoOp()
    {
        var map = new MapData(5, 5);
        var ground = map.GetLayer("Ground");
        ground.SetCell(2, 2, 5, "grass");

        map.Resize(5, 5);

        Assert.Equal(5, map.Width);
        Assert.Equal(5, map.Height);
        Assert.Equal("grass", ground.GetCell(2, 2, 5));
        Assert.Equal(25, ground.Cells.Length);
    }

    [Fact]
    public void Resize_To1x1_KeepsOnlyCellZeroZero()
    {
        var map = new MapData(3, 3);
        var ground = map.GetLayer("Ground");
        ground.SetCell(0, 0, 3, "grass");
        ground.SetCell(1, 0, 3, "wall");
        ground.SetCell(0, 1, 3, "water");

        map.Resize(1, 1);

        Assert.Single(ground.Cells);
        Assert.Equal("grass", ground.GetCell(0, 0, 1));
    }

    [Fact]
    public void Resize_PreservesDataAcrossMultipleLayers()
    {
        var map = new MapData(3, 3);
        var ground = map.GetLayer("Ground");
        var objects = map.GetLayer("Objects");
        ground.SetCell(1, 1, 3, "grass");
        objects.SetCell(1, 1, 3, "tree");

        map.Resize(5, 5);

        Assert.Equal("grass", ground.GetCell(1, 1, 5));
        Assert.Equal("tree", objects.GetCell(1, 1, 5));
    }

    [Fact]
    public void Resize_RemovesOutOfBoundsEntities_ReturnsThemInList()
    {
        var map = new MapData(5, 5);
        var entity = new Entity { GroupName = "chest", X = 4, Y = 4 };
        map.Entities.Add(entity);

        var removed = map.Resize(3, 3);

        Assert.Single(removed);
        Assert.Equal(entity.Id, removed[0].Id);
        Assert.DoesNotContain(entity, map.Entities);
    }

    [Fact]
    public void Resize_KeepsInBoundsEntities()
    {
        var map = new MapData(5, 5);
        var inBounds = new Entity { GroupName = "chest", X = 1, Y = 1 };
        var outBounds = new Entity { GroupName = "door", X = 4, Y = 4 };
        map.Entities.Add(inBounds);
        map.Entities.Add(outBounds);

        map.Resize(3, 3);

        Assert.Single(map.Entities);
        Assert.Equal(inBounds.Id, map.Entities[0].Id);
    }

    [Fact]
    public void Resize_UpdatesWidthAndHeightProperties()
    {
        var map = new MapData(5, 5);

        map.Resize(8, 12);

        Assert.Equal(8, map.Width);
        Assert.Equal(12, map.Height);
    }

    [Fact]
    public void Resize_ClampsMinimumTo1x1()
    {
        var map = new MapData(5, 5);
        var ground = map.GetLayer("Ground");
        ground.SetCell(0, 0, 5, "grass");

        map.Resize(0, -5);

        Assert.Equal(1, map.Width);
        Assert.Equal(1, map.Height);
        Assert.Single(ground.Cells);
        Assert.Equal("grass", ground.GetCell(0, 0, 1));
    }
}
