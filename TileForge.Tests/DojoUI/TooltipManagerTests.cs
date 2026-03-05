using DojoUI;
using Xunit;

namespace TileForge.Tests.DojoUI;

public class TooltipManagerTests
{
    [Fact]
    public void NotVisible_Initially()
    {
        var tooltip = new TooltipManager();
        Assert.False(tooltip.IsVisible);
    }

    [Fact]
    public void NotVisible_BeforeDelay()
    {
        var tooltip = new TooltipManager(delaySeconds: 0.5);
        tooltip.SetHover("Hello", 100, 100);
        tooltip.Update(0.3);
        Assert.False(tooltip.IsVisible);
    }

    [Fact]
    public void Visible_AfterDelay()
    {
        var tooltip = new TooltipManager(delaySeconds: 0.5);
        tooltip.SetHover("Hello", 100, 100);
        tooltip.Update(0.6);
        Assert.True(tooltip.IsVisible);
    }

    [Fact]
    public void ClearHover_HidesTooltip()
    {
        var tooltip = new TooltipManager(delaySeconds: 0.5);
        tooltip.SetHover("Hello", 100, 100);
        tooltip.Update(0.6);
        Assert.True(tooltip.IsVisible);

        tooltip.ClearHover();
        Assert.False(tooltip.IsVisible);
    }

    [Fact]
    public void ChangedText_ResetsTimer()
    {
        var tooltip = new TooltipManager(delaySeconds: 0.5);
        tooltip.SetHover("Hello", 100, 100);
        tooltip.Update(0.4);                 // Almost ready — 0.4s elapsed
        tooltip.SetHover("Different", 100, 100); // Text changes → timer resets
        tooltip.Update(0.2);                 // Only 0.2s on the new timer
        Assert.False(tooltip.IsVisible);
    }

    [Fact]
    public void IsVisible_AfterExactDelay()
    {
        var tooltip = new TooltipManager(delaySeconds: 0.5);
        tooltip.SetHover("Hello", 100, 100);
        tooltip.Update(0.5);
        Assert.True(tooltip.IsVisible);
    }
}
