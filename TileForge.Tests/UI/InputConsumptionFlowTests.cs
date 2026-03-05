using DojoUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Xunit;

namespace TileForge.Tests.UI;

/// <summary>
/// Integration tests verifying that InputEvent consumption prevents click
/// propagation between UI components (toolbar → panels → canvas).
/// </summary>
public class InputConsumptionFlowTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static MouseState MakeMouse(int x, int y, ButtonState left = ButtonState.Released)
    {
        return new MouseState(x, y, 0, left, ButtonState.Released, ButtonState.Released,
                              ButtonState.Released, ButtonState.Released);
    }

    private static InputEvent MakeClickAt(int x, int y)
    {
        return new InputEvent(
            MakeMouse(x, y, ButtonState.Pressed),
            MakeMouse(x, y, ButtonState.Released));
    }

    // -------------------------------------------------------------------------
    // 1. Toolbar click does not reach canvas
    // -------------------------------------------------------------------------

    [Fact]
    public void ToolbarClick_ConsumesBeforeCanvas()
    {
        // Simulate: toolbar button at (50,30), canvas at (200,50,800,600)
        var toolbarButton = new Rectangle(40, 20, 30, 30);
        var canvasBounds = new Rectangle(200, 50, 800, 600);

        var input = MakeClickAt(50, 30);

        // Toolbar processes first — consumes the click
        bool toolbarConsumed = input.TryConsumeClick(toolbarButton);

        // Canvas processes second — should not consume
        bool canvasConsumed = input.TryConsumeClick(canvasBounds);

        Assert.True(toolbarConsumed);
        Assert.False(canvasConsumed);
        Assert.True(input.Consumed);
    }

    // -------------------------------------------------------------------------
    // 2. PanelDock click does not reach canvas
    // -------------------------------------------------------------------------

    [Fact]
    public void PanelDockClick_ConsumesBeforeCanvas()
    {
        // PanelDock on left (0-200), canvas on right (200+)
        var panelBounds = new Rectangle(0, 50, 200, 600);
        var canvasBounds = new Rectangle(200, 50, 800, 600);

        var input = MakeClickAt(100, 300);

        // PanelDock processes first
        bool panelConsumed = input.TryConsumeClick(panelBounds);

        // Canvas processes second
        bool canvasConsumed = input.TryConsumeClick(canvasBounds);

        Assert.True(panelConsumed);
        Assert.False(canvasConsumed);
    }

    // -------------------------------------------------------------------------
    // 3. Open dropdown popup consumes all clicks globally
    // -------------------------------------------------------------------------

    [Fact]
    public void OpenDropdownPopup_ConsumesAllClicks()
    {
        // Dropdown popup is a modal overlay — calls ConsumeClick() on any click
        var input = MakeClickAt(500, 400);

        // Simulate open dropdown calling ConsumeClick (as Dropdown.Update does)
        input.ConsumeClick();

        // No other component can consume
        Assert.True(input.Consumed);
        Assert.False(input.TryConsumeClick(new Rectangle(0, 0, 1000, 1000)));
    }

    // -------------------------------------------------------------------------
    // 4. Visible context menu consumes all clicks
    // -------------------------------------------------------------------------

    [Fact]
    public void VisibleContextMenu_ConsumesAllClicks()
    {
        // Context menu visible — ConsumeClick() swallows everything
        var input = MakeClickAt(300, 200);

        // Simulate context menu consuming
        input.ConsumeClick();

        // Subsequent controls see consumed
        Assert.True(input.Consumed);
        Assert.False(input.HasUnconsumedClick);
    }

    // -------------------------------------------------------------------------
    // 5. Panel header click prevents content processing
    // -------------------------------------------------------------------------

    [Fact]
    public void PanelHeaderClick_ConsumesBeforeContent()
    {
        // Panel header at top of panel, content below
        var headerBounds = new Rectangle(0, 50, 200, 24);
        var contentBounds = new Rectangle(0, 74, 200, 300);

        var input = MakeClickAt(100, 60);

        // Header processes first
        bool headerConsumed = input.TryConsumeClick(headerBounds);

        // Content processes second
        bool contentConsumed = input.TryConsumeClick(contentBounds);

        Assert.True(headerConsumed);
        Assert.False(contentConsumed);
    }

    // -------------------------------------------------------------------------
    // 6. Canvas click when nothing else consumed works normally
    // -------------------------------------------------------------------------

    [Fact]
    public void CanvasClick_WhenNothingConsumed_ProcessesNormally()
    {
        var canvasBounds = new Rectangle(200, 50, 800, 600);

        var input = MakeClickAt(500, 300);

        // No toolbar or panel consumed
        Assert.False(input.Consumed);
        Assert.True(input.HasUnconsumedClick);

        // Canvas consumes
        bool consumed = input.TryConsumeClick(canvasBounds);
        Assert.True(consumed);
    }

    // -------------------------------------------------------------------------
    // 7. Click outside all components — nothing consumed
    // -------------------------------------------------------------------------

    [Fact]
    public void ClickOutsideAllComponents_NothingConsumed()
    {
        var toolbarBounds = new Rectangle(0, 0, 100, 30);
        var panelBounds = new Rectangle(0, 30, 200, 600);
        var canvasBounds = new Rectangle(200, 30, 800, 600);

        // Click in dead zone outside all bounds
        var input = MakeClickAt(150, 15);

        Assert.False(input.TryConsumeClick(toolbarBounds));
        Assert.False(input.TryConsumeClick(panelBounds));
        Assert.False(input.TryConsumeClick(canvasBounds));
        Assert.False(input.Consumed);
    }

    // -------------------------------------------------------------------------
    // 8. Sequential panel updates — first panel consumes, second doesn't
    // -------------------------------------------------------------------------

    [Fact]
    public void SequentialPanels_FirstConsumes_SecondSkips()
    {
        // Two panels in a dock, overlapping click area (shared InputEvent)
        var panel1Content = new Rectangle(0, 74, 200, 150);
        var panel2Content = new Rectangle(0, 248, 200, 150);

        var input = MakeClickAt(100, 100);

        // Panel 1 content area contains the click
        bool p1 = input.TryConsumeClick(panel1Content);

        // Panel 2 can't consume even though it has the same InputEvent
        bool p2 = input.TryConsumeClick(panel2Content);

        Assert.True(p1);
        Assert.False(p2);
    }

    // -------------------------------------------------------------------------
    // 9. No click (just mouse move) — nothing consumed
    // -------------------------------------------------------------------------

    [Fact]
    public void NoClick_MouseMoveOnly_NothingConsumed()
    {
        // Mouse is not clicking (both frames released)
        var input = new InputEvent(
            MakeMouse(100, 100, ButtonState.Released),
            MakeMouse(90, 90, ButtonState.Released));

        var bounds = new Rectangle(0, 0, 200, 200);
        Assert.False(input.TryConsumeClick(bounds));
        Assert.False(input.Consumed);
        Assert.False(input.HasUnconsumedClick);
    }

    // -------------------------------------------------------------------------
    // 10. Right-click consumption independent of left-click
    // -------------------------------------------------------------------------

    [Fact]
    public void RightClick_ConsumptionIndependent()
    {
        // Right-click (for context menus)
        var input = new InputEvent(
            new MouseState(100, 100, 0, ButtonState.Released, ButtonState.Released,
                           ButtonState.Pressed, ButtonState.Released, ButtonState.Released),
            new MouseState(100, 100, 0, ButtonState.Released, ButtonState.Released,
                           ButtonState.Released, ButtonState.Released, ButtonState.Released));

        var bounds = new Rectangle(50, 50, 100, 100);

        // Right-click consumes
        bool consumed = input.TryConsumeRightClick(bounds);
        Assert.True(consumed);

        // Second attempt fails
        Assert.False(input.TryConsumeRightClick(bounds));
    }

    // -------------------------------------------------------------------------
    // 11. Full flow: toolbar → panelDock → canvas simulation
    // -------------------------------------------------------------------------

    [Fact]
    public void FullFlow_ToolbarConsumed_PanelAndCanvasSkipped()
    {
        // Simulates the TileForgeGame.Update flow
        var toolbarRect = new Rectangle(10, 26, 30, 30);
        var panelRect = new Rectangle(0, 56, 200, 600);
        var canvasRect = new Rectangle(200, 56, 800, 600);

        var input = MakeClickAt(25, 40); // Click on toolbar button

        // Step 1: Toolbar
        bool toolbar = input.TryConsumeClick(toolbarRect);

        // Step 2: PanelDock (header + content)
        bool panelHeader = input.TryConsumeClick(new Rectangle(0, 56, 200, 24));
        bool panelContent = input.TryConsumeClick(panelRect);

        // Step 3: Canvas
        bool canvas = input.TryConsumeClick(canvasRect);

        Assert.True(toolbar);
        Assert.False(panelHeader);
        Assert.False(panelContent);
        Assert.False(canvas);
    }

    [Fact]
    public void FullFlow_PanelConsumed_CanvasSkipped()
    {
        var toolbarRect = new Rectangle(10, 26, 30, 30);
        var panelContentRect = new Rectangle(0, 80, 200, 500);
        var canvasRect = new Rectangle(200, 56, 800, 600);

        var input = MakeClickAt(100, 300); // Click in panel area

        // Step 1: Toolbar — click outside its bounds
        bool toolbar = input.TryConsumeClick(toolbarRect);

        // Step 2: PanelDock content — consumes
        bool panel = input.TryConsumeClick(panelContentRect);

        // Step 3: Canvas — skipped
        bool canvas = input.TryConsumeClick(canvasRect);

        Assert.False(toolbar);
        Assert.True(panel);
        Assert.False(canvas);
    }

    // -------------------------------------------------------------------------
    // 13. Checkbox with InputEvent consumption
    // -------------------------------------------------------------------------

    [Fact]
    public void Checkbox_ConsumesClick_OtherControlSkips()
    {
        var checkboxBounds = new Rectangle(10, 10, 20, 20);
        var otherBounds = new Rectangle(10, 10, 20, 20); // Same area

        var input = MakeClickAt(15, 15);

        var checkbox = new Checkbox();
        bool toggled = checkbox.Update(input, checkboxBounds);

        // Checkbox consumed the click
        Assert.True(toggled);
        Assert.True(input.Consumed);

        // Another control at same position can't consume
        Assert.False(input.TryConsumeClick(otherBounds));
    }
}
