using DojoUI;
using Microsoft.Xna.Framework;
using Xunit;

namespace TileForge.Tests.DojoUI;

public class ScrollPanelTests
{
    [Fact]
    public void InitialScrollOffset_IsZero()
    {
        var panel = new ScrollPanel();
        Assert.Equal(0, panel.ScrollOffset);
    }

    [Fact]
    public void ScrollOffset_CanBeSet()
    {
        var panel = new ScrollPanel();
        panel.ScrollOffset = 50;
        Assert.Equal(50, panel.ScrollOffset);
    }

    [Fact]
    public void ClampScroll_ClampsNegativeToZero()
    {
        var panel = new ScrollPanel();
        panel.ScrollOffset = -10;
        panel.ClampScroll();
        Assert.Equal(0, panel.ScrollOffset);
    }

    [Fact]
    public void ContentOverflows_FalseInitially()
    {
        var panel = new ScrollPanel();
        Assert.False(panel.ContentOverflows);
    }

    [Fact]
    public void ScrollBarWidth_IsSix()
    {
        Assert.Equal(6, ScrollPanel.ScrollBarWidth);
    }

    [Fact]
    public void ScrollStep_IsTwenty()
    {
        Assert.Equal(20, ScrollPanel.ScrollStep);
    }
}
