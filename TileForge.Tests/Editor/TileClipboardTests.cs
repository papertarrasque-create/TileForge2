using TileForge.Editor;
using Xunit;

namespace TileForge.Tests.Editor;

public class TileClipboardTests
{
    // ===== Construction =====

    [Fact]
    public void Constructor_SetsWidthAndHeight()
    {
        var clipboard = new TileClipboard(3, 2, new string[6]);

        Assert.Equal(3, clipboard.Width);
        Assert.Equal(2, clipboard.Height);
    }

    [Fact]
    public void Constructor_StoresCellsArray()
    {
        var cells = new string[] { "grass", "wall", "water", "sand" };
        var clipboard = new TileClipboard(2, 2, cells);

        Assert.Same(cells, clipboard.Cells);
    }

    [Fact]
    public void Constructor_1x1_SingleCell()
    {
        var cells = new string[] { "grass" };
        var clipboard = new TileClipboard(1, 1, cells);

        Assert.Equal(1, clipboard.Width);
        Assert.Equal(1, clipboard.Height);
        Assert.Single(clipboard.Cells);
    }

    // ===== GetCell =====

    [Fact]
    public void GetCell_TopLeftCorner_ReturnsFirstCell()
    {
        // Layout (2x2):
        // [0,0]="a"  [1,0]="b"
        // [0,1]="c"  [1,1]="d"
        var cells = new string[] { "a", "b", "c", "d" };
        var clipboard = new TileClipboard(2, 2, cells);

        Assert.Equal("a", clipboard.GetCell(0, 0));
    }

    [Fact]
    public void GetCell_TopRightCorner_ReturnsCorrectCell()
    {
        var cells = new string[] { "a", "b", "c", "d" };
        var clipboard = new TileClipboard(2, 2, cells);

        Assert.Equal("b", clipboard.GetCell(1, 0));
    }

    [Fact]
    public void GetCell_BottomLeftCorner_ReturnsCorrectCell()
    {
        var cells = new string[] { "a", "b", "c", "d" };
        var clipboard = new TileClipboard(2, 2, cells);

        Assert.Equal("c", clipboard.GetCell(0, 1));
    }

    [Fact]
    public void GetCell_BottomRightCorner_ReturnsCorrectCell()
    {
        var cells = new string[] { "a", "b", "c", "d" };
        var clipboard = new TileClipboard(2, 2, cells);

        Assert.Equal("d", clipboard.GetCell(1, 1));
    }

    [Fact]
    public void GetCell_NullCell_ReturnsNull()
    {
        var cells = new string[] { "a", null, null, "d" };
        var clipboard = new TileClipboard(2, 2, cells);

        Assert.Null(clipboard.GetCell(1, 0));
        Assert.Null(clipboard.GetCell(0, 1));
    }

    [Fact]
    public void GetCell_WideClipboard_RowMajorIndexing()
    {
        // Layout (4x2):
        // [0,0]="a" [1,0]="b" [2,0]="c" [3,0]="d"
        // [0,1]="e" [1,1]="f" [2,1]="g" [3,1]="h"
        var cells = new string[] { "a", "b", "c", "d", "e", "f", "g", "h" };
        var clipboard = new TileClipboard(4, 2, cells);

        Assert.Equal("a", clipboard.GetCell(0, 0));
        Assert.Equal("d", clipboard.GetCell(3, 0));
        Assert.Equal("e", clipboard.GetCell(0, 1));
        Assert.Equal("h", clipboard.GetCell(3, 1));
    }

    [Fact]
    public void GetCell_TallClipboard_RowMajorIndexing()
    {
        // Layout (2x4):
        // [0,0]="a" [1,0]="b"
        // [0,1]="c" [1,1]="d"
        // [0,2]="e" [1,2]="f"
        // [0,3]="g" [1,3]="h"
        var cells = new string[] { "a", "b", "c", "d", "e", "f", "g", "h" };
        var clipboard = new TileClipboard(2, 4, cells);

        Assert.Equal("a", clipboard.GetCell(0, 0));
        Assert.Equal("b", clipboard.GetCell(1, 0));
        Assert.Equal("g", clipboard.GetCell(0, 3));
        Assert.Equal("h", clipboard.GetCell(1, 3));
    }

    // ===== Bounds / Cell Count =====

    [Fact]
    public void Cells_Length_EqualsWidthTimesHeight()
    {
        var clipboard = new TileClipboard(3, 4, new string[12]);

        Assert.Equal(12, clipboard.Cells.Length);
        Assert.Equal(clipboard.Width * clipboard.Height, clipboard.Cells.Length);
    }

    [Fact]
    public void GetCell_AllCellsAccessible_NoIndexOutOfRange()
    {
        int w = 5, h = 3;
        var cells = new string[w * h];
        for (int i = 0; i < cells.Length; i++)
            cells[i] = $"cell_{i}";

        var clipboard = new TileClipboard(w, h, cells);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var exception = Record.Exception(() => clipboard.GetCell(x, y));
                Assert.Null(exception);
            }
        }
    }

    [Fact]
    public void GetCell_AllCellsMatchExpected_RowMajorOrder()
    {
        int w = 3, h = 3;
        var cells = new string[w * h];
        for (int i = 0; i < cells.Length; i++)
            cells[i] = $"cell_{i}";

        var clipboard = new TileClipboard(w, h, cells);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int expectedIndex = x + y * w;
                Assert.Equal($"cell_{expectedIndex}", clipboard.GetCell(x, y));
            }
        }
    }
}
