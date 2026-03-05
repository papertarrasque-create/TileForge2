using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class GameOverScreenTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static (GameOverScreen screen, ScreenManager manager, GameStateManager gameStateManager) CreateGameOverScreen()
    {
        var gameStateManager = new GameStateManager();
        var manager = new ScreenManager();
        var screen = new GameOverScreen(gameStateManager);
        manager.Push(screen);
        return (screen, manager, gameStateManager);
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
    // Construction
    // =========================================================================

    [Fact]
    public void GameOverScreen_CanBeConstructed()
    {
        var gameStateManager = new GameStateManager();
        var screen = new GameOverScreen(gameStateManager);
        Assert.NotNull(screen);
    }

    // =========================================================================
    // IsOverlay
    // =========================================================================

    [Fact]
    public void GameOverScreen_IsOverlay_ReturnsFalse()
    {
        var gameStateManager = new GameStateManager();
        var screen = new GameOverScreen(gameStateManager);
        Assert.False(screen.IsOverlay);
    }

    // =========================================================================
    // Restart (index 0)
    // =========================================================================

    [Fact]
    public void GameOverScreen_Restart_SetsRestartRequested()
    {
        var (screen, manager, gameStateManager) = CreateGameOverScreen();

        // Index 0 (Restart) is already selected by default
        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.True(gameStateManager.RestartRequested);
    }

    [Fact]
    public void GameOverScreen_Restart_ClearsScreenStack()
    {
        var (screen, manager, gameStateManager) = CreateGameOverScreen();

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Return to Editor (index 1)
    // =========================================================================

    [Fact]
    public void GameOverScreen_ReturnToEditor_SetsExitRequested()
    {
        var (screen, manager, gameStateManager) = CreateGameOverScreen();

        // Navigate down to index 1 (Return to Editor)
        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.True(manager.ExitRequested);
    }

    [Fact]
    public void GameOverScreen_ReturnToEditor_ClearsScreenStack()
    {
        var (screen, manager, gameStateManager) = CreateGameOverScreen();

        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Navigation
    // =========================================================================

    [Fact]
    public void GameOverScreen_MoveDown_CyclesSelection()
    {
        // Press Down once from index 0 → index 1 (Return to Editor)
        var (screen, manager, gameStateManager) = CreateGameOverScreen();

        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        // Should trigger Return to Editor, not Restart
        Assert.True(manager.ExitRequested);
        Assert.False(gameStateManager.RestartRequested);
    }

    [Fact]
    public void GameOverScreen_MoveUp_WrapsToLastItem()
    {
        // Press Up from index 0 → wraps to index 1 (Return to Editor)
        var (screen, manager, gameStateManager) = CreateGameOverScreen();

        var moveUpInput = SimulateKeyPress(Keys.Up);
        screen.Update(DefaultGameTime, moveUpInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.True(manager.ExitRequested);
        Assert.False(gameStateManager.RestartRequested);
    }

    // =========================================================================
    // Cancel key
    // =========================================================================

    [Fact]
    public void GameOverScreen_Cancel_TriggersReturnToEditor()
    {
        var (screen, manager, gameStateManager) = CreateGameOverScreen();

        var cancelInput = SimulateKeyPress(Keys.X);
        screen.Update(DefaultGameTime, cancelInput);

        Assert.True(manager.ExitRequested);
        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Wrap from last to first
    // =========================================================================

    [Fact]
    public void GameOverScreen_MoveDown_WrapsFromLastToFirst()
    {
        // Press Down twice from index 0: 0 → 1 → 0 (wraps back)
        var (screen, manager, gameStateManager) = CreateGameOverScreen();

        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput); // now index 1
        screen.Update(DefaultGameTime, moveDownInput); // wraps back to index 0

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        // Should trigger Restart (index 0), not Return to Editor
        Assert.True(gameStateManager.RestartRequested);
        Assert.False(manager.ExitRequested);
    }
}
