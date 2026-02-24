using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Editor.Tools;
using Xunit;

namespace TileForge.Tests;

/// <summary>
/// Tests for InputRouter keyboard shortcut processing.
///
/// MonoGame's KeyboardState constructor accepts params Keys[] and can be constructed
/// without a running game. MouseState has a default constructor. This allows us to
/// test all keyboard-driven logic in InputRouter.
///
/// Note: InputRouter.Update takes (KeyboardState current, KeyboardState prev, MouseState mouse).
/// A key is "pressed" when current.IsKeyDown(key) && prev.IsKeyUp(key).
/// </summary>
public class InputRouterTests
{
    /// <summary>
    /// Helper: creates EditorState with a map so that layer/entity operations don't early-return.
    /// </summary>
    private static EditorState CreateStateWithMap()
    {
        var state = new EditorState
        {
            Map = new MapData(10, 10),
        };
        return state;
    }

    /// <summary>
    /// Creates an InputRouter with the given state and callback trackers.
    /// </summary>
    private static (InputRouter router, CallbackTracker tracker) CreateRouter(EditorState state)
    {
        var tracker = new CallbackTracker();
        var router = new InputRouter(
            state,
            save: () => tracker.SaveCalled = true,
            open: () => tracker.OpenCalled = true,
            enterPlayMode: () => tracker.EnterPlayModeCalled = true,
            exitPlayMode: () => tracker.ExitPlayModeCalled = true,
            exitGame: () => tracker.ExitGameCalled = true,
            resizeMap: () => { });
        return (router, tracker);
    }

    /// <summary>
    /// Simulates a single key press: prev has key up, current has key down.
    /// </summary>
    private static (KeyboardState current, KeyboardState prev) SimulateKeyPress(params Keys[] keys)
    {
        var current = new KeyboardState(keys);
        var prev = new KeyboardState(); // all keys up
        return (current, prev);
    }

    /// <summary>
    /// Simulates a key press with modifier keys held down.
    /// </summary>
    private static (KeyboardState current, KeyboardState prev) SimulateKeyPressWithModifier(
        Keys modifier, Keys key)
    {
        var current = new KeyboardState(modifier, key);
        var prev = new KeyboardState(modifier); // modifier already held, key was up
        return (current, prev);
    }

    // ===== Ctrl+S Save =====

    [Fact]
    public void Update_CtrlS_CallsSave()
    {
        var state = CreateStateWithMap();
        var (router, tracker) = CreateRouter(state);
        var (current, prev) = SimulateKeyPressWithModifier(Keys.LeftControl, Keys.S);

        router.Update(current, prev, default);

        Assert.True(tracker.SaveCalled);
    }

    [Fact]
    public void Update_CtrlS_ReturnsTrue()
    {
        var state = CreateStateWithMap();
        var (router, tracker) = CreateRouter(state);
        var (current, prev) = SimulateKeyPressWithModifier(Keys.LeftControl, Keys.S);

        bool result = router.Update(current, prev, default);

        Assert.True(result);
    }

    // ===== Ctrl+O Open =====

    [Fact]
    public void Update_CtrlO_CallsOpen()
    {
        var state = CreateStateWithMap();
        var (router, tracker) = CreateRouter(state);
        var (current, prev) = SimulateKeyPressWithModifier(Keys.LeftControl, Keys.O);

        router.Update(current, prev, default);

        Assert.True(tracker.OpenCalled);
    }

    // ===== Ctrl+Z Undo =====

    [Fact]
    public void Update_CtrlZ_CallsUndo()
    {
        var state = CreateStateWithMap();
        // Push a command so undo has something to do
        var cmd = new TileForge.Tests.Editor.MockCommand();
        state.UndoStack.Push(cmd);
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPressWithModifier(Keys.LeftControl, Keys.Z);

        router.Update(current, prev, default);

        Assert.Equal(1, cmd.UndoCount);
    }

    [Fact]
    public void Update_CtrlZ_ReturnsTrue()
    {
        var state = CreateStateWithMap();
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPressWithModifier(Keys.LeftControl, Keys.Z);

        bool result = router.Update(current, prev, default);

        Assert.True(result);
    }

    // ===== Ctrl+Y Redo =====

    [Fact]
    public void Update_CtrlY_CallsRedo()
    {
        var state = CreateStateWithMap();
        var cmd = new TileForge.Tests.Editor.MockCommand();
        state.UndoStack.Push(cmd);
        state.UndoStack.Undo(); // now on redo stack
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPressWithModifier(Keys.LeftControl, Keys.Y);

        router.Update(current, prev, default);

        Assert.Equal(1, cmd.ExecuteCount); // Redo re-executes
    }

    // ===== Tool Keybinds =====

    [Fact]
    public void Update_B_SetsBrushTool()
    {
        var state = CreateStateWithMap();
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.B);

        router.Update(current, prev, default);

        Assert.IsType<BrushTool>(state.ActiveTool);
    }

    [Fact]
    public void Update_E_SetsEraserTool()
    {
        var state = CreateStateWithMap();
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.E);

        router.Update(current, prev, default);

        Assert.IsType<EraserTool>(state.ActiveTool);
    }

    [Fact]
    public void Update_F_SetsFillTool()
    {
        var state = CreateStateWithMap();
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.F);

        router.Update(current, prev, default);

        Assert.IsType<FillTool>(state.ActiveTool);
    }

    [Fact]
    public void Update_N_SetsEntityTool()
    {
        var state = CreateStateWithMap();
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.N);

        router.Update(current, prev, default);

        Assert.IsType<EntityTool>(state.ActiveTool);
    }

    // ===== Tool keybinds do not fire in play mode =====

    [Fact]
    public void Update_B_InPlayMode_DoesNotChangeTool()
    {
        var state = CreateStateWithMap();
        state.IsPlayMode = true;
        state.ActiveTool = new EraserTool(); // set to something other than BrushTool
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.B);

        router.Update(current, prev, default);

        // Should still be EraserTool since play mode skips editor keybinds
        Assert.IsType<EraserTool>(state.ActiveTool);
    }

    // ===== F5 Play Mode =====

    [Fact]
    public void Update_F5_WhenNotInPlayMode_CallsEnterPlayMode()
    {
        var state = CreateStateWithMap();
        var (router, tracker) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.F5);

        router.Update(current, prev, default);

        Assert.True(tracker.EnterPlayModeCalled);
        Assert.False(tracker.ExitPlayModeCalled);
    }

    [Fact]
    public void Update_F5_WhenInPlayMode_CallsExitPlayMode()
    {
        var state = CreateStateWithMap();
        state.IsPlayMode = true;
        var (router, tracker) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.F5);

        router.Update(current, prev, default);

        Assert.True(tracker.ExitPlayModeCalled);
        Assert.False(tracker.EnterPlayModeCalled);
    }

    [Fact]
    public void Update_F5_ReturnsTrue()
    {
        var state = CreateStateWithMap();
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.F5);

        bool result = router.Update(current, prev, default);

        Assert.True(result);
    }

    // ===== Escape =====

    [Fact]
    public void Update_Escape_InPlayMode_PassesThrough()
    {
        // Escape in play mode is handled by ScreenManager (opens PauseScreen),
        // not by InputRouter. InputRouter returns false so the key reaches the game loop.
        var state = CreateStateWithMap();
        state.IsPlayMode = true;
        var (router, tracker) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.Escape);

        bool consumed = router.Update(current, prev, default);

        Assert.False(consumed);
        Assert.False(tracker.ExitPlayModeCalled);
    }

    [Fact]
    public void Update_Escape_WithSelectedEntity_DeselectsEntity()
    {
        var state = CreateStateWithMap();
        state.SelectedEntityId = "entity123";
        var (router, tracker) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.Escape);

        router.Update(current, prev, default);

        Assert.Null(state.SelectedEntityId);
        Assert.False(tracker.ExitGameCalled);
    }

    [Fact]
    public void Update_Escape_NoPlayModeNoEntity_CallsExitGame()
    {
        var state = CreateStateWithMap();
        var (router, tracker) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.Escape);

        router.Update(current, prev, default);

        Assert.True(tracker.ExitGameCalled);
    }

    [Fact]
    public void Update_Escape_ReturnsTrue()
    {
        var state = CreateStateWithMap();
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.Escape);

        bool result = router.Update(current, prev, default);

        Assert.True(result);
    }

    // ===== Tab Layer Switching =====

    [Fact]
    public void Update_Tab_CyclesToNextLayer()
    {
        var state = CreateStateWithMap();
        // Default map has Ground and Objects layers; active is Ground
        state.ActiveLayerName = "Ground";
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.Tab);

        router.Update(current, prev, default);

        Assert.Equal("Objects", state.ActiveLayerName);
    }

    [Fact]
    public void Update_Tab_WrapsAroundToFirstLayer()
    {
        var state = CreateStateWithMap();
        state.ActiveLayerName = "Objects"; // last layer
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.Tab);

        router.Update(current, prev, default);

        Assert.Equal("Ground", state.ActiveLayerName);
    }

    [Fact]
    public void Update_Tab_SingleLayer_DoesNotCycle()
    {
        var state = new EditorState
        {
            Map = new MapData(10, 10),
        };
        // Remove the second layer to leave only one
        state.Map.Layers.RemoveAt(1);
        state.ActiveLayerName = "Ground";
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.Tab);

        router.Update(current, prev, default);

        // Should remain on Ground since there's only one layer
        Assert.Equal("Ground", state.ActiveLayerName);
    }

    // ===== V Layer Visibility Toggle =====

    [Fact]
    public void Update_V_TogglesActiveLayerVisibility()
    {
        var state = CreateStateWithMap();
        state.ActiveLayerName = "Ground";
        Assert.True(state.ActiveLayer.Visible); // default is visible
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.V);

        router.Update(current, prev, default);

        Assert.False(state.ActiveLayer.Visible);
    }

    [Fact]
    public void Update_V_TogglesVisibilityBackToTrue()
    {
        var state = CreateStateWithMap();
        state.ActiveLayerName = "Ground";
        state.ActiveLayer.Visible = false;
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.V);

        router.Update(current, prev, default);

        Assert.True(state.ActiveLayer.Visible);
    }

    // ===== Delete Selected Entity =====

    [Fact]
    public void Update_Delete_WithSelectedEntity_RemovesEntity()
    {
        var state = CreateStateWithMap();
        var entity = new Entity { GroupName = "chest", X = 5, Y = 5 };
        state.Map.Entities.Add(entity);
        state.SelectedEntityId = entity.Id;
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.Delete);

        router.Update(current, prev, default);

        Assert.DoesNotContain(entity, state.Map.Entities);
        Assert.Null(state.SelectedEntityId);
    }

    [Fact]
    public void Update_Delete_WithSelectedEntity_PushesUndoCommand()
    {
        var state = CreateStateWithMap();
        var entity = new Entity { GroupName = "chest", X = 5, Y = 5 };
        state.Map.Entities.Add(entity);
        state.SelectedEntityId = entity.Id;
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.Delete);

        router.Update(current, prev, default);

        Assert.True(state.UndoStack.CanUndo);
    }

    [Fact]
    public void Update_Delete_NoSelectedEntity_DoesNothing()
    {
        var state = CreateStateWithMap();
        var entity = new Entity { GroupName = "chest", X = 5, Y = 5 };
        state.Map.Entities.Add(entity);
        state.SelectedEntityId = null;
        var (router, _) = CreateRouter(state);
        var (current, prev) = SimulateKeyPress(Keys.Delete);

        router.Update(current, prev, default);

        Assert.Contains(entity, state.Map.Entities);
    }

    // ===== Auto Tool Switch =====

    [Fact]
    public void Update_SelectedEntityGroup_AutoSwitchesToEntityTool()
    {
        var state = CreateStateWithMap();
        var group = new TileGroup { Name = "door", Type = GroupType.Entity };
        state.AddGroup(group);
        state.SelectedGroupName = "door";
        state.ActiveTool = new BrushTool(); // not EntityTool
        var (router, _) = CreateRouter(state);

        // A no-op frame (no keys pressed) still triggers auto-switch logic
        var current = new KeyboardState();
        var prev = new KeyboardState();
        router.Update(current, prev, default);

        Assert.IsType<EntityTool>(state.ActiveTool);
    }

    [Fact]
    public void Update_SelectedTileGroup_AutoSwitchesFromEntityToolToBrush()
    {
        var state = CreateStateWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);
        state.SelectedGroupName = "grass";
        state.ActiveTool = new EntityTool(); // currently EntityTool
        var (router, _) = CreateRouter(state);

        var current = new KeyboardState();
        var prev = new KeyboardState();
        router.Update(current, prev, default);

        Assert.IsType<BrushTool>(state.ActiveTool);
    }

    [Fact]
    public void Update_SelectedTileGroup_DoesNotSwitchIfAlreadyBrush()
    {
        var state = CreateStateWithMap();
        var group = new TileGroup { Name = "grass", Type = GroupType.Tile };
        state.AddGroup(group);
        state.SelectedGroupName = "grass";
        var brushTool = new BrushTool();
        state.ActiveTool = brushTool;
        var (router, _) = CreateRouter(state);

        var current = new KeyboardState();
        var prev = new KeyboardState();
        router.Update(current, prev, default);

        // ActiveTool should still be a BrushTool (may be same instance or new, either is fine)
        Assert.IsType<BrushTool>(state.ActiveTool);
    }

    // ===== No keys pressed returns false =====

    [Fact]
    public void Update_NoKeysPressed_ReturnsFalse()
    {
        var state = CreateStateWithMap();
        var (router, _) = CreateRouter(state);
        var current = new KeyboardState();
        var prev = new KeyboardState();

        bool result = router.Update(current, prev, default);

        Assert.False(result);
    }

    // ===== Right control works as modifier =====

    [Fact]
    public void Update_RightCtrlS_CallsSave()
    {
        var state = CreateStateWithMap();
        var (router, tracker) = CreateRouter(state);
        var current = new KeyboardState(Keys.RightControl, Keys.S);
        var prev = new KeyboardState(Keys.RightControl);

        router.Update(current, prev, default);

        Assert.True(tracker.SaveCalled);
    }

    // ===== Key held across frames does not re-trigger =====

    [Fact]
    public void Update_KeyHeldAcrossFrames_DoesNotRetrigger()
    {
        var state = CreateStateWithMap();
        var (router, tracker) = CreateRouter(state);
        // First frame: key pressed
        var frame1Current = new KeyboardState(Keys.F5);
        var frame1Prev = new KeyboardState();
        router.Update(frame1Current, frame1Prev, default);
        Assert.True(tracker.EnterPlayModeCalled);

        // Reset tracker
        tracker.EnterPlayModeCalled = false;

        // Second frame: key still held (both current and prev have it down)
        var frame2Current = new KeyboardState(Keys.F5);
        var frame2Prev = new KeyboardState(Keys.F5);
        router.Update(frame2Current, frame2Prev, default);

        // Should NOT re-trigger since key was already down in prev
        Assert.False(tracker.EnterPlayModeCalled);
    }
}

/// <summary>
/// Tracks which callbacks were called by InputRouter.
/// </summary>
public class CallbackTracker
{
    public bool SaveCalled { get; set; }
    public bool OpenCalled { get; set; }
    public bool EnterPlayModeCalled { get; set; }
    public bool ExitPlayModeCalled { get; set; }
    public bool ExitGameCalled { get; set; }
}
