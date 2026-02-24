using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class InventoryScreenTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static GameStateManager CreateGameStateManager()
    {
        var gsm = new GameStateManager();
        // Initialize with minimal state so State.Player is valid
        gsm.State.Player = new PlayerState { Health = 80, MaxHealth = 100 };
        return gsm;
    }

    private static (InventoryScreen screen, ScreenManager manager, GameStateManager gsm) CreateInventoryScreen()
    {
        var gsm = CreateGameStateManager();
        var screenManager = new ScreenManager();
        var screen = new InventoryScreen(gsm);
        screenManager.Push(screen);
        return (screen, screenManager, gsm);
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
    // Construction & overlay
    // =========================================================================

    [Fact]
    public void IsOverlay_ReturnsTrue()
    {
        var gsm = CreateGameStateManager();
        var screen = new InventoryScreen(gsm);
        Assert.True(screen.IsOverlay);
    }

    // =========================================================================
    // Empty inventory — only "Close" item
    // =========================================================================

    [Fact]
    public void EmptyInventory_InteractImmediately_PopsScreen()
    {
        // No items → index 0 = "Close"
        var (screen, manager, _) = CreateInventoryScreen();

        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Cancel closes
    // =========================================================================

    [Fact]
    public void Cancel_PopsScreen()
    {
        var (screen, manager, _) = CreateInventoryScreen();

        var cancel = SimulateKeyPress(Keys.X);
        screen.Update(DefaultGameTime, cancel);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // OpenInventory toggle closes
    // =========================================================================

    [Fact]
    public void OpenInventory_PopsScreen()
    {
        var (screen, manager, _) = CreateInventoryScreen();

        var toggle = SimulateKeyPress(Keys.I);
        screen.Update(DefaultGameTime, toggle);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Inventory with items
    // =========================================================================

    [Fact]
    public void WithItems_CloseIsLastItem()
    {
        var (screen, manager, gsm) = CreateInventoryScreen();
        gsm.AddToInventory("sword");
        gsm.AddToInventory("shield");

        // 2 items + Close = 3 items. Navigate to Close (index 2)
        var down = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, down);
        screen.Update(DefaultGameTime, down);

        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        Assert.False(manager.HasScreens);
    }

    [Fact]
    public void DuplicateItems_GroupedByName()
    {
        var (screen, manager, gsm) = CreateInventoryScreen();
        gsm.AddToInventory("potion");
        gsm.AddToInventory("potion");
        gsm.AddToInventory("potion");

        // 3 potions grouped = 1 item entry + Close = 2 items total
        // Navigate to Close (index 1) and select
        var down = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, down);

        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Item use — heal
    // =========================================================================

    [Fact]
    public void UseHealItem_HealsPlayer()
    {
        var (screen, manager, gsm) = CreateInventoryScreen();
        gsm.AddToInventory("health_potion");

        // Populate the ItemPropertyCache as CollectItem would
        gsm.State.ItemPropertyCache["health_potion"] = new Dictionary<string, string> { ["heal"] = "25" };

        Assert.Equal(80, gsm.State.Player.Health);

        // Index 0 = "health_potion", Interact to use
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        Assert.Equal(100, gsm.State.Player.Health); // healed to max (80 + 25 = capped at 100)
        Assert.Empty(gsm.State.Player.Inventory);   // consumed
        Assert.True(manager.HasScreens);             // screen stays open
    }

    [Fact]
    public void UseHealItem_AtFullHealth_DoesNotConsume()
    {
        var (screen, manager, gsm) = CreateInventoryScreen();
        gsm.State.Player.Health = 100; // full health
        gsm.AddToInventory("health_potion");

        // Populate the ItemPropertyCache as CollectItem would
        gsm.State.ItemPropertyCache["health_potion"] = new Dictionary<string, string> { ["heal"] = "25" };

        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        Assert.Equal(100, gsm.State.Player.Health);
        Assert.Single(gsm.State.Player.Inventory); // NOT consumed
    }

    [Fact]
    public void UseNonHealItem_ShowsCannotUse()
    {
        var (screen, manager, gsm) = CreateInventoryScreen();
        gsm.AddToInventory("sword");

        // No entity with heal property for sword
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        // Sword still in inventory (not consumed)
        Assert.Single(gsm.State.Player.Inventory);
        Assert.True(manager.HasScreens);
    }

    // =========================================================================
    // Navigation wrapping
    // =========================================================================

    [Fact]
    public void MoveUp_FromIndexZero_WrapsToClose()
    {
        var (screen, manager, gsm) = CreateInventoryScreen();
        gsm.AddToInventory("sword");

        // 1 item + Close = 2. MoveUp from 0 wraps to 1 (Close)
        var up = SimulateKeyPress(Keys.Up);
        screen.Update(DefaultGameTime, up);

        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        Assert.False(manager.HasScreens);
    }

    [Fact]
    public void MoveDown_FromClose_WrapsToFirstItem()
    {
        var (screen, manager, gsm) = CreateInventoryScreen();
        gsm.AddToInventory("sword");

        // Navigate to Close (index 1), then down wraps to 0
        var down = SimulateKeyPress(Keys.Down);
        screen.Update(DefaultGameTime, down); // index 1 (Close)
        screen.Update(DefaultGameTime, down); // wraps to 0 (sword)

        // Interact at sword index = attempt to use (sword has no heal → "Cannot use")
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(DefaultGameTime, interact);

        // Sword not consumable, screen stays open
        Assert.True(manager.HasScreens);
        Assert.Single(gsm.State.Player.Inventory);
    }

    // =========================================================================
    // RemoveFromInventory
    // =========================================================================

    [Fact]
    public void RemoveFromInventory_RemovesOneInstance()
    {
        var gsm = CreateGameStateManager();
        gsm.AddToInventory("potion");
        gsm.AddToInventory("potion");
        gsm.AddToInventory("sword");

        gsm.RemoveFromInventory("potion");

        Assert.Equal(2, gsm.State.Player.Inventory.Count);
        Assert.Contains("potion", gsm.State.Player.Inventory);
        Assert.Contains("sword", gsm.State.Player.Inventory);
    }

    [Fact]
    public void RemoveFromInventory_ReturnsFalse_WhenNotPresent()
    {
        var gsm = CreateGameStateManager();
        Assert.False(gsm.RemoveFromInventory("nonexistent"));
    }
}
