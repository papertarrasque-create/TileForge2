using Microsoft.Xna.Framework;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class HitTestRegistryTests
{
    [Fact]
    public void HitTest_NoRegistrations_ReturnsNegativeOne()
    {
        var registry = new HitTestRegistry();
        Assert.Equal(-1, registry.HitTest("anything", new Point(50, 50)));
    }

    [Fact]
    public void Register_ThenHitTest_ReturnsIndex()
    {
        var registry = new HitTestRegistry();
        registry.Register("buttons", new Rectangle(10, 10, 100, 30), 0);
        registry.Register("buttons", new Rectangle(10, 50, 100, 30), 1);
        Assert.Equal(0, registry.HitTest("buttons", new Point(50, 20)));
        Assert.Equal(1, registry.HitTest("buttons", new Point(50, 60)));
    }

    [Fact]
    public void HitTest_PointOutsideAllRects_ReturnsNegativeOne()
    {
        var registry = new HitTestRegistry();
        registry.Register("buttons", new Rectangle(10, 10, 100, 30), 0);
        Assert.Equal(-1, registry.HitTest("buttons", new Point(500, 500)));
    }

    [Fact]
    public void HitTest_WrongZone_ReturnsNegativeOne()
    {
        var registry = new HitTestRegistry();
        registry.Register("buttons", new Rectangle(10, 10, 100, 30), 0);
        Assert.Equal(-1, registry.HitTest("labels", new Point(50, 20)));
    }

    [Fact]
    public void Clear_RemovesAllRegistrations()
    {
        var registry = new HitTestRegistry();
        registry.Register("buttons", new Rectangle(10, 10, 100, 30), 0);
        registry.Clear();
        Assert.Equal(-1, registry.HitTest("buttons", new Point(50, 20)));
    }

    [Fact]
    public void OverlappingRects_ReturnsLastRegistered()
    {
        var registry = new HitTestRegistry();
        registry.Register("buttons", new Rectangle(10, 10, 100, 100), 0);
        registry.Register("buttons", new Rectangle(10, 10, 100, 100), 1);
        Assert.Equal(1, registry.HitTest("buttons", new Point(50, 50)));
    }

    [Fact]
    public void MultipleZones_Independent()
    {
        var registry = new HitTestRegistry();
        registry.Register("zone-a", new Rectangle(10, 10, 50, 50), 0);
        registry.Register("zone-b", new Rectangle(10, 10, 50, 50), 5);
        Assert.Equal(0, registry.HitTest("zone-a", new Point(30, 30)));
        Assert.Equal(5, registry.HitTest("zone-b", new Point(30, 30)));
    }
}
