using Xunit;
using Microsoft.Xna.Framework;
using TileForge.UI;

namespace TileForge.Tests.UI;

public class NodeGraphCameraTests
{
    [Fact]
    public void WorldToScreen_DefaultZoomAndOffset_ReturnsIdentity()
    {
        var cam = new NodeGraphCamera();
        var result = cam.WorldToScreen(new Vector2(100, 200));
        Assert.Equal(100f, result.X);
        Assert.Equal(200f, result.Y);
    }

    [Fact]
    public void WorldToScreen_WithZoom_ScalesPosition()
    {
        var cam = new NodeGraphCamera();
        cam.AdjustZoom(1f, Vector2.Zero); // zoom to 2.0
        var result = cam.WorldToScreen(new Vector2(50, 50));
        // At zoom 2.0 with offset adjusted to keep origin stable
        Assert.Equal(50f * 2f + cam.Offset.X, result.X, 0.01f);
    }

    [Fact]
    public void ScreenToWorld_RoundTrip_ReturnsOriginal()
    {
        var cam = new NodeGraphCamera();
        cam.AdjustZoom(0.5f, new Vector2(100, 100));
        var world = new Vector2(75, 150);
        var screen = cam.WorldToScreen(world);
        var backToWorld = cam.ScreenToWorld(screen);
        Assert.Equal(world.X, backToWorld.X, 0.01f);
        Assert.Equal(world.Y, backToWorld.Y, 0.01f);
    }

    [Fact]
    public void AdjustZoom_ClampsAtMin()
    {
        var cam = new NodeGraphCamera();
        cam.AdjustZoom(-10f, Vector2.Zero);
        Assert.Equal(NodeGraphCamera.MinZoom, cam.Zoom);
    }

    [Fact]
    public void AdjustZoom_ClampsAtMax()
    {
        var cam = new NodeGraphCamera();
        cam.AdjustZoom(10f, Vector2.Zero);
        Assert.Equal(NodeGraphCamera.MaxZoom, cam.Zoom);
    }

    [Fact]
    public void AdjustZoom_KeepsScreenCenterStable()
    {
        var cam = new NodeGraphCamera();
        var screenCenter = new Vector2(400, 300);
        var worldBefore = cam.ScreenToWorld(screenCenter);
        cam.AdjustZoom(0.5f, screenCenter);
        var worldAfter = cam.ScreenToWorld(screenCenter);
        Assert.Equal(worldBefore.X, worldAfter.X, 0.01f);
        Assert.Equal(worldBefore.Y, worldAfter.Y, 0.01f);
    }

    [Fact]
    public void CenterOn_CentersWorldPointInViewport()
    {
        var cam = new NodeGraphCamera();
        cam.CenterOn(100f, 200f, 800, 600);
        var screen = cam.WorldToScreen(new Vector2(100, 200));
        Assert.Equal(400f, screen.X, 0.01f);
        Assert.Equal(300f, screen.Y, 0.01f);
    }

    [Fact]
    public void DefaultZoom_IsOne()
    {
        var cam = new NodeGraphCamera();
        Assert.Equal(1.0f, cam.Zoom);
    }
}
