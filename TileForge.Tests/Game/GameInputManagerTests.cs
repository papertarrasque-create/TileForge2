using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class GameInputManagerTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static GameInputManager MakeManager() => new GameInputManager();

    /// <summary>
    /// Advances the manager by one frame with the given pressed keys.
    /// </summary>
    private static void Tick(GameInputManager mgr, Keys[] pressedKeys)
        => mgr.Update(new KeyboardState(pressedKeys));

    // -----------------------------------------------------------------------
    // 1. Default bindings exist for all GameAction values
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(GameAction.MoveUp)]
    [InlineData(GameAction.MoveDown)]
    [InlineData(GameAction.MoveLeft)]
    [InlineData(GameAction.MoveRight)]
    [InlineData(GameAction.Interact)]
    [InlineData(GameAction.Cancel)]
    [InlineData(GameAction.Pause)]
    [InlineData(GameAction.OpenInventory)]
    public void DefaultBindings_ExistForAllGameActions(GameAction action)
    {
        var mgr = MakeManager();

        // Press the first bound key for each action and confirm it is detected
        Keys triggerKey = action switch
        {
            GameAction.MoveUp        => Keys.Up,
            GameAction.MoveDown      => Keys.Down,
            GameAction.MoveLeft      => Keys.Left,
            GameAction.MoveRight     => Keys.Right,
            GameAction.Interact      => Keys.Z,
            GameAction.Cancel        => Keys.X,
            GameAction.Pause         => Keys.Escape,
            GameAction.OpenInventory => Keys.I,
            _                        => throw new System.Exception("Unhandled action"),
        };

        Tick(mgr, new[] { triggerKey });

        Assert.True(mgr.IsActionPressed(action),
            $"Expected default binding for {action} to include {triggerKey}");
    }

    // -----------------------------------------------------------------------
    // 2. IsActionJustPressed detects edge: key down this frame, up last frame
    // -----------------------------------------------------------------------

    [Fact]
    public void IsActionJustPressed_KeyDownThisFrame_UpLastFrame_ReturnsTrue()
    {
        var mgr = MakeManager();
        Tick(mgr, System.Array.Empty<Keys>());   // frame 1: no keys
        Tick(mgr, new[] { Keys.Up });             // frame 2: Up pressed

        Assert.True(mgr.IsActionJustPressed(GameAction.MoveUp));
    }

    // -----------------------------------------------------------------------
    // 3. IsActionJustPressed returns false when key held across frames
    // -----------------------------------------------------------------------

    [Fact]
    public void IsActionJustPressed_KeyHeldAcrossFrames_ReturnsFalse()
    {
        var mgr = MakeManager();
        Tick(mgr, new[] { Keys.Up });   // frame 1: Up pressed
        Tick(mgr, new[] { Keys.Up });   // frame 2: Up still pressed

        Assert.False(mgr.IsActionJustPressed(GameAction.MoveUp));
    }

    // -----------------------------------------------------------------------
    // 4. IsActionPressed detects held key
    // -----------------------------------------------------------------------

    [Fact]
    public void IsActionPressed_KeyHeld_ReturnsTrue()
    {
        var mgr = MakeManager();
        Tick(mgr, new[] { Keys.Down });

        Assert.True(mgr.IsActionPressed(GameAction.MoveDown));
    }

    // -----------------------------------------------------------------------
    // 5. IsActionPressed returns false when key not pressed
    // -----------------------------------------------------------------------

    [Fact]
    public void IsActionPressed_KeyNotPressed_ReturnsFalse()
    {
        var mgr = MakeManager();
        Tick(mgr, System.Array.Empty<Keys>());

        Assert.False(mgr.IsActionPressed(GameAction.MoveDown));
    }

    // -----------------------------------------------------------------------
    // 6. Multi-key binding: Z triggers Interact
    // -----------------------------------------------------------------------

    [Fact]
    public void IsActionPressed_ZKey_TriggersInteract()
    {
        var mgr = MakeManager();
        Tick(mgr, new[] { Keys.Z });

        Assert.True(mgr.IsActionPressed(GameAction.Interact));
    }

    // -----------------------------------------------------------------------
    // 7. Multi-key binding: Enter also triggers Interact
    // -----------------------------------------------------------------------

    [Fact]
    public void IsActionPressed_EnterKey_TriggersInteract()
    {
        var mgr = MakeManager();
        Tick(mgr, new[] { Keys.Enter });

        Assert.True(mgr.IsActionPressed(GameAction.Interact));
    }

    // -----------------------------------------------------------------------
    // 8. No action detected when no keys pressed
    // -----------------------------------------------------------------------

    [Fact]
    public void NoAction_WhenNoKeysPressed()
    {
        var mgr = MakeManager();
        Tick(mgr, System.Array.Empty<Keys>());

        foreach (GameAction action in System.Enum.GetValues<GameAction>())
        {
            Assert.False(mgr.IsActionPressed(action),
                $"Expected IsActionPressed({action}) to be false when no keys are pressed");
        }
    }

    // -----------------------------------------------------------------------
    // 9. JustPressed returns false before first Update call
    // -----------------------------------------------------------------------

    [Fact]
    public void IsActionJustPressed_BeforeFirstUpdate_ReturnsFalse()
    {
        var mgr = MakeManager();

        // No Update() called — default KeyboardState reports no keys,
        // but _hasBeenUpdated guard must prevent false positives.
        Assert.False(mgr.IsActionJustPressed(GameAction.MoveUp));
    }

    // -----------------------------------------------------------------------
    // 10. Multiple actions can be pressed simultaneously
    // -----------------------------------------------------------------------

    [Fact]
    public void IsActionPressed_MultipleActionsSimultaneously()
    {
        var mgr = MakeManager();
        Tick(mgr, new[] { Keys.Up, Keys.Z, Keys.I });

        Assert.True(mgr.IsActionPressed(GameAction.MoveUp));
        Assert.True(mgr.IsActionPressed(GameAction.Interact));
        Assert.True(mgr.IsActionPressed(GameAction.OpenInventory));
    }

    // -----------------------------------------------------------------------
    // Bonus: JustPressed works for alternate key in multi-key binding
    //        (Enter pressed fresh while Z was already held — should still detect)
    // -----------------------------------------------------------------------

    [Fact]
    public void IsActionJustPressed_AlternateKeyPressedFresh_WhileOtherKeyHeld_ReturnsTrue()
    {
        var mgr = MakeManager();
        Tick(mgr, new[] { Keys.Z });              // frame 1: Z held
        Tick(mgr, new[] { Keys.Z, Keys.Enter });  // frame 2: Enter newly pressed

        // Enter is down in current AND up in previous — triggers JustPressed
        Assert.True(mgr.IsActionJustPressed(GameAction.Interact));
    }
}
