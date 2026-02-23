using TileForge.Data;
using TileForge.Editor.Commands;
using Xunit;

namespace TileForge.Tests.Editor.Commands;

public class ResizeMapCommandTests
{
    private static MapData CreatePopulatedMap(int width = 5, int height = 5)
    {
        var map = new MapData(width, height);
        var ground = map.GetLayer("Ground");
        ground.SetCell(0, 0, width, "grass");
        ground.SetCell(1, 0, width, "wall");
        ground.SetCell(0, 1, width, "water");
        ground.SetCell(1, 1, width, "sand");
        return map;
    }

    [Fact]
    public void Execute_ResizesMapToNewDimensions()
    {
        var map = CreatePopulatedMap();
        var cmd = new ResizeMapCommand(map, 8, 10);

        cmd.Execute();

        Assert.Equal(8, map.Width);
        Assert.Equal(10, map.Height);
    }

    [Fact]
    public void Execute_PreservesExistingCellData()
    {
        var map = CreatePopulatedMap();
        var cmd = new ResizeMapCommand(map, 8, 10);

        cmd.Execute();

        var ground = map.GetLayer("Ground");
        Assert.Equal("grass", ground.GetCell(0, 0, 8));
        Assert.Equal("wall", ground.GetCell(1, 0, 8));
        Assert.Equal("water", ground.GetCell(0, 1, 8));
        Assert.Equal("sand", ground.GetCell(1, 1, 8));
    }

    [Fact]
    public void Undo_RestoresOriginalDimensions()
    {
        var map = CreatePopulatedMap(5, 5);
        var cmd = new ResizeMapCommand(map, 8, 10);

        cmd.Execute();
        cmd.Undo();

        Assert.Equal(5, map.Width);
        Assert.Equal(5, map.Height);
    }

    [Fact]
    public void Undo_RestoresOriginalCellDataExactly()
    {
        var map = CreatePopulatedMap(5, 5);
        var ground = map.GetLayer("Ground");

        // Snapshot original cells for comparison
        var originalCells = new string[ground.Cells.Length];
        ground.Cells.CopyTo(originalCells, 0);

        var cmd = new ResizeMapCommand(map, 8, 10);
        cmd.Execute();
        cmd.Undo();

        Assert.Equal(originalCells.Length, ground.Cells.Length);
        for (int i = 0; i < originalCells.Length; i++)
        {
            Assert.Equal(originalCells[i], ground.Cells[i]);
        }
    }

    [Fact]
    public void Undo_RestoresRemovedEntities()
    {
        var map = CreatePopulatedMap(5, 5);
        var entity = new Entity { GroupName = "chest", X = 4, Y = 4 };
        map.Entities.Add(entity);

        var cmd = new ResizeMapCommand(map, 3, 3);
        cmd.Execute();

        // Entity at (4,4) should have been removed
        Assert.Empty(map.Entities);

        cmd.Undo();

        // Entity should be restored
        Assert.Single(map.Entities);
        Assert.Equal(entity.Id, map.Entities[0].Id);
    }

    [Fact]
    public void ExecuteUndo_RoundtripIsLossless()
    {
        var map = CreatePopulatedMap(5, 5);
        var ground = map.GetLayer("Ground");
        var objects = map.GetLayer("Objects");
        objects.SetCell(2, 2, 5, "tree");

        var entity = new Entity { GroupName = "npc", X = 3, Y = 3 };
        map.Entities.Add(entity);

        // Snapshot everything
        var origWidth = map.Width;
        var origHeight = map.Height;
        var origGroundCells = (string[])ground.Cells.Clone();
        var origObjectsCells = (string[])objects.Cells.Clone();
        var origEntityCount = map.Entities.Count;
        var origEntityId = entity.Id;

        var cmd = new ResizeMapCommand(map, 2, 2);
        cmd.Execute();
        cmd.Undo();

        Assert.Equal(origWidth, map.Width);
        Assert.Equal(origHeight, map.Height);
        Assert.Equal(origGroundCells, ground.Cells);
        Assert.Equal(origObjectsCells, objects.Cells);
        Assert.Equal(origEntityCount, map.Entities.Count);
        Assert.Equal(origEntityId, map.Entities[0].Id);
    }

    [Fact]
    public void ExecuteUndoExecute_ProducesSameResultAsSingleExecute()
    {
        // First, do a single execute on a fresh map to capture expected state
        var map1 = CreatePopulatedMap(5, 5);
        var cmd1 = new ResizeMapCommand(map1, 3, 3);
        cmd1.Execute();

        var expectedWidth = map1.Width;
        var expectedHeight = map1.Height;
        var expectedGroundCells = (string[])map1.GetLayer("Ground").Cells.Clone();

        // Now do execute, undo, execute on an identical map
        var map2 = CreatePopulatedMap(5, 5);
        var cmd2 = new ResizeMapCommand(map2, 3, 3);
        cmd2.Execute();
        cmd2.Undo();
        cmd2.Execute();

        Assert.Equal(expectedWidth, map2.Width);
        Assert.Equal(expectedHeight, map2.Height);
        Assert.Equal(expectedGroundCells, map2.GetLayer("Ground").Cells);
    }

    [Fact]
    public void Execute_WorksWithMultipleLayers()
    {
        var map = new MapData(4, 4);
        map.AddLayer("Overlay");

        var ground = map.GetLayer("Ground");
        var objects = map.GetLayer("Objects");
        var overlay = map.GetLayer("Overlay");

        ground.SetCell(0, 0, 4, "grass");
        objects.SetCell(1, 1, 4, "tree");
        overlay.SetCell(2, 2, 4, "fog");

        var cmd = new ResizeMapCommand(map, 6, 6);
        cmd.Execute();

        Assert.Equal("grass", ground.GetCell(0, 0, 6));
        Assert.Equal("tree", objects.GetCell(1, 1, 6));
        Assert.Equal("fog", overlay.GetCell(2, 2, 6));

        Assert.Equal(6 * 6, ground.Cells.Length);
        Assert.Equal(6 * 6, objects.Cells.Length);
        Assert.Equal(6 * 6, overlay.Cells.Length);
    }

    [Fact]
    public void Undo_MultipleLayers_RestoresAllLayerData()
    {
        var map = new MapData(4, 4);
        map.AddLayer("Overlay");

        var ground = map.GetLayer("Ground");
        var overlay = map.GetLayer("Overlay");
        ground.SetCell(0, 0, 4, "grass");
        overlay.SetCell(3, 3, 4, "fog");

        var origGroundCells = (string[])ground.Cells.Clone();
        var origOverlayCells = (string[])overlay.Cells.Clone();

        var cmd = new ResizeMapCommand(map, 2, 2);
        cmd.Execute();
        cmd.Undo();

        Assert.Equal(4, map.Width);
        Assert.Equal(4, map.Height);
        Assert.Equal(origGroundCells, ground.Cells);
        Assert.Equal(origOverlayCells, overlay.Cells);
    }
}
