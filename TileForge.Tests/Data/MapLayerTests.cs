using TileForge.Data;
using Xunit;

namespace TileForge.Tests.Data;

public class MapLayerTests
{
    [Fact]
    public void Constructor_CreatesCellsArrayOfCorrectSize()
    {
        var layer = new MapLayer("Test", 10, 8);

        Assert.Equal(10 * 8, layer.Cells.Length);
    }

    [Fact]
    public void Constructor_SetsNameCorrectly()
    {
        var layer = new MapLayer("Ground", 5, 5);

        Assert.Equal("Ground", layer.Name);
    }

    [Fact]
    public void Constructor_CellsAreInitiallyNull()
    {
        var layer = new MapLayer("Test", 5, 5);

        for (int i = 0; i < layer.Cells.Length; i++)
        {
            Assert.Null(layer.Cells[i]);
        }
    }

    [Fact]
    public void DefaultVisibility_IsTrue()
    {
        var layer = new MapLayer("Test", 5, 5);

        Assert.True(layer.Visible);
    }

    [Fact]
    public void Visible_CanBeSetToFalse()
    {
        var layer = new MapLayer("Test", 5, 5);

        layer.Visible = false;

        Assert.False(layer.Visible);
    }

    [Fact]
    public void SetCell_GetCell_Roundtrip_ReturnsCorrectValue()
    {
        int width = 10;
        var layer = new MapLayer("Test", width, 8);

        layer.SetCell(3, 4, width, "grass");
        var result = layer.GetCell(3, 4, width);

        Assert.Equal("grass", result);
    }

    [Fact]
    public void SetCell_GetCell_MultipleValues_AllPreserved()
    {
        int width = 10;
        var layer = new MapLayer("Test", width, 8);

        layer.SetCell(0, 0, width, "grass");
        layer.SetCell(9, 7, width, "wall");
        layer.SetCell(5, 3, width, "water");

        Assert.Equal("grass", layer.GetCell(0, 0, width));
        Assert.Equal("wall", layer.GetCell(9, 7, width));
        Assert.Equal("water", layer.GetCell(5, 3, width));
    }

    [Fact]
    public void SetCell_OverwritesPreviousValue()
    {
        int width = 10;
        var layer = new MapLayer("Test", width, 8);

        layer.SetCell(3, 4, width, "grass");
        layer.SetCell(3, 4, width, "wall");

        Assert.Equal("wall", layer.GetCell(3, 4, width));
    }

    [Fact]
    public void SetCell_CanSetToNull()
    {
        int width = 10;
        var layer = new MapLayer("Test", width, 8);

        layer.SetCell(3, 4, width, "grass");
        layer.SetCell(3, 4, width, null);

        Assert.Null(layer.GetCell(3, 4, width));
    }

    [Fact]
    public void GetCell_OutOfBoundsNegativeIndex_ReturnsNull()
    {
        int width = 10;
        var layer = new MapLayer("Test", width, 8);

        // Negative x with small y will produce negative index
        var result = layer.GetCell(-1, 0, width);

        Assert.Null(result);
    }

    [Fact]
    public void GetCell_OutOfBoundsBeyondLength_ReturnsNull()
    {
        int width = 10;
        var layer = new MapLayer("Test", width, 8);

        // x=10 with y=8 will be index 90, beyond 80 cells
        var result = layer.GetCell(10, 8, width);

        Assert.Null(result);
    }

    [Fact]
    public void GetCell_UnsetCell_ReturnsNull()
    {
        int width = 10;
        var layer = new MapLayer("Test", width, 8);

        var result = layer.GetCell(5, 5, width);

        Assert.Null(result);
    }

    [Fact]
    public void SetCell_OutOfBoundsNegativeIndex_DoesNotThrow()
    {
        int width = 10;
        var layer = new MapLayer("Test", width, 8);

        // Should silently do nothing
        var exception = Record.Exception(() => layer.SetCell(-1, 0, width, "grass"));

        Assert.Null(exception);
    }

    [Fact]
    public void SetCell_OutOfBoundsBeyondLength_DoesNotThrow()
    {
        int width = 10;
        var layer = new MapLayer("Test", width, 8);

        // Should silently do nothing
        var exception = Record.Exception(() => layer.SetCell(10, 8, width, "grass"));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0, 0, 10, 0)]     // First cell
    [InlineData(9, 0, 10, 9)]     // End of first row
    [InlineData(0, 1, 10, 10)]    // Start of second row
    [InlineData(5, 3, 10, 35)]    // Middle cell
    public void GetCell_SetCell_UsesCorrectIndexing(int x, int y, int width, int expectedIndex)
    {
        var layer = new MapLayer("Test", width, 8);

        layer.SetCell(x, y, width, "test");

        // Verify the correct index in the underlying array was set
        Assert.Equal("test", layer.Cells[expectedIndex]);
    }
}
