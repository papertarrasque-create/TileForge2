using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class PauseScreenTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static SaveManager CreateSaveManager()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tileforge_pause_tests_" + Guid.NewGuid().ToString("N"));
        return new SaveManager(dir);
    }

    private static (PauseScreen screen, ScreenManager manager) CreatePauseScreen()
    {
        var saveManager = CreateSaveManager();
        var gameStateManager = new GameStateManager();
        var inputManager = new GameInputManager();
        var bindingsPath = Path.Combine(Path.GetTempPath(), "tileforge_pause_bindings_" + Guid.NewGuid().ToString("N"), "keybindings.json");
        var manager = new ScreenManager();
        var screen = new PauseScreen(saveManager, gameStateManager, inputManager, bindingsPath);
        manager.Push(screen);
        return (screen, manager);
    }

    private static GameInputManager SimulateKeyPress(Keys key)
    {
        var input = new GameInputManager();
        input.Update(new KeyboardState());
        input.Update(new KeyboardState(key));
        return input;
    }

    private static readonly GameTime DefaultGameTime =
        new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    // =========================================================================
    // IsOverlay
    // =========================================================================

    [Fact]
    public void IsOverlay_ReturnsTrue()
    {
        var saveManager = CreateSaveManager();
        var gameStateManager = new GameStateManager();
        var inputManager = new GameInputManager();
        var screen = new PauseScreen(saveManager, gameStateManager, inputManager, "/tmp/test.json");

        Assert.True(screen.IsOverlay);
    }

    // =========================================================================
    // Menu: Resume (0), Save Game (1), Load Game (2), Settings (3), Return to Editor (4)
    // =========================================================================

    [Fact]
    public void PauseScreen_HasFiveMenuItems()
    {
        // Navigate down 4 times from index 0 to reach "Return to Editor" (index 4)
        var (screen, manager) = CreatePauseScreen();

        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.True(manager.ExitRequested);
        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Resume (Interact at index 0)
    // =========================================================================

    [Fact]
    public void DefaultSelectedIndex_IsZero_InteractImmediatelyPopsScreen()
    {
        var (screen, manager) = CreatePauseScreen();
        var input = SimulateKeyPress(Keys.Z);

        screen.Update(DefaultGameTime, input);

        Assert.False(manager.HasScreens);
    }

    [Fact]
    public void Interact_AtIndexZero_PopsScreen()
    {
        var (screen, manager) = CreatePauseScreen();
        var input = SimulateKeyPress(Keys.Z);

        screen.Update(DefaultGameTime, input);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Save Game (index 1) — pushes SaveLoadScreen
    // =========================================================================

    [Fact]
    public void MoveDown_ThenInteract_PushesSaveLoadScreen()
    {
        var (screen, manager) = CreatePauseScreen();

        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.True(manager.HasScreens);
        Assert.False(manager.ExitRequested);
    }

    // =========================================================================
    // Settings (index 3) — pushes SettingsScreen
    // =========================================================================

    [Fact]
    public void MoveDownThreeTimes_ThenInteract_PushesSettingsScreen()
    {
        var (screen, manager) = CreatePauseScreen();

        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        // SettingsScreen pushed on top — manager still has screens
        Assert.True(manager.HasScreens);
        Assert.False(manager.ExitRequested);
    }

    // =========================================================================
    // Return to Editor (index 4)
    // =========================================================================

    [Fact]
    public void MoveDownFourTimes_ThenInteract_SetsExitRequestedAndClearsScreens()
    {
        var (screen, manager) = CreatePauseScreen();

        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.True(manager.ExitRequested);
        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Cancel / Pause resumes
    // =========================================================================

    [Fact]
    public void Cancel_PopsScreen()
    {
        var (screen, manager) = CreatePauseScreen();
        var input = SimulateKeyPress(Keys.X);

        screen.Update(DefaultGameTime, input);

        Assert.False(manager.HasScreens);
    }

    [Fact]
    public void Pause_PopsScreen()
    {
        var (screen, manager) = CreatePauseScreen();
        var input = SimulateKeyPress(Keys.Escape);

        screen.Update(DefaultGameTime, input);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Navigation wrapping
    // =========================================================================

    [Fact]
    public void MoveUp_FromIndexZero_WrapsToLastItem()
    {
        // Wraps to index 4 ("Return to Editor")
        var (screen, manager) = CreatePauseScreen();

        var moveUpInput = SimulateKeyPress(Keys.Up);
        screen.Update(DefaultGameTime, moveUpInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.True(manager.ExitRequested);
        Assert.False(manager.HasScreens);
    }

    [Fact]
    public void MoveDown_FromLastItem_WrapsToResume()
    {
        // Navigate to index 4, then one more down wraps to 0 (Resume)
        var (screen, manager) = CreatePauseScreen();

        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput);
        screen.Update(DefaultGameTime, moveDownInput); // wraps to 0

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.False(manager.HasScreens);
        Assert.False(manager.ExitRequested);
    }
}
