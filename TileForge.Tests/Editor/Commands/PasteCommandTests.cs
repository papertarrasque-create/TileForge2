using TileForge.Data;
using TileForge.Editor;
using TileForge.Editor.Commands;
using Xunit;

namespace TileForge.Tests.Editor.Commands;

public class PasteCommandTests
{
    private static MapData CreateMap(int w = 5, int h = 5)
    {
        return new MapData(w, h);
    }

    private static TileClipboard CreateClipboard(int w, int h, params string[] cells)
    {
        return new TileClipboard(w, h, cells);
    }

    [Fact]
    public void Execute_PastesSingleCell()
    {
        var map = CreateMap();
        var clipboard = CreateClipboard(1, 1, "grass");
        var cmd = new PasteCommand(map, "Ground", 2, 3, clipboard);

        cmd.Execute();

        Assert.Equal("grass", map.GetLayer("Ground").GetCell(2, 3, map.Width));
    }

    [Fact]
    public void Execute_Pastes2x2Region()
    {
        var map = CreateMap();
        var clipboard = CreateClipboard(2, 2, "a", "b", "c", "d");
        var cmd = new PasteCommand(map, "Ground", 1, 1, clipboard);

        cmd.Execute();

        var layer = map.GetLayer("Ground");
        Assert.Equal("a", layer.GetCell(1, 1, map.Width));
        Assert.Equal("b", layer.GetCell(2, 1, map.Width));
        Assert.Equal("c", layer.GetCell(1, 2, map.Width));
        Assert.Equal("d", layer.GetCell(2, 2, map.Width));
    }

    [Fact]
    public void Execute_SkipsNullClipboardCells()
    {
        var map = CreateMap();
        var layer = map.GetLayer("Ground");
        layer.SetCell(1, 1, map.Width, "original");

        var clipboard = CreateClipboard(2, 1, null, "new");
        var cmd = new PasteCommand(map, "Ground", 1, 1, clipboard);

        cmd.Execute();

        // Null clipboard cell should leave original intact
        Assert.Equal("original", layer.GetCell(1, 1, map.Width));
        Assert.Equal("new", layer.GetCell(2, 1, map.Width));
    }

    [Fact]
    public void Execute_ClipsToMapBounds()
    {
        var map = CreateMap(3, 3);
        var clipboard = CreateClipboard(2, 2, "a", "b", "c", "d");
        // Paste at (2,2) — only (2,2) is in bounds for a 3x3 map
        var cmd = new PasteCommand(map, "Ground", 2, 2, clipboard);

        cmd.Execute();

        Assert.Equal("a", map.GetLayer("Ground").GetCell(2, 2, map.Width));
    }

    [Fact]
    public void Undo_RestoresOriginalCells()
    {
        var map = CreateMap();
        var layer = map.GetLayer("Ground");
        layer.SetCell(1, 1, map.Width, "original1");
        layer.SetCell(2, 1, map.Width, "original2");

        var clipboard = CreateClipboard(2, 1, "new1", "new2");
        var cmd = new PasteCommand(map, "Ground", 1, 1, clipboard);

        cmd.Execute();
        Assert.Equal("new1", layer.GetCell(1, 1, map.Width));
        Assert.Equal("new2", layer.GetCell(2, 1, map.Width));

        cmd.Undo();
        Assert.Equal("original1", layer.GetCell(1, 1, map.Width));
        Assert.Equal("original2", layer.GetCell(2, 1, map.Width));
    }

    [Fact]
    public void Undo_RestoresNullCells()
    {
        var map = CreateMap();
        var layer = map.GetLayer("Ground");
        // Cell starts as null

        var clipboard = CreateClipboard(1, 1, "grass");
        var cmd = new PasteCommand(map, "Ground", 0, 0, clipboard);

        cmd.Execute();
        Assert.Equal("grass", layer.GetCell(0, 0, map.Width));

        cmd.Undo();
        Assert.Null(layer.GetCell(0, 0, map.Width));
    }

    [Fact]
    public void ExecuteUndo_RoundtripIsLossless()
    {
        var map = CreateMap(4, 4);
        var layer = map.GetLayer("Ground");
        // Set up some existing data
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                layer.SetCell(x, y, map.Width, $"orig_{x}_{y}");

        var clipboard = CreateClipboard(2, 2, "p1", "p2", "p3", "p4");
        var cmd = new PasteCommand(map, "Ground", 1, 1, clipboard);

        cmd.Execute();
        cmd.Undo();

        // All cells should be restored
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                Assert.Equal($"orig_{x}_{y}", layer.GetCell(x, y, map.Width));
    }

    [Fact]
    public void Execute_NonexistentLayer_DoesNotThrow()
    {
        var map = CreateMap();
        var clipboard = CreateClipboard(1, 1, "grass");
        var cmd = new PasteCommand(map, "NoSuchLayer", 0, 0, clipboard);

        var ex = Record.Exception(() => cmd.Execute());
        Assert.Null(ex);
    }

    [Fact]
    public void Execute_PasteAtOrigin_Works()
    {
        var map = CreateMap();
        var clipboard = CreateClipboard(1, 1, "grass");
        var cmd = new PasteCommand(map, "Ground", 0, 0, clipboard);

        cmd.Execute();

        Assert.Equal("grass", map.GetLayer("Ground").GetCell(0, 0, map.Width));
    }
}
