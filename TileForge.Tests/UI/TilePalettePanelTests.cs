using Microsoft.Xna.Framework;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Tests.Helpers;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class TilePalettePanelTests
{
    private static EditorState CreateState()
    {
        var state = new EditorState
        {
            Map = new MapData(10, 10),
            Sheet = new MockSpriteSheet(16, 16, cols: 8, rows: 8),
        };
        return state;
    }

    private static TilePalettePanel CreatePanel(int contentWidth = 200, int contentHeight = 200)
    {
        var panel = new TilePalettePanel();
        // Simulate PanelDock layout assignment
        panel.ContentBounds = new Rectangle(0, 24, contentWidth, contentHeight);
        return panel;
    }

    // ===== Sprite-to-group index =====

    [Fact]
    public void RebuildGroupIndex_MapsSpritesToGroups()
    {
        var state = CreateState();
        var grass = new TileGroup { Name = "grass", Sprites = new() { new SpriteRef { Col = 0, Row = 0 } } };
        var wall = new TileGroup { Name = "wall", Sprites = new() { new SpriteRef { Col = 1, Row = 0 } } };
        state.AddGroup(grass);
        state.AddGroup(wall);

        var panel = CreatePanel();
        panel.RebuildGroupIndex(state);

        Assert.Equal(grass, panel.SpriteGroupIndex[(0, 0)]);
        Assert.Equal(wall, panel.SpriteGroupIndex[(1, 0)]);
    }

    [Fact]
    public void RebuildGroupIndex_FirstGroupWins()
    {
        var state = CreateState();
        var first = new TileGroup { Name = "first", Sprites = new() { new SpriteRef { Col = 0, Row = 0 } } };
        var second = new TileGroup { Name = "second", Sprites = new() { new SpriteRef { Col = 0, Row = 0 } } };
        state.AddGroup(first);
        state.AddGroup(second);

        var panel = CreatePanel();
        panel.RebuildGroupIndex(state);

        Assert.Equal(first, panel.SpriteGroupIndex[(0, 0)]);
    }

    [Fact]
    public void RebuildGroupIndex_EmptyGroups_NoEntries()
    {
        var state = CreateState();
        state.AddGroup(new TileGroup { Name = "empty", Sprites = new() });

        var panel = CreatePanel();
        panel.RebuildGroupIndex(state);

        Assert.Empty(panel.SpriteGroupIndex);
    }

    [Fact]
    public void RebuildGroupIndex_MultiSpriteGroup_AllMapped()
    {
        var state = CreateState();
        var grass = new TileGroup
        {
            Name = "grass",
            Sprites = new()
            {
                new SpriteRef { Col = 0, Row = 0 },
                new SpriteRef { Col = 1, Row = 0 },
                new SpriteRef { Col = 2, Row = 0 },
            }
        };
        state.AddGroup(grass);

        var panel = CreatePanel();
        panel.RebuildGroupIndex(state);

        Assert.Equal(3, panel.SpriteGroupIndex.Count);
        Assert.Equal(grass, panel.SpriteGroupIndex[(0, 0)]);
        Assert.Equal(grass, panel.SpriteGroupIndex[(1, 0)]);
        Assert.Equal(grass, panel.SpriteGroupIndex[(2, 0)]);
    }

    // ===== Tile display size calculation =====

    [Fact]
    public void CalculateTileDisplaySize_FitsToWidth()
    {
        var panel = CreatePanel(contentWidth: 200);

        // 8 columns: 200/8 = 25
        Assert.Equal(25, panel.CalculateTileDisplaySize(8));
    }

    [Fact]
    public void CalculateTileDisplaySize_ClampsToMinimum()
    {
        var panel = CreatePanel(contentWidth: 40);

        // 8 columns: 40/8 = 5, but min is 8
        Assert.Equal(8, panel.CalculateTileDisplaySize(8));
    }

    [Fact]
    public void CalculateTileDisplaySize_ZeroCols_ReturnsMinimum()
    {
        var panel = CreatePanel(contentWidth: 200);

        Assert.Equal(8, panel.CalculateTileDisplaySize(0));
    }

    // ===== Panel properties =====

    [Fact]
    public void Title_IsTileset()
    {
        var panel = new TilePalettePanel();
        Assert.Equal("Tileset", panel.Title);
    }

    [Fact]
    public void SizeMode_IsFlexible()
    {
        var panel = new TilePalettePanel();
        Assert.Equal(PanelSizeMode.Flexible, panel.SizeMode);
    }
}
