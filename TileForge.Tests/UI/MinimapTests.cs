using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Tests.Helpers;
using TileForge.UI;
using DojoUI;
using Xunit;

namespace TileForge.Tests.UI;

public class MinimapTests
{
    private static EditorState CreateState(int mapW = 40, int mapH = 30)
    {
        return new EditorState
        {
            Map = new MapData(mapW, mapH),
            Sheet = new MockSpriteSheet(16, 16, cols: 16, rows: 16),
        };
    }

    private static Rectangle DefaultCanvasBounds => new(200, 28, 1240, 850);

    // ===== GetGroupColor =====

    [Fact]
    public void GetGroupColor_DeterministicForSameName()
    {
        var c1 = Minimap.GetGroupColor("grass");
        var c2 = Minimap.GetGroupColor("grass");

        Assert.Equal(c1, c2);
    }

    [Fact]
    public void GetGroupColor_DifferentNamesProduceDifferentColors()
    {
        var c1 = Minimap.GetGroupColor("grass");
        var c2 = Minimap.GetGroupColor("wall");

        // At least one RGB channel should differ
        Assert.True(c1.R != c2.R || c1.G != c2.G || c1.B != c2.B);
    }

    // ===== GetMinimapRect =====

    [Fact]
    public void GetMinimapRect_WideMap_MaxWidthConstrained()
    {
        var rect = Minimap.GetMinimapRect(60, 30, DefaultCanvasBounds);

        Assert.Equal(160, rect.Width);
        Assert.Equal(80, rect.Height);
    }

    [Fact]
    public void GetMinimapRect_TallMap_MaxHeightConstrained()
    {
        var rect = Minimap.GetMinimapRect(30, 60, DefaultCanvasBounds);

        Assert.Equal(80, rect.Width);
        Assert.Equal(160, rect.Height);
    }

    [Fact]
    public void GetMinimapRect_SquareMap_SquareMinimap()
    {
        var rect = Minimap.GetMinimapRect(40, 40, DefaultCanvasBounds);

        Assert.Equal(160, rect.Width);
        Assert.Equal(160, rect.Height);
    }

    [Fact]
    public void GetMinimapRect_PositionedInBottomRightCorner()
    {
        var bounds = new Rectangle(200, 28, 1000, 800);
        var rect = Minimap.GetMinimapRect(40, 40, bounds);

        int expectedRight = bounds.Right - LayoutConstants.MinimapMargin;
        int expectedBottom = bounds.Bottom - LayoutConstants.MinimapMargin;

        Assert.Equal(expectedRight, rect.Right);
        Assert.Equal(expectedBottom, rect.Bottom);
    }

    // ===== Toggle =====

    [Fact]
    public void Toggle_FlipsVisibility()
    {
        var minimap = new Minimap();
        Assert.True(minimap.IsVisible); // default

        minimap.Toggle();
        Assert.False(minimap.IsVisible);

        minimap.Toggle();
        Assert.True(minimap.IsVisible);
    }

    // ===== HandleClick =====

    [Fact]
    public void HandleClick_InsideMinimap_ReturnsTrue()
    {
        var minimap = new Minimap();
        var state = CreateState();
        var camera = new Camera();

        // Get minimap bounds
        var mmRect = Minimap.GetMinimapRect(state.Map.Width, state.Map.Height, DefaultCanvasBounds);
        int clickX = mmRect.X + mmRect.Width / 2;
        int clickY = mmRect.Y + mmRect.Height / 2;

        bool result = minimap.HandleClick(clickX, clickY, state, camera, DefaultCanvasBounds);

        Assert.True(result);
    }

    [Fact]
    public void HandleClick_OutsideMinimap_ReturnsFalse()
    {
        var minimap = new Minimap();
        var state = CreateState();
        var camera = new Camera();

        // Click way outside the minimap
        bool result = minimap.HandleClick(300, 300, state, camera, DefaultCanvasBounds);

        Assert.False(result);
    }

    [Fact]
    public void HandleClick_NotVisible_ReturnsFalse()
    {
        var minimap = new Minimap();
        minimap.IsVisible = false;
        var state = CreateState();
        var camera = new Camera();

        var mmRect = Minimap.GetMinimapRect(state.Map.Width, state.Map.Height, DefaultCanvasBounds);
        int clickX = mmRect.X + mmRect.Width / 2;
        int clickY = mmRect.Y + mmRect.Height / 2;

        bool result = minimap.HandleClick(clickX, clickY, state, camera, DefaultCanvasBounds);

        Assert.False(result);
    }

    [Fact]
    public void HandleClick_CentersMinimap_UpdatesCameraOffset()
    {
        var minimap = new Minimap();
        var state = CreateState();
        var camera = new Camera();
        camera.Offset = Vector2.Zero;

        var mmRect = Minimap.GetMinimapRect(state.Map.Width, state.Map.Height, DefaultCanvasBounds);
        int clickX = mmRect.X + mmRect.Width / 2;
        int clickY = mmRect.Y + mmRect.Height / 2;

        minimap.HandleClick(clickX, clickY, state, camera, DefaultCanvasBounds);

        // Camera offset should have changed from zero
        Assert.True(camera.Offset != Vector2.Zero);
    }

    // ===== Ctrl+M keybind =====

    [Fact]
    public void CtrlM_InvokesToggleMinimap()
    {
        var state = new EditorState { Map = new MapData(10, 10) };
        bool toggled = false;
        var router = new InputRouter(
            state,
            save: () => { }, open: () => { },
            enterPlayMode: () => { }, exitPlayMode: () => { },
            exitGame: () => { }, resizeMap: () => { },
            toggleMinimap: () => toggled = true);

        var current = new KeyboardState(Keys.LeftControl, Keys.M);
        var prev = new KeyboardState(Keys.LeftControl);

        bool result = router.Update(current, prev, default);

        Assert.True(toggled);
        Assert.True(result);
    }
}
