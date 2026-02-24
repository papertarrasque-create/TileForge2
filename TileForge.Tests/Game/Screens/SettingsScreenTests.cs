using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using TileForge.Game.Screens;
using DojoUI;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class SettingsScreenTests : IDisposable
{
    // =========================================================================
    // Setup / teardown
    // =========================================================================

    private readonly string _tempDir;
    private readonly string _bindingsPath;

    public SettingsScreenTests()
    {
        _tempDir = Path.Combine(
            Path.GetTempPath(),
            "tileforge_settings_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _bindingsPath = Path.Combine(_tempDir, "keybindings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // =========================================================================
    // Helpers — identical idiom to PauseScreenTests
    // =========================================================================

    /// <summary>
    /// Creates a SettingsScreen, pushes it onto a fresh ScreenManager, and
    /// returns both. Push sets ScreenManager on the screen and calls OnEnter.
    /// </summary>
    private (SettingsScreen screen, ScreenManager manager) CreateSettingsScreen()
    {
        var mgr = new GameInputManager();
        var screen = new SettingsScreen(mgr, _bindingsPath);
        var screenManager = new ScreenManager();
        screenManager.Push(screen);
        return (screen, screenManager);
    }

    /// <summary>
    /// Creates a SettingsScreen with an explicit GameInputManager so tests can
    /// also inspect/modify bindings.
    /// </summary>
    private (SettingsScreen screen, ScreenManager manager, GameInputManager input)
        CreateSettingsScreenWithInput()
    {
        var input = new GameInputManager();
        var screen = new SettingsScreen(input, _bindingsPath);
        var screenManager = new ScreenManager();
        screenManager.Push(screen);
        return (screen, screenManager, input);
    }

    /// <summary>
    /// Simulates a single just-pressed key:
    ///   frame 1 — no keys (establishes baseline);
    ///   frame 2 — key held (JustPressed = true).
    /// Returns the input manager so callers can pass it straight to Update.
    /// </summary>
    private static GameInputManager SimulateKeyPress(Keys key)
    {
        var input = new GameInputManager();
        input.Update(new KeyboardState());
        input.Update(new KeyboardState(key));
        return input;
    }

    private static readonly GameTime DefaultGameTime =
        new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    // Simple screen used as a stack base so Pop() leaves one screen behind
    private class DummyScreen : GameScreen
    {
        public override void Update(GameTime gameTime, GameInputManager input) { }
        public override void Draw(SpriteBatch spriteBatch, SpriteFont font,
            Renderer renderer, Rectangle canvasBounds) { }
    }

    // =========================================================================
    // Construction
    // =========================================================================

    [Fact]
    public void SettingsScreen_CanBeConstructed()
    {
        var input = new GameInputManager();
        var screen = new SettingsScreen(input, _bindingsPath);
        Assert.NotNull(screen);
    }

    // =========================================================================
    // IsOverlay
    // =========================================================================

    [Fact]
    public void IsOverlay_ReturnsTrue()
    {
        var input = new GameInputManager();
        var screen = new SettingsScreen(input, _bindingsPath);
        Assert.True(screen.IsOverlay);
    }

    // =========================================================================
    // Cancel pops the screen
    // =========================================================================

    [Fact]
    public void Cancel_PopsScreen()
    {
        var (screen, manager) = CreateSettingsScreen();
        var input = SimulateKeyPress(Keys.X);   // Cancel key

        screen.Update(DefaultGameTime, input);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // MoveUp wraps from top to bottom
    // =========================================================================

    [Fact]
    public void MoveUp_FromIndexZero_WrapsToLastItem_WhichIsBack()
    {
        // There are 9 actions + Reset + Back = 11 items.
        // Starting at index 0, MoveUp should wrap to index 10 (Back).
        // Pressing Interact at Back should pop the screen.
        var (screen, manager) = CreateSettingsScreen();

        // Navigate up → wraps to Back (index 10)
        var moveUp = SimulateKeyPress(Keys.Up);
        screen.Update(DefaultGameTime, moveUp);

        // Interact at Back → pop
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // MoveDown navigation
    // =========================================================================

    [Fact]
    public void MoveDown_Then_MoveUp_ReturnToIndexZero()
    {
        // Verify round-trip navigation leaves the selection where it started.
        // We can confirm this by pressing Interact at index 0, which should NOT
        // pop the screen (it starts a key-capture for the first action), so
        // HasScreens remains true.
        var (screen, manager) = CreateSettingsScreen();

        var down = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, down);

        var up = SimulateKeyPress(Keys.Up);
        screen.Update(DefaultGameTime, up);

        // Index is back at 0. Pressing Interact starts key-capture (not pop).
        // The screen should still be on the stack.
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        Assert.True(manager.HasScreens);
    }

    // =========================================================================
    // Interact at Back item (last index) pops screen
    // =========================================================================

    [Fact]
    public void Interact_AtBackItem_PopsScreen()
    {
        var (screen, manager) = CreateSettingsScreen();

        // 9 actions + Reset = 10 navigations down bring us to Back (index 10)
        var down = SimulateKeyPress(Keys.Down);
        for (int i = 0; i < 10; i++)
            screen.Update(DefaultGameTime, down);

        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Interact at Reset Defaults item (second-to-last) saves bindings
    // =========================================================================

    [Fact]
    public void Interact_AtResetItem_SavesBindingsFile()
    {
        var (screen, manager, input) = CreateSettingsScreenWithInput();

        // Rebind something so the file won't just contain defaults
        input.RebindAction(GameAction.MoveUp, Keys.W);

        // 9 actions navigations down bring us to Reset (index 9)
        var down = SimulateKeyPress(Keys.Down);
        for (int i = 0; i < 9; i++)
            screen.Update(DefaultGameTime, down);

        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        // File should have been written
        Assert.True(File.Exists(_bindingsPath));
        // Screen should still be open (Reset does not pop)
        Assert.True(manager.HasScreens);
    }

    // =========================================================================
    // MoveDown wraps from bottom back to top
    // =========================================================================

    [Fact]
    public void MoveDown_FromLastIndex_WrapsToIndexZero()
    {
        // Navigate to the last item (Back, index 10) then press MoveDown.
        // The selection should wrap to 0 and Interact at 0 should start
        // key-capture (not pop), so the screen remains on the stack.
        var (screen, manager) = CreateSettingsScreen();

        // Go to Back (index 10) via MoveUp wrap
        var up = SimulateKeyPress(Keys.Up);
        screen.Update(DefaultGameTime, up);

        // MoveDown should wrap back to index 0
        var down = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, down);

        // At index 0, Interact starts key-capture — screen stays
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        Assert.True(manager.HasScreens);
    }

    // =========================================================================
    // Back item pops on top of a non-empty stack
    // =========================================================================

    [Fact]
    public void Back_LeavesUnderlyingScreenIntact()
    {
        // Push a dummy screen first so the stack has two items.
        // After SettingsScreen pops, the dummy should still be there.
        var input = new GameInputManager();
        var screenManager = new ScreenManager();
        screenManager.Push(new DummyScreen());

        var settings = new SettingsScreen(input, _bindingsPath);
        screenManager.Push(settings);

        Assert.True(screenManager.HasScreens);

        // Navigate to Back and confirm
        var up = SimulateKeyPress(Keys.Up);        // wrap to Back
        settings.Update(DefaultGameTime, up);

        var interact = SimulateKeyPress(Keys.Z);   // select Back
        settings.Update(DefaultGameTime, interact);

        // Dummy is still on the stack
        Assert.True(screenManager.HasScreens);
    }
}
