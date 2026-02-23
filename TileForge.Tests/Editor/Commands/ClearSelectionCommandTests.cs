using Microsoft.Xna.Framework;
using TileForge.Data;
using TileForge.Editor.Commands;
using Xunit;

namespace TileForge.Tests.Editor.Commands;

public class ClearSelectionCommandTests
{
    private static MapData CreateMap(int w = 5, int h = 5)
    {
        return new MapData(w, h);
    }

    [Fact]
    public void Execute_ClearsSingleCell()
    {
        var map = CreateMap();
        var layer = map.GetLayer("Ground");
        layer.SetCell(2, 2, map.Width, "grass");

        var cmd = new ClearSelectionCommand(map, "Ground", new Rectangle(2, 2, 1, 1));
        cmd.Execute();

        Assert.Null(layer.GetCell(2, 2, map.Width));
    }

    [Fact]
    public void Execute_ClearsMultipleCells()
    {
        var map = CreateMap();
        var layer = map.GetLayer("Ground");
        layer.SetCell(1, 1, map.Width, "grass");
        layer.SetCell(2, 1, map.Width, "wall");
        layer.SetCell(1, 2, map.Width, "water");
        layer.SetCell(2, 2, map.Width, "sand");

        var cmd = new ClearSelectionCommand(map, "Ground", new Rectangle(1, 1, 2, 2));
        cmd.Execute();

        Assert.Null(layer.GetCell(1, 1, map.Width));
        Assert.Null(layer.GetCell(2, 1, map.Width));
        Assert.Null(layer.GetCell(1, 2, map.Width));
        Assert.Null(layer.GetCell(2, 2, map.Width));
    }

    [Fact]
    public void Execute_DoesNotAffectCellsOutsideSelection()
    {
        var map = CreateMap();
        var layer = map.GetLayer("Ground");
        layer.SetCell(0, 0, map.Width, "keep");
        layer.SetCell(1, 1, map.Width, "clear");

        var cmd = new ClearSelectionCommand(map, "Ground", new Rectangle(1, 1, 1, 1));
        cmd.Execute();

        Assert.Equal("keep", layer.GetCell(0, 0, map.Width));
        Assert.Null(layer.GetCell(1, 1, map.Width));
    }

    [Fact]
    public void Execute_ClipsToMapBounds()
    {
        var map = CreateMap(3, 3);
        var layer = map.GetLayer("Ground");
        layer.SetCell(2, 2, map.Width, "grass");

        // Selection extends beyond map bounds
        var cmd = new ClearSelectionCommand(map, "Ground", new Rectangle(2, 2, 5, 5));

        var ex = Record.Exception(() => cmd.Execute());
        Assert.Null(ex);
        Assert.Null(layer.GetCell(2, 2, map.Width));
    }

    [Fact]
    public void Undo_RestoresOriginalCells()
    {
        var map = CreateMap();
        var layer = map.GetLayer("Ground");
        layer.SetCell(1, 1, map.Width, "grass");
        layer.SetCell(2, 1, map.Width, "wall");

        var cmd = new ClearSelectionCommand(map, "Ground", new Rectangle(1, 1, 2, 1));
        cmd.Execute();
        Assert.Null(layer.GetCell(1, 1, map.Width));
        Assert.Null(layer.GetCell(2, 1, map.Width));

        cmd.Undo();
        Assert.Equal("grass", layer.GetCell(1, 1, map.Width));
        Assert.Equal("wall", layer.GetCell(2, 1, map.Width));
    }

    [Fact]
    public void Undo_RestoresNullCellsAsNull()
    {
        var map = CreateMap();
        // Cell starts null, gets cleared (still null), undo should still be null
        var cmd = new ClearSelectionCommand(map, "Ground", new Rectangle(0, 0, 1, 1));

        cmd.Execute();
        cmd.Undo();

        Assert.Null(map.GetLayer("Ground").GetCell(0, 0, map.Width));
    }

    [Fact]
    public void ExecuteUndo_RoundtripIsLossless()
    {
        var map = CreateMap(4, 4);
        var layer = map.GetLayer("Ground");
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                layer.SetCell(x, y, map.Width, $"cell_{x}_{y}");

        var cmd = new ClearSelectionCommand(map, "Ground", new Rectangle(1, 1, 2, 2));
        cmd.Execute();
        cmd.Undo();

        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                Assert.Equal($"cell_{x}_{y}", layer.GetCell(x, y, map.Width));
    }

    [Fact]
    public void Execute_NonexistentLayer_DoesNotThrow()
    {
        var map = CreateMap();
        var cmd = new ClearSelectionCommand(map, "NoSuchLayer", new Rectangle(0, 0, 1, 1));

        var ex = Record.Exception(() => cmd.Execute());
        Assert.Null(ex);
    }

    [Fact]
    public void Execute_AtOrigin_Works()
    {
        var map = CreateMap();
        var layer = map.GetLayer("Ground");
        layer.SetCell(0, 0, map.Width, "grass");

        var cmd = new ClearSelectionCommand(map, "Ground", new Rectangle(0, 0, 1, 1));
        cmd.Execute();

        Assert.Null(layer.GetCell(0, 0, map.Width));
    }
}
