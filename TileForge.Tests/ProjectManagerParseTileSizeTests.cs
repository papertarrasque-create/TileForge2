using Xunit;

namespace TileForge.Tests;

/// <summary>
/// Tests for ProjectManager.ParseTileSize (internal static).
/// Requires InternalsVisibleTo in the main project.
/// </summary>
public class ProjectManagerParseTileSizeTests
{
    [Fact]
    public void ParseTileSize_SquareNumber_ParsesWidthAndHeight()
    {
        bool ok = ProjectManager.ParseTileSize("16", out int w, out int h, out int p);

        Assert.True(ok);
        Assert.Equal(16, w);
        Assert.Equal(16, h);
        Assert.Equal(0, p);
    }

    [Fact]
    public void ParseTileSize_Rectangular_ParsesBothDimensions()
    {
        bool ok = ProjectManager.ParseTileSize("16x24", out int w, out int h, out int p);

        Assert.True(ok);
        Assert.Equal(16, w);
        Assert.Equal(24, h);
        Assert.Equal(0, p);
    }

    [Fact]
    public void ParseTileSize_WithPadding_ParsesPadding()
    {
        bool ok = ProjectManager.ParseTileSize("16+1", out int w, out int h, out int p);

        Assert.True(ok);
        Assert.Equal(16, w);
        Assert.Equal(16, h);
        Assert.Equal(1, p);
    }

    [Fact]
    public void ParseTileSize_RectangularWithPadding_ParsesAll()
    {
        bool ok = ProjectManager.ParseTileSize("16x24+2", out int w, out int h, out int p);

        Assert.True(ok);
        Assert.Equal(16, w);
        Assert.Equal(24, h);
        Assert.Equal(2, p);
    }

    [Fact]
    public void ParseTileSize_WithWhitespace_TrimsInput()
    {
        bool ok = ProjectManager.ParseTileSize("  32  ", out int w, out int h, out int p);

        Assert.True(ok);
        Assert.Equal(32, w);
        Assert.Equal(32, h);
    }

    [Fact]
    public void ParseTileSize_Zero_ReturnsFalse()
    {
        bool ok = ProjectManager.ParseTileSize("0", out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void ParseTileSize_Negative_ReturnsFalse()
    {
        bool ok = ProjectManager.ParseTileSize("-5", out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void ParseTileSize_NonNumeric_ReturnsFalse()
    {
        bool ok = ProjectManager.ParseTileSize("abc", out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void ParseTileSize_EmptyString_ReturnsFalse()
    {
        bool ok = ProjectManager.ParseTileSize("", out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void ParseTileSize_RectangularZeroWidth_ReturnsFalse()
    {
        bool ok = ProjectManager.ParseTileSize("0x16", out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void ParseTileSize_RectangularZeroHeight_ReturnsFalse()
    {
        bool ok = ProjectManager.ParseTileSize("16x0", out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void ParseTileSize_LargeValue_Parses()
    {
        bool ok = ProjectManager.ParseTileSize("128", out int w, out int h, out _);

        Assert.True(ok);
        Assert.Equal(128, w);
        Assert.Equal(128, h);
    }
}
