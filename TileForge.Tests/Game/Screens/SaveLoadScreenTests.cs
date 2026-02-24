using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class SaveLoadScreenTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a SaveManager backed by a temp directory so tests don't touch real saves.
    /// Returns the directory path so tests can verify file creation.
    /// </summary>
    private static (SaveManager manager, string dir) CreateSaveManager()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tileforge_sls_tests_" + Guid.NewGuid().ToString("N"));
        return (new SaveManager(dir), dir);
    }

    /// <summary>
    /// Simulates a single just-pressed key by calling Update twice:
    /// frame 1 — no keys (establishes baseline);
    /// frame 2 — key held (JustPressed = true).
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

    // =========================================================================
    // IsOverlay
    // =========================================================================

    [Fact]
    public void IsOverlay_ReturnsTrue()
    {
        var (saveManager, _) = CreateSaveManager();
        var screen = new SaveLoadScreen(saveManager, new GameStateManager(), SaveLoadMode.Save);

        Assert.True(screen.IsOverlay);
    }

    // =========================================================================
    // Save mode menu item count
    // =========================================================================

    [Fact]
    public void SaveMode_EmptySlots_HasTwoItems_NewSave_And_Back()
    {
        // No existing slots → menu is: "New Save", "Back" (2 items)
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Save);
        manager.Push(screen);

        // Default index 0 = "New Save". MoveDown → index 1 = "Back" → Interact pops.
        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.False(manager.HasScreens); // Back was selected — screen popped
    }

    [Fact]
    public void SaveMode_WithOneSlot_HasThreeItems_Slot_NewSave_Back()
    {
        // One existing slot → menu: "save_01", "New Save", "Back" (3 items)
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        saveManager.Save(gsm.State, "save_01");

        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Save);
        manager.Push(screen);

        // Navigate to "Back" (index 2 = slot_count(1) + newSave(1))
        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput); // index 1 "New Save"
        screen.Update(DefaultGameTime, moveDownInput); // index 2 "Back"

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.False(manager.HasScreens); // Back selected — popped
    }

    // =========================================================================
    // Load mode menu item count
    // =========================================================================

    [Fact]
    public void LoadMode_EmptySlots_HasOneItem_Back()
    {
        // No slots, load mode → menu: "Back" (1 item)
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Load);
        manager.Push(screen);

        // Default index 0 = "Back" — Interact pops immediately
        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.False(manager.HasScreens);
    }

    [Fact]
    public void LoadMode_WithOneSlot_HasTwoItems_Slot_Back()
    {
        // One slot, load mode → menu: "save_01", "Back" (2 items)
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        saveManager.Save(gsm.State, "save_01");

        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Load);
        manager.Push(screen);

        // Navigate to index 1 "Back", interact → pop
        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.False(manager.HasScreens);
    }

    [Fact]
    public void LoadMode_DoesNotHaveNewSaveItem()
    {
        // In load mode with no slots, index 0 is "Back", not "New Save".
        // Selecting it immediately pops (same as Back — not a save action).
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Load);
        manager.Push(screen);

        // Verify only 1 item (Back): MoveDown wraps back to index 0
        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput); // wraps to 0

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.False(manager.HasScreens);
        // No save should have been created
        Assert.Empty(saveManager.GetSlots());
    }

    // =========================================================================
    // New Save creates a slot
    // =========================================================================

    [Fact]
    public void SaveMode_SelectNewSave_CreatesSlotAndRefreshes()
    {
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Save);
        manager.Push(screen);

        // No slots → index 0 = "New Save"
        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        // Screen should still be open (status message shown, not popped)
        Assert.True(manager.HasScreens);

        // One slot should now exist
        var slots = saveManager.GetSlots();
        Assert.Single(slots);
        Assert.Equal("save_01", slots[0]);
    }

    [Fact]
    public void SaveMode_SelectNewSave_TwiceGeneratesDistinctSlotNames()
    {
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Save);
        manager.Push(screen);

        // First "New Save"
        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        // Second "New Save" — after first save the slot list has 1 item,
        // so "New Save" is now at index 1. Navigate there.
        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput); // index 1 = "New Save"
        screen.Update(DefaultGameTime, interactInput);

        var slots = saveManager.GetSlots();
        Assert.Equal(2, slots.Count);
        Assert.Equal("save_01", slots[0]);
        Assert.Equal("save_02", slots[1]);
    }

    // =========================================================================
    // Overwrite existing slot (Save mode)
    // =========================================================================

    [Fact]
    public void SaveMode_SelectExistingSlot_OverwritesSave()
    {
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        saveManager.Save(gsm.State, "save_01");

        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Save);
        manager.Push(screen);

        // index 0 = "save_01" → Interact overwrites
        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        // Screen stays open (status message)
        Assert.True(manager.HasScreens);
        // Still exactly one slot
        Assert.Single(saveManager.GetSlots());
    }

    // =========================================================================
    // Load a slot (Load mode)
    // =========================================================================

    [Fact]
    public void LoadMode_SelectSlot_CallsLoadStateAndPopsScreen()
    {
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();

        // Set a distinguishing flag, save, then reset state
        gsm.SetFlag("test_flag");
        saveManager.Save(gsm.State, "save_01");

        // New manager with fresh state (flag not set)
        var gsm2 = new GameStateManager();
        Assert.False(gsm2.HasFlag("test_flag"));

        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm2, SaveLoadMode.Load);
        manager.Push(screen);

        // index 0 = "save_01" → Interact loads
        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        // Screen should be popped after loading
        Assert.False(manager.HasScreens);

        // GameStateManager should now have the loaded flag
        Assert.True(gsm2.HasFlag("test_flag"));
    }

    // =========================================================================
    // Cancel / Back button pops the screen
    // =========================================================================

    [Fact]
    public void Cancel_PopsScreen()
    {
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Save);
        manager.Push(screen);

        var input = SimulateKeyPress(Keys.X);  // Cancel key
        screen.Update(DefaultGameTime, input);

        Assert.False(manager.HasScreens);
    }

    [Fact]
    public void BackItem_Interact_PopsScreen()
    {
        // Save mode, no slots: Back is at index 1
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Save);
        manager.Push(screen);

        // Navigate to Back (index 1 in Save mode with no slots)
        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput);

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Navigation wraps around
    // =========================================================================

    [Fact]
    public void MoveUp_FromIndexZero_WrapsToLastItem()
    {
        // Save mode, no slots: items are "New Save" (0), "Back" (1).
        // MoveUp from 0 wraps to 1 (Back) → Interact pops.
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Save);
        manager.Push(screen);

        var moveUpInput = SimulateKeyPress(Keys.Up);
        screen.Update(DefaultGameTime, moveUpInput); // wraps to index 1 (Back)

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        Assert.False(manager.HasScreens);
        // Nothing was saved (Back was selected)
        Assert.Empty(saveManager.GetSlots());
    }

    [Fact]
    public void MoveDown_FromLastItem_WrapsToFirstItem()
    {
        // Save mode, no slots: items are "New Save" (0), "Back" (1).
        // MoveDown from "Back" (1) wraps to "New Save" (0) → Interact creates save.
        var (saveManager, _) = CreateSaveManager();
        var gsm = new GameStateManager();
        var manager = new ScreenManager();
        var screen = new SaveLoadScreen(saveManager, gsm, SaveLoadMode.Save);
        manager.Push(screen);

        var moveDownInput = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, moveDownInput); // index 1 (Back)
        screen.Update(DefaultGameTime, moveDownInput); // wraps to index 0 (New Save)

        var interactInput = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interactInput);

        // "New Save" selected — slot created, screen stays open
        Assert.True(manager.HasScreens);
        Assert.Single(saveManager.GetSlots());
    }
}
