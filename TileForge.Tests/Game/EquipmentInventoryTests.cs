using System;
using System.Collections.Generic;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

/// <summary>
/// Tests the GameStateManager operations that the InventoryScreen calls
/// during equip/unequip/use flows -- the full user-facing cycle.
/// </summary>
public class EquipmentInventoryTests
{
    private GameStateManager CreateManagerWithItems()
    {
        var gsm = new GameStateManager();
        // Add a weapon to inventory
        gsm.AddToInventory("Iron Sword");
        gsm.State.ItemPropertyCache["Iron Sword"] = new Dictionary<string, string>
        {
            { "equip_slot", "weapon" },
            { "equip_attack", "10" }
        };
        // Add armor to inventory
        gsm.AddToInventory("Chainmail");
        gsm.State.ItemPropertyCache["Chainmail"] = new Dictionary<string, string>
        {
            { "equip_slot", "armor" },
            { "equip_defense", "8" }
        };
        // Add a potion (not equippable)
        gsm.AddToInventory("Potion");
        gsm.AddToInventory("Potion");
        gsm.State.ItemPropertyCache["Potion"] = new Dictionary<string, string>
        {
            { "heal", "25" }
        };
        return gsm;
    }

    // -------------------------------------------------------------------------
    // 1. EquipFromInventory_WeaponGoesToSlot
    // -------------------------------------------------------------------------
    [Fact]
    public void EquipFromInventory_WeaponGoesToSlot()
    {
        var gsm = CreateManagerWithItems();

        gsm.EquipItem("Iron Sword", EquipmentSlot.Weapon);

        Assert.Equal("Iron Sword", gsm.GetEquippedItem(EquipmentSlot.Weapon));
    }

    // -------------------------------------------------------------------------
    // 2. EquipFromInventory_RemovedFromInventory
    // -------------------------------------------------------------------------
    [Fact]
    public void EquipFromInventory_RemovedFromInventory()
    {
        var gsm = CreateManagerWithItems();

        gsm.EquipItem("Iron Sword", EquipmentSlot.Weapon);

        Assert.DoesNotContain("Iron Sword", gsm.State.Player.Inventory);
    }

    // -------------------------------------------------------------------------
    // 3. EquipFromInventory_StatsIncrease
    // -------------------------------------------------------------------------
    [Fact]
    public void EquipFromInventory_StatsIncrease()
    {
        var gsm = CreateManagerWithItems();
        int baseAttack = gsm.GetEffectiveAttack();

        gsm.EquipItem("Iron Sword", EquipmentSlot.Weapon);

        Assert.Equal(baseAttack + 10, gsm.GetEffectiveAttack());
    }

    // -------------------------------------------------------------------------
    // 4. UnequipFromSlot_ReturnsToInventory
    // -------------------------------------------------------------------------
    [Fact]
    public void UnequipFromSlot_ReturnsToInventory()
    {
        var gsm = CreateManagerWithItems();
        gsm.EquipItem("Iron Sword", EquipmentSlot.Weapon);

        gsm.UnequipItem(EquipmentSlot.Weapon);

        Assert.Null(gsm.GetEquippedItem(EquipmentSlot.Weapon));
        Assert.Contains("Iron Sword", gsm.State.Player.Inventory);
    }

    // -------------------------------------------------------------------------
    // 5. UnequipFromSlot_StatsDecrease
    // -------------------------------------------------------------------------
    [Fact]
    public void UnequipFromSlot_StatsDecrease()
    {
        var gsm = CreateManagerWithItems();
        int baseAttack = gsm.GetEffectiveAttack();
        gsm.EquipItem("Iron Sword", EquipmentSlot.Weapon);
        Assert.Equal(baseAttack + 10, gsm.GetEffectiveAttack());

        gsm.UnequipItem(EquipmentSlot.Weapon);

        Assert.Equal(baseAttack, gsm.GetEffectiveAttack());
    }

    // -------------------------------------------------------------------------
    // 6. EquipDisplacement_OldItemReturnsToInventory
    // -------------------------------------------------------------------------
    [Fact]
    public void EquipDisplacement_OldItemReturnsToInventory()
    {
        var gsm = CreateManagerWithItems();
        // Add a second weapon
        gsm.AddToInventory("Steel Sword");
        gsm.State.ItemPropertyCache["Steel Sword"] = new Dictionary<string, string>
        {
            { "equip_slot", "weapon" },
            { "equip_attack", "15" }
        };

        gsm.EquipItem("Iron Sword", EquipmentSlot.Weapon);
        gsm.EquipItem("Steel Sword", EquipmentSlot.Weapon);

        // Steel Sword is now equipped
        Assert.Equal("Steel Sword", gsm.GetEquippedItem(EquipmentSlot.Weapon));
        // Iron Sword returned to inventory
        Assert.Contains("Iron Sword", gsm.State.Player.Inventory);
        // Steel Sword removed from inventory
        Assert.DoesNotContain("Steel Sword", gsm.State.Player.Inventory);
    }

    // -------------------------------------------------------------------------
    // 7. HealItem_NotEquippable_ReturnsNull
    // -------------------------------------------------------------------------
    [Fact]
    public void HealItem_NotEquippable_ReturnsNull()
    {
        var gsm = CreateManagerWithItems();

        var slot = gsm.GetItemEquipSlot("Potion");

        Assert.Null(slot);
    }

    // -------------------------------------------------------------------------
    // 8. HealItem_StillUsable
    // -------------------------------------------------------------------------
    [Fact]
    public void HealItem_StillUsable()
    {
        var gsm = CreateManagerWithItems();
        gsm.DamagePlayer(30);
        int healthBefore = gsm.State.Player.Health;

        gsm.HealPlayer(25);
        gsm.RemoveFromInventory("Potion");

        Assert.Equal(healthBefore + 25, gsm.State.Player.Health);
        // Started with 2 potions, used 1, should have 1 left
        Assert.Single(gsm.State.Player.Inventory.FindAll(i => i == "Potion"));
    }

    // -------------------------------------------------------------------------
    // 9. EquipWeaponAndArmor_BothSlotsWork
    // -------------------------------------------------------------------------
    [Fact]
    public void EquipWeaponAndArmor_BothSlotsWork()
    {
        var gsm = CreateManagerWithItems();

        gsm.EquipItem("Iron Sword", EquipmentSlot.Weapon);
        gsm.EquipItem("Chainmail", EquipmentSlot.Armor);

        Assert.Equal("Iron Sword", gsm.GetEquippedItem(EquipmentSlot.Weapon));
        Assert.Equal("Chainmail", gsm.GetEquippedItem(EquipmentSlot.Armor));
        // Both items removed from inventory; only 2 potions remain
        Assert.Equal(2, gsm.State.Player.Inventory.Count);
        Assert.All(gsm.State.Player.Inventory, item => Assert.Equal("Potion", item));
    }

    // -------------------------------------------------------------------------
    // 10. IsEquipped_AfterEquip_True
    // -------------------------------------------------------------------------
    [Fact]
    public void IsEquipped_AfterEquip_True()
    {
        var gsm = CreateManagerWithItems();

        gsm.EquipItem("Iron Sword", EquipmentSlot.Weapon);

        Assert.True(gsm.IsEquipped("Iron Sword"));
    }

    // -------------------------------------------------------------------------
    // 11. IsEquipped_AfterUnequip_False
    // -------------------------------------------------------------------------
    [Fact]
    public void IsEquipped_AfterUnequip_False()
    {
        var gsm = CreateManagerWithItems();
        gsm.EquipItem("Iron Sword", EquipmentSlot.Weapon);
        Assert.True(gsm.IsEquipped("Iron Sword"));

        gsm.UnequipItem(EquipmentSlot.Weapon);

        Assert.False(gsm.IsEquipped("Iron Sword"));
    }

    // -------------------------------------------------------------------------
    // 12. GetItemEquipSlot_Potion_ReturnsNull
    // -------------------------------------------------------------------------
    [Fact]
    public void GetItemEquipSlot_Potion_ReturnsNull()
    {
        var gsm = CreateManagerWithItems();

        var result = gsm.GetItemEquipSlot("Potion");

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // 13. EquipItem_WithTwoCopies_OnlyOneConsumed
    // -------------------------------------------------------------------------
    [Fact]
    public void EquipItem_WithTwoCopies_OnlyOneConsumed()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("Dagger");
        gsm.AddToInventory("Dagger");
        gsm.State.ItemPropertyCache["Dagger"] = new Dictionary<string, string>
        {
            { "equip_slot", "weapon" },
            { "equip_attack", "5" }
        };

        gsm.EquipItem("Dagger", EquipmentSlot.Weapon);

        // One copy equipped, one still in inventory
        Assert.Equal("Dagger", gsm.GetEquippedItem(EquipmentSlot.Weapon));
        Assert.Single(gsm.State.Player.Inventory);
        Assert.Equal("Dagger", gsm.State.Player.Inventory[0]);
    }

    // -------------------------------------------------------------------------
    // 14. FullCycle_EquipUseUnequip_InventoryCorrect
    // -------------------------------------------------------------------------
    [Fact]
    public void FullCycle_EquipUseUnequip_InventoryCorrect()
    {
        var gsm = CreateManagerWithItems();
        // Initial state: Iron Sword, Chainmail, Potion x2 in inventory (4 items total)
        Assert.Equal(4, gsm.State.Player.Inventory.Count);

        // Step 1: Equip the weapon
        gsm.EquipItem("Iron Sword", EquipmentSlot.Weapon);
        Assert.Equal("Iron Sword", gsm.GetEquippedItem(EquipmentSlot.Weapon));
        Assert.Equal(3, gsm.State.Player.Inventory.Count); // Chainmail + 2 Potions

        // Step 2: Use a potion (simulate heal flow)
        gsm.DamagePlayer(20);
        gsm.HealPlayer(25);
        gsm.RemoveFromInventory("Potion");
        Assert.Equal(2, gsm.State.Player.Inventory.Count); // Chainmail + 1 Potion

        // Step 3: Equip the armor
        gsm.EquipItem("Chainmail", EquipmentSlot.Armor);
        Assert.Equal("Chainmail", gsm.GetEquippedItem(EquipmentSlot.Armor));
        Assert.Single(gsm.State.Player.Inventory); // 1 Potion left

        // Step 4: Unequip the weapon (returns to inventory)
        gsm.UnequipItem(EquipmentSlot.Weapon);
        Assert.Null(gsm.GetEquippedItem(EquipmentSlot.Weapon));
        Assert.Equal(2, gsm.State.Player.Inventory.Count); // Iron Sword + Potion

        // Step 5: Verify final stats
        // Only armor equipped: base defense 2 + chainmail 8 = 10
        Assert.Equal(10, gsm.GetEffectiveDefense());
        // No weapon equipped: base attack 5
        Assert.Equal(5, gsm.GetEffectiveAttack());

        // Verify inventory contents
        Assert.Contains("Iron Sword", gsm.State.Player.Inventory);
        Assert.Contains("Potion", gsm.State.Player.Inventory);
        Assert.DoesNotContain("Chainmail", gsm.State.Player.Inventory);
    }
}
