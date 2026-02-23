using TileForge.Editor;
using Xunit;

namespace TileForge.Tests.Editor;

public class GridConfigTests
{
    [Fact]
    public void DefaultMode_IsNormal()
    {
        var config = new GridConfig();
        Assert.Equal(GridMode.Normal, config.Mode);
    }

    [Fact]
    public void CycleMode_NormalToFine()
    {
        var config = new GridConfig();
        config.CycleMode();
        Assert.Equal(GridMode.Fine, config.Mode);
    }

    [Fact]
    public void CycleMode_FineToOff()
    {
        var config = new GridConfig { Mode = GridMode.Fine };
        config.CycleMode();
        Assert.Equal(GridMode.Off, config.Mode);
    }

    [Fact]
    public void CycleMode_OffToNormal()
    {
        var config = new GridConfig { Mode = GridMode.Off };
        config.CycleMode();
        Assert.Equal(GridMode.Normal, config.Mode);
    }

    [Fact]
    public void CycleMode_FullCycle_ReturnsToNormal()
    {
        var config = new GridConfig();
        config.CycleMode(); // Normal → Fine
        config.CycleMode(); // Fine → Off
        config.CycleMode(); // Off → Normal
        Assert.Equal(GridMode.Normal, config.Mode);
    }

    [Fact]
    public void LineColor_HasDefault()
    {
        var config = new GridConfig();
        Assert.Equal(LayoutConstants.CanvasGridColor, config.LineColor);
    }

    [Fact]
    public void BorderColor_HasDefault()
    {
        var config = new GridConfig();
        Assert.Equal(LayoutConstants.CanvasGridBorderColor, config.BorderColor);
    }

    [Fact]
    public void SubdivisionColor_HasDefault()
    {
        var config = new GridConfig();
        Assert.Equal(LayoutConstants.GridSubdivisionColor, config.SubdivisionColor);
    }
}
