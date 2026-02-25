using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework.Input;
using TileForge.Data;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game;

public class ItemCollectionTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static EntityInstance MakeItem(string id, string definitionName, int x, int y,
        Dictionary<string, string> props = null)
    {
        return new EntityInstance
        {
            Id = id,
            DefinitionName = definitionName,
            X = x,
            Y = y,
            IsActive = true,
            Properties = props ?? new Dictionary<string, string>(),
        };
    }

    private static LoadedMap MakeMinimalLoadedMap(string id = "map_b")
    {
        return new LoadedMap
        {
            Id = id,
            Width = 5,
            Height = 5,
            Layers = new List<LoadedMapLayer>
            {
                new() { Name = "Ground", Cells = new string[25] },
            },
            Groups = new List<TileGroup>
            {
                new() { Name = "player", Type = GroupType.Entity, IsPlayer = true },
            },
            Entities = new List<EntityInstance>(),
        };
    }

    // =========================================================================
    // 1. GameState_ItemPropertyCache_DefaultsToEmpty
    // =========================================================================

    [Fact]
    public void GameState_ItemPropertyCache_DefaultsToEmpty()
    {
        var state = new GameState();

        Assert.NotNull(state.ItemPropertyCache);
        Assert.Empty(state.ItemPropertyCache);
    }

    // =========================================================================
    // 2. GameState_ItemPropertyCache_SerializationRoundtrip
    // =========================================================================

    [Fact]
    public void GameState_ItemPropertyCache_SerializationRoundtrip()
    {
        var original = new GameState
        {
            CurrentMapId = "map_01",
        };
        original.ItemPropertyCache["Potion"] = new Dictionary<string, string> { ["heal"] = "25" };
        original.ItemPropertyCache["Elixir"] = new Dictionary<string, string> { ["heal"] = "50", ["cure"] = "true" };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<GameState>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.ItemPropertyCache.Count);
        Assert.True(deserialized.ItemPropertyCache.ContainsKey("Potion"));
        Assert.Equal("25", deserialized.ItemPropertyCache["Potion"]["heal"]);
        Assert.True(deserialized.ItemPropertyCache.ContainsKey("Elixir"));
        Assert.Equal("50", deserialized.ItemPropertyCache["Elixir"]["heal"]);
        Assert.Equal("true", deserialized.ItemPropertyCache["Elixir"]["cure"]);
    }

    // =========================================================================
    // 3. GameStateManager_CollectItem_AddsToInventory
    // =========================================================================

    [Fact]
    public void GameStateManager_CollectItem_AddsToInventory()
    {
        var mgr = new GameStateManager();
        var entity = MakeItem("potion_01", "Potion", 3, 4,
            new Dictionary<string, string> { ["heal"] = "20" });

        mgr.CollectItem(entity);

        Assert.True(mgr.HasItem("Potion"));
    }

    // =========================================================================
    // 4. GameStateManager_CollectItem_CachesProperties
    // =========================================================================

    [Fact]
    public void GameStateManager_CollectItem_CachesProperties()
    {
        var mgr = new GameStateManager();
        var entity = MakeItem("potion_01", "Potion", 3, 4,
            new Dictionary<string, string> { ["heal"] = "20" });

        mgr.CollectItem(entity);

        Assert.True(mgr.State.ItemPropertyCache.ContainsKey("Potion"));
        Assert.Equal("20", mgr.State.ItemPropertyCache["Potion"]["heal"]);
    }

    // =========================================================================
    // 5. GameStateManager_CollectItem_DeactivatesEntity
    // =========================================================================

    [Fact]
    public void GameStateManager_CollectItem_DeactivatesEntity()
    {
        var mgr = new GameStateManager();
        var entity = MakeItem("potion_01", "Potion", 3, 4,
            new Dictionary<string, string> { ["heal"] = "20" });

        mgr.CollectItem(entity);

        Assert.False(entity.IsActive);
    }

    // =========================================================================
    // 6. GameStateManager_CollectItem_SetsInactiveFlag
    // =========================================================================

    [Fact]
    public void GameStateManager_CollectItem_SetsInactiveFlag()
    {
        var mgr = new GameStateManager();
        var entity = MakeItem("potion_01", "Potion", 3, 4,
            new Dictionary<string, string> { ["heal"] = "20" });

        mgr.CollectItem(entity);

        Assert.True(mgr.HasFlag(GameStateManager.EntityInactivePrefix + "potion_01"));
    }

    // =========================================================================
    // 7. GameStateManager_CollectItem_DoesNotOverwriteExistingCache
    // =========================================================================

    [Fact]
    public void GameStateManager_CollectItem_DoesNotOverwriteExistingCache()
    {
        var mgr = new GameStateManager();

        // First potion: heal=20
        var first = MakeItem("potion_01", "Potion", 1, 1,
            new Dictionary<string, string> { ["heal"] = "20" });
        mgr.CollectItem(first);

        // Second potion with different value (same DefinitionName)
        var second = MakeItem("potion_02", "Potion", 2, 2,
            new Dictionary<string, string> { ["heal"] = "99" });
        mgr.CollectItem(second);

        // Cache should still have the original value (first-write wins)
        Assert.Equal("20", mgr.State.ItemPropertyCache["Potion"]["heal"]);
    }

    // =========================================================================
    // 8. GameStateManager_CollectItem_EmptyProperties_DoesNotCache
    // =========================================================================

    [Fact]
    public void GameStateManager_CollectItem_EmptyProperties_DoesNotCache()
    {
        var mgr = new GameStateManager();
        // Entity with no properties (e.g. a key with no gameplay properties)
        var entity = MakeItem("key_01", "IronKey", 5, 5);

        mgr.CollectItem(entity);

        Assert.False(mgr.State.ItemPropertyCache.ContainsKey("IronKey"));
        // But inventory should still have it
        Assert.True(mgr.HasItem("IronKey"));
    }

    // =========================================================================
    // 9. ItemPropertyCache_SurvivesMapTransition
    // =========================================================================

    [Fact]
    public void ItemPropertyCache_SurvivesMapTransition()
    {
        var mgr = new GameStateManager();
        mgr.State.Player = new PlayerState { Health = 80, MaxHealth = 100 };

        var entity = MakeItem("potion_01", "Potion", 2, 2,
            new Dictionary<string, string> { ["heal"] = "30" });
        mgr.CollectItem(entity);

        Assert.True(mgr.State.ItemPropertyCache.ContainsKey("Potion"));

        // Transition to a new map (clears ActiveEntities, rebuilds from new map)
        var newMap = MakeMinimalLoadedMap("map_b");
        mgr.SwitchMap(newMap, 1, 1);

        // Cache must survive the transition
        Assert.True(mgr.State.ItemPropertyCache.ContainsKey("Potion"));
        Assert.Equal("30", mgr.State.ItemPropertyCache["Potion"]["heal"]);
    }

    // =========================================================================
    // 9b. CollectItem — event hooks for quest tracking
    // =========================================================================

    [Fact]
    public void CollectItem_WithOnCollectSetFlag_SetsFlag()
    {
        var mgr = new GameStateManager();
        var entity = MakeItem("gem_01", "Gem", 2, 3,
            new Dictionary<string, string> { ["on_collect_set_flag"] = "has_gem" });

        mgr.CollectItem(entity);

        Assert.True(mgr.HasFlag("has_gem"));
    }

    [Fact]
    public void CollectItem_WithOnCollectIncrement_IncrementsVariable()
    {
        var mgr = new GameStateManager();
        var entity = MakeItem("gem_01", "Gem", 2, 3,
            new Dictionary<string, string> { ["on_collect_increment"] = "gem_count" });

        mgr.CollectItem(entity);

        Assert.Equal("1", mgr.GetVariable("gem_count"));
    }

    // =========================================================================
    // 10. InventoryScreen_UseItem_ResolvesFromPropertyCache
    // =========================================================================

    [Fact]
    public void InventoryScreen_UseItem_ResolvesFromPropertyCache()
    {
        var mgr = new GameStateManager();
        mgr.State.Player = new PlayerState { Health = 60, MaxHealth = 100 };

        // Simulate item collected on previous map: inventory populated, cache populated,
        // but ActiveEntities is empty (as after a map transition that doesn't include this item).
        mgr.AddToInventory("Potion");
        mgr.State.ItemPropertyCache["Potion"] = new Dictionary<string, string> { ["heal"] = "25" };

        var screen = new InventoryScreen(mgr);
        var screenManager = new ScreenManager();
        screenManager.Push(screen);

        var gameTime = new Microsoft.Xna.Framework.GameTime(
            System.TimeSpan.FromSeconds(1),
            System.TimeSpan.FromSeconds(0.016));

        // Navigate past the 3 equipment slots (Weapon, Armor, Accessory) to reach the Potion
        for (int i = 0; i < 3; i++)
        {
            var downInput = new GameInputManager();
            downInput.Update(new KeyboardState());
            downInput.Update(new KeyboardState(Keys.Down));
            screen.Update(gameTime, downInput);
        }

        // Simulate pressing Interact (Z key) to use the item at index 3 = "Potion"
        var input = new GameInputManager();
        input.Update(new KeyboardState());          // previous frame: nothing pressed
        input.Update(new KeyboardState(Keys.Z));    // current frame: Z pressed
        screen.Update(gameTime, input);

        // Player should be healed from 60 → 85
        Assert.Equal(85, mgr.State.Player.Health);
        // Potion consumed from inventory
        Assert.Empty(mgr.State.Player.Inventory);
    }
}
