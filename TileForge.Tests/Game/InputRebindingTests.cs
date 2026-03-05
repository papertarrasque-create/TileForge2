using System;
using System.IO;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class InputRebindingTests : IDisposable
{
    private readonly string _tempDir;

    public InputRebindingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tileforge_rebind_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private static GameInputManager MakeManager() => new GameInputManager();

    private static void Tick(GameInputManager mgr, Keys[] pressedKeys)
        => mgr.Update(new KeyboardState(pressedKeys));

    private string TempPath(string fileName) => Path.Combine(_tempDir, fileName);

    // -----------------------------------------------------------------------
    // 1. RebindAction changes the key for an action
    // -----------------------------------------------------------------------

    [Fact]
    public void RebindAction_ChangesKeyForAction()
    {
        var mgr = MakeManager();

        mgr.RebindAction(GameAction.MoveUp, Keys.W);
        Tick(mgr, new[] { Keys.W });

        Assert.True(mgr.IsActionPressed(GameAction.MoveUp));
    }

    // -----------------------------------------------------------------------
    // 2. RebindAction replaces all previous keys (multi-key binding becomes single)
    // -----------------------------------------------------------------------

    [Fact]
    public void RebindAction_ReplacesAllPreviousKeys_MultiBindingBecomesSingle()
    {
        var mgr = MakeManager();

        // Interact defaults to Z + Enter; rebind to A only
        mgr.RebindAction(GameAction.Interact, Keys.A);

        Tick(mgr, new[] { Keys.Z });
        Assert.False(mgr.IsActionPressed(GameAction.Interact),
            "Old key Z should no longer trigger Interact after rebind");

        Tick(mgr, new[] { Keys.Enter });
        Assert.False(mgr.IsActionPressed(GameAction.Interact),
            "Old key Enter should no longer trigger Interact after rebind");

        Tick(mgr, new[] { Keys.A });
        Assert.True(mgr.IsActionPressed(GameAction.Interact),
            "New key A should trigger Interact after rebind");
    }

    // -----------------------------------------------------------------------
    // 3. ResetDefaults restores original bindings after rebind
    // -----------------------------------------------------------------------

    [Fact]
    public void ResetDefaults_RestoresOriginalBindings_AfterRebind()
    {
        var mgr = MakeManager();

        mgr.RebindAction(GameAction.MoveUp, Keys.W);
        mgr.ResetDefaults();

        // Original Up key should work again
        Tick(mgr, new[] { Keys.Up });
        Assert.True(mgr.IsActionPressed(GameAction.MoveUp),
            "Default key Up should trigger MoveUp after ResetDefaults");

        // Rebound key should no longer work (unless it coincidentally matches a default)
        Tick(mgr, new[] { Keys.W });
        Assert.False(mgr.IsActionPressed(GameAction.MoveUp),
            "Rebound key W should not trigger MoveUp after ResetDefaults");
    }

    // -----------------------------------------------------------------------
    // 4. GetBindings returns copy (modifying returned dict doesn't affect manager)
    // -----------------------------------------------------------------------

    [Fact]
    public void GetBindings_ReturnsCopy_ModifyingItDoesNotAffectManager()
    {
        var mgr = MakeManager();

        var bindings = mgr.GetBindings();

        // Mutate the returned dictionary
        bindings[GameAction.MoveUp] = new[] { Keys.W };

        // Manager should still respond to the original key
        Tick(mgr, new[] { Keys.Up });
        Assert.True(mgr.IsActionPressed(GameAction.MoveUp),
            "Manager should still use original Up key after external dictionary mutation");

        // Manager should NOT respond to the externally injected key
        Tick(mgr, new[] { Keys.W });
        Assert.False(mgr.IsActionPressed(GameAction.MoveUp),
            "Manager should not use W key — it was only added to the external copy");
    }

    // -----------------------------------------------------------------------
    // 5. SaveBindings creates a JSON file
    // -----------------------------------------------------------------------

    [Fact]
    public void SaveBindings_CreatesJsonFile()
    {
        var mgr = MakeManager();
        var path = TempPath("bindings.json");

        mgr.SaveBindings(path);

        Assert.True(File.Exists(path), "SaveBindings should create the file");
    }

    // -----------------------------------------------------------------------
    // 6. LoadBindings restores saved bindings
    // -----------------------------------------------------------------------

    [Fact]
    public void LoadBindings_RestoresSavedBindings()
    {
        var mgr = MakeManager();
        mgr.RebindAction(GameAction.MoveUp, Keys.W);

        var path = TempPath("bindings.json");
        mgr.SaveBindings(path);

        var mgr2 = MakeManager();
        mgr2.LoadBindings(path);

        Tick(mgr2, new[] { Keys.W });
        Assert.True(mgr2.IsActionPressed(GameAction.MoveUp),
            "LoadBindings should restore the rebound key W for MoveUp");
    }

    // -----------------------------------------------------------------------
    // 7. SaveBindings + LoadBindings roundtrip
    // -----------------------------------------------------------------------

    [Fact]
    public void SaveLoadBindings_Roundtrip_PreservesAllActions()
    {
        var mgr = MakeManager();
        mgr.RebindAction(GameAction.MoveUp,        Keys.W);
        mgr.RebindAction(GameAction.MoveDown,      Keys.S);
        mgr.RebindAction(GameAction.MoveLeft,      Keys.A);
        mgr.RebindAction(GameAction.MoveRight,     Keys.D);
        mgr.RebindAction(GameAction.Interact,      Keys.E);
        mgr.RebindAction(GameAction.Cancel,        Keys.Q);
        mgr.RebindAction(GameAction.Pause,         Keys.P);
        mgr.RebindAction(GameAction.OpenInventory, Keys.Tab);

        var path = TempPath("bindings_roundtrip.json");
        mgr.SaveBindings(path);

        var mgr2 = MakeManager();
        mgr2.LoadBindings(path);

        Tick(mgr2, new[] { Keys.W });   Assert.True(mgr2.IsActionPressed(GameAction.MoveUp));
        Tick(mgr2, new[] { Keys.S });   Assert.True(mgr2.IsActionPressed(GameAction.MoveDown));
        Tick(mgr2, new[] { Keys.A });   Assert.True(mgr2.IsActionPressed(GameAction.MoveLeft));
        Tick(mgr2, new[] { Keys.D });   Assert.True(mgr2.IsActionPressed(GameAction.MoveRight));
        Tick(mgr2, new[] { Keys.E });   Assert.True(mgr2.IsActionPressed(GameAction.Interact));
        Tick(mgr2, new[] { Keys.Q });   Assert.True(mgr2.IsActionPressed(GameAction.Cancel));
        Tick(mgr2, new[] { Keys.P });   Assert.True(mgr2.IsActionPressed(GameAction.Pause));
        Tick(mgr2, new[] { Keys.Tab }); Assert.True(mgr2.IsActionPressed(GameAction.OpenInventory));
    }

    // -----------------------------------------------------------------------
    // 8. LoadBindings with missing file does nothing (keeps defaults)
    // -----------------------------------------------------------------------

    [Fact]
    public void LoadBindings_MissingFile_KeepsDefaults()
    {
        var mgr = MakeManager();
        var path = TempPath("nonexistent.json");

        // File does not exist; call should be a no-op
        var ex = Record.Exception(() => mgr.LoadBindings(path));
        Assert.Null(ex);

        // Default bindings should still work
        Tick(mgr, new[] { Keys.Up });
        Assert.True(mgr.IsActionPressed(GameAction.MoveUp),
            "Default MoveUp binding should still work after LoadBindings with missing file");
    }

    // -----------------------------------------------------------------------
    // 9. LoadBindings with corrupt JSON does nothing (keeps defaults)
    // -----------------------------------------------------------------------

    [Fact]
    public void LoadBindings_CorruptJson_KeepsDefaults()
    {
        var path = TempPath("corrupt.json");
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(path, "{ this is not valid json !!!!");

        var mgr = MakeManager();
        var ex = Record.Exception(() => mgr.LoadBindings(path));
        Assert.Null(ex);

        // Default bindings should still work
        Tick(mgr, new[] { Keys.Up });
        Assert.True(mgr.IsActionPressed(GameAction.MoveUp),
            "Default MoveUp binding should still work after LoadBindings with corrupt JSON");
    }

    // -----------------------------------------------------------------------
    // 10. After rebind, IsActionPressed uses new key
    // -----------------------------------------------------------------------

    [Fact]
    public void AfterRebind_IsActionPressed_UsesNewKey()
    {
        var mgr = MakeManager();

        mgr.RebindAction(GameAction.MoveDown, Keys.S);
        Tick(mgr, new[] { Keys.S });

        Assert.True(mgr.IsActionPressed(GameAction.MoveDown),
            "IsActionPressed should detect new key S for MoveDown after rebind");
    }

    // -----------------------------------------------------------------------
    // 11. After rebind, old key no longer triggers action
    // -----------------------------------------------------------------------

    [Fact]
    public void AfterRebind_OldKey_NoLongerTriggersAction()
    {
        var mgr = MakeManager();

        mgr.RebindAction(GameAction.MoveDown, Keys.S);
        Tick(mgr, new[] { Keys.Down });

        Assert.False(mgr.IsActionPressed(GameAction.MoveDown),
            "Old default key Down should no longer trigger MoveDown after rebind to S");
    }
}
