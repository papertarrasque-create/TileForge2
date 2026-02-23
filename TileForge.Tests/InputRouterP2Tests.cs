using Microsoft.Xna.Framework.Input;
using TileForge.Data;
using TileForge.Editor;
using Xunit;

namespace TileForge.Tests;

/// <summary>
/// Tests for P2 keyboard shortcuts added to InputRouter:
/// Ctrl+Shift+O (open recent) and Ctrl+N (new project).
/// </summary>
public class InputRouterP2Tests
{
    private static EditorState CreateStateWithMap()
    {
        return new EditorState { Map = new MapData(10, 10) };
    }

    private static (InputRouter router, P2CallbackTracker tracker) CreateRouterWithP2(EditorState state)
    {
        var tracker = new P2CallbackTracker();
        var router = new InputRouter(
            state,
            save: () => tracker.SaveCalled = true,
            open: () => tracker.OpenCalled = true,
            enterPlayMode: () => { },
            exitPlayMode: () => { },
            exitGame: () => { },
            resizeMap: () => { },
            openRecent: () => tracker.OpenRecentCalled = true,
            newProject: () => tracker.NewProjectCalled = true);
        return (router, tracker);
    }

    // ===== Ctrl+Shift+O (Open Recent) =====

    [Fact]
    public void Update_CtrlShiftO_CallsOpenRecent()
    {
        var state = CreateStateWithMap();
        var (router, tracker) = CreateRouterWithP2(state);
        var current = new KeyboardState(Keys.LeftControl, Keys.LeftShift, Keys.O);
        var prev = new KeyboardState(Keys.LeftControl, Keys.LeftShift);

        router.Update(current, prev, default);

        Assert.True(tracker.OpenRecentCalled);
        Assert.False(tracker.OpenCalled); // Should NOT trigger plain Ctrl+O
    }

    [Fact]
    public void Update_CtrlShiftO_ReturnsTrue()
    {
        var state = CreateStateWithMap();
        var (router, _) = CreateRouterWithP2(state);
        var current = new KeyboardState(Keys.LeftControl, Keys.LeftShift, Keys.O);
        var prev = new KeyboardState(Keys.LeftControl, Keys.LeftShift);

        bool result = router.Update(current, prev, default);

        Assert.True(result);
    }

    [Fact]
    public void Update_CtrlShiftO_WithNullCallback_DoesNotThrow()
    {
        var state = CreateStateWithMap();
        // Use the old constructor signature (openRecent defaults to null)
        var router = new InputRouter(state,
            save: () => { }, open: () => { },
            enterPlayMode: () => { }, exitPlayMode: () => { },
            exitGame: () => { }, resizeMap: () => { });

        var current = new KeyboardState(Keys.LeftControl, Keys.LeftShift, Keys.O);
        var prev = new KeyboardState(Keys.LeftControl, Keys.LeftShift);

        var ex = Record.Exception(() => router.Update(current, prev, default));

        Assert.Null(ex);
    }

    // ===== Ctrl+N (New Project) =====

    [Fact]
    public void Update_CtrlN_CallsNewProject()
    {
        var state = CreateStateWithMap();
        var (router, tracker) = CreateRouterWithP2(state);
        var current = new KeyboardState(Keys.LeftControl, Keys.N);
        var prev = new KeyboardState(Keys.LeftControl);

        router.Update(current, prev, default);

        Assert.True(tracker.NewProjectCalled);
    }

    [Fact]
    public void Update_CtrlN_ReturnsTrue()
    {
        var state = CreateStateWithMap();
        var (router, _) = CreateRouterWithP2(state);
        var current = new KeyboardState(Keys.LeftControl, Keys.N);
        var prev = new KeyboardState(Keys.LeftControl);

        bool result = router.Update(current, prev, default);

        Assert.True(result);
    }

    [Fact]
    public void Update_CtrlN_WithNullCallback_DoesNotThrow()
    {
        var state = CreateStateWithMap();
        var router = new InputRouter(state,
            save: () => { }, open: () => { },
            enterPlayMode: () => { }, exitPlayMode: () => { },
            exitGame: () => { }, resizeMap: () => { });

        var current = new KeyboardState(Keys.LeftControl, Keys.N);
        var prev = new KeyboardState(Keys.LeftControl);

        var ex = Record.Exception(() => router.Update(current, prev, default));

        Assert.Null(ex);
    }

    // ===== Plain N key (no Ctrl) still sets EntityTool =====

    [Fact]
    public void Update_N_WithoutCtrl_SetsEntityTool()
    {
        var state = CreateStateWithMap();
        var (router, tracker) = CreateRouterWithP2(state);
        var current = new KeyboardState(Keys.N);
        var prev = new KeyboardState();

        router.Update(current, prev, default);

        Assert.False(tracker.NewProjectCalled);
        Assert.IsType<TileForge.Editor.Tools.EntityTool>(state.ActiveTool);
    }
}

public class P2CallbackTracker
{
    public bool SaveCalled { get; set; }
    public bool OpenCalled { get; set; }
    public bool OpenRecentCalled { get; set; }
    public bool NewProjectCalled { get; set; }
}
