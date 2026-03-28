using TileForge.Data;
using TileForge.Editor.Commands;
using Xunit;

namespace TileForge.Tests.Editor.Commands;

public class MoveGroupLayerCommandTests
{
    private static MapData MakeMap(int w, int h, string[] layerNames)
    {
        var map = new MapData(w, h);
        map.Layers.Clear();
        foreach (var name in layerNames)
            map.Layers.Add(new MapLayer(name, w, h));
        return map;
    }

    [Fact]
    public void Execute_MovesCells_FromOldLayerToNewLayer()
    {
        var map = MakeMap(3, 3, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(1, 1, 3, "bush");
        map.Layers[0].SetCell(2, 0, 3, "bush");

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });
        cmd.Execute();

        Assert.Null(map.Layers[0].GetCell(1, 1, 3));
        Assert.Null(map.Layers[0].GetCell(2, 0, 3));
        Assert.Equal("bush", map.Layers[1].GetCell(1, 1, 3));
        Assert.Equal("bush", map.Layers[1].GetCell(2, 0, 3));
        Assert.Equal("Objects", group.LayerName);
    }

    [Fact]
    public void Undo_RestoresOriginalState()
    {
        var map = MakeMap(3, 3, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(1, 1, 3, "bush");

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });
        cmd.Execute();
        cmd.Undo();

        Assert.Equal("bush", map.Layers[0].GetCell(1, 1, 3));
        Assert.Null(map.Layers[1].GetCell(1, 1, 3));
        Assert.Equal("Ground", group.LayerName);
    }

    [Fact]
    public void Execute_OverwritesConflicts_OnTargetLayer()
    {
        var map = MakeMap(3, 3, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(1, 1, 3, "bush");
        map.Layers[1].SetCell(1, 1, 3, "rock"); // conflict

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });
        cmd.Execute();

        Assert.Null(map.Layers[0].GetCell(1, 1, 3));
        Assert.Equal("bush", map.Layers[1].GetCell(1, 1, 3));
    }

    [Fact]
    public void Undo_RestoresOverwrittenConflicts()
    {
        var map = MakeMap(3, 3, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(1, 1, 3, "bush");
        map.Layers[1].SetCell(1, 1, 3, "rock"); // conflict

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });
        cmd.Execute();
        cmd.Undo();

        Assert.Equal("bush", map.Layers[0].GetCell(1, 1, 3));
        Assert.Equal("rock", map.Layers[1].GetCell(1, 1, 3));
    }

    [Fact]
    public void ConflictCount_ReturnsCorrectCount()
    {
        var map = MakeMap(3, 3, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(0, 0, 3, "bush");
        map.Layers[0].SetCell(1, 1, 3, "bush");
        map.Layers[1].SetCell(1, 1, 3, "rock"); // one conflict

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });

        Assert.Equal(1, cmd.ConflictCount);
    }

    [Fact]
    public void SkipsMap_WhenTargetLayerMissing()
    {
        var map = MakeMap(3, 3, new[] { "Ground" }); // no "Objects" layer
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(1, 1, 3, "bush");

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });
        cmd.Execute();

        // Cell stays on Ground because Objects layer doesn't exist
        Assert.Equal("bush", map.Layers[0].GetCell(1, 1, 3));
    }

    [Fact]
    public void WorksAcrossMultipleMaps()
    {
        var map1 = MakeMap(2, 2, new[] { "Ground", "Objects" });
        var map2 = MakeMap(2, 2, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map1.Layers[0].SetCell(0, 0, 2, "bush");
        map2.Layers[0].SetCell(1, 1, 2, "bush");

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map1, map2 });
        cmd.Execute();

        Assert.Null(map1.Layers[0].GetCell(0, 0, 2));
        Assert.Equal("bush", map1.Layers[1].GetCell(0, 0, 2));
        Assert.Null(map2.Layers[0].GetCell(1, 1, 2));
        Assert.Equal("bush", map2.Layers[1].GetCell(1, 1, 2));
    }
}
