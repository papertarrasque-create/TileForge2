using System;
using System.Collections.Generic;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class EquipmentManagerTests
{
    // -------------------------------------------------------------------------
    // Equip / Unequip mechanics
    // -------------------------------------------------------------------------

    [Fact]
    public void EquipItem_MovesFromInventoryToSlot()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("Sword");

        gsm.EquipItem("Sword", EquipmentSlot.Weapon);

        Assert.Empty(gsm.State.Player.Inventory);
        Assert.Equal("Sword", gsm.State.Player.Equipment["Weapon"]);
    }

    [Fact]
    public void EquipItem_RemovesOneFromInventory()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("Sword");
        gsm.AddToInventory("Sword");
        gsm.AddToInventory("Sword");

        gsm.EquipItem("Sword", EquipmentSlot.Weapon);

        Assert.Equal(2, gsm.State.Player.Inventory.Count);
    }

    [Fact]
    public void EquipItem_DisplacesExisting_ReturnsToInventory()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("Sword");
        gsm.AddToInventory("Axe");

        gsm.EquipItem("Sword", EquipmentSlot.Weapon);
        gsm.EquipItem("Axe", EquipmentSlot.Weapon);

        Assert.Equal("Axe", gsm.GetEquippedItem(EquipmentSlot.Weapon));
        Assert.Contains("Sword", gsm.State.Player.Inventory);
        Assert.Single(gsm.State.Player.Inventory.FindAll(i => i == "Sword"));
        Assert.DoesNotContain("Axe", gsm.State.Player.Inventory);
    }

    [Fact]
    public void UnequipItem_MovesBackToInventory()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("Shield");
        gsm.EquipItem("Shield", EquipmentSlot.Armor);

        gsm.UnequipItem(EquipmentSlot.Armor);

        Assert.Null(gsm.GetEquippedItem(EquipmentSlot.Armor));
        Assert.Contains("Shield", gsm.State.Player.Inventory);
    }

    [Fact]
    public void UnequipItem_EmptySlot_NoError()
    {
        var gsm = new GameStateManager();

        // Should not throw
        var exception = Record.Exception(() => gsm.UnequipItem(EquipmentSlot.Weapon));

        Assert.Null(exception);
    }

    [Fact]
    public void IsEquipped_TrueWhenEquipped()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("Ring");
        gsm.EquipItem("Ring", EquipmentSlot.Accessory);

        Assert.True(gsm.IsEquipped("Ring"));
    }

    [Fact]
    public void IsEquipped_FalseWhenNotEquipped()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("Ring");

        Assert.False(gsm.IsEquipped("Ring"));
    }

    [Fact]
    public void GetEquippedItem_ReturnsItemName()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("Helmet");
        gsm.EquipItem("Helmet", EquipmentSlot.Armor);

        Assert.Equal("Helmet", gsm.GetEquippedItem(EquipmentSlot.Armor));
    }

    [Fact]
    public void GetEquippedItem_EmptySlot_ReturnsNull()
    {
        var gsm = new GameStateManager();

        Assert.Null(gsm.GetEquippedItem(EquipmentSlot.Weapon));
    }

    [Fact]
    public void EquipItem_AllThreeSlots()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("Sword");
        gsm.AddToInventory("ChainMail");
        gsm.AddToInventory("Amulet");

        gsm.EquipItem("Sword", EquipmentSlot.Weapon);
        gsm.EquipItem("ChainMail", EquipmentSlot.Armor);
        gsm.EquipItem("Amulet", EquipmentSlot.Accessory);

        Assert.Equal("Sword", gsm.GetEquippedItem(EquipmentSlot.Weapon));
        Assert.Equal("ChainMail", gsm.GetEquippedItem(EquipmentSlot.Armor));
        Assert.Equal("Amulet", gsm.GetEquippedItem(EquipmentSlot.Accessory));
        Assert.Empty(gsm.State.Player.Inventory);
    }

    // -------------------------------------------------------------------------
    // GetItemEquipSlot
    // -------------------------------------------------------------------------

    [Fact]
    public void GetItemEquipSlot_WeaponItem_ReturnsWeapon()
    {
        var gsm = new GameStateManager();
        gsm.State.ItemPropertyCache["Sword"] = new Dictionary<string, string>
        {
            { "equip_slot", "weapon" }
        };

        var result = gsm.GetItemEquipSlot("Sword");

        Assert.Equal(EquipmentSlot.Weapon, result);
    }

    [Fact]
    public void GetItemEquipSlot_NonEquippableItem_ReturnsNull()
    {
        var gsm = new GameStateManager();
        gsm.State.ItemPropertyCache["Potion"] = new Dictionary<string, string>
        {
            { "heal_amount", "30" }
        };

        var result = gsm.GetItemEquipSlot("Potion");

        Assert.Null(result);
    }

    [Fact]
    public void GetItemEquipSlot_UnknownItem_ReturnsNull()
    {
        var gsm = new GameStateManager();

        var result = gsm.GetItemEquipSlot("NonExistentItem");

        Assert.Null(result);
    }

    [Fact]
    public void GetItemEquipSlot_CaseInsensitive()
    {
        var gsm = new GameStateManager();
        gsm.State.ItemPropertyCache["GreatSword"] = new Dictionary<string, string>
        {
            { "equip_slot", "Weapon" }
        };

        var result = gsm.GetItemEquipSlot("GreatSword");

        Assert.Equal(EquipmentSlot.Weapon, result);
    }

    // -------------------------------------------------------------------------
    // Effective stats
    // -------------------------------------------------------------------------

    [Fact]
    public void GetEffectiveAttack_NoGear_ReturnsBaseAttack()
    {
        var gsm = new GameStateManager();

        Assert.Equal(5, gsm.GetEffectiveAttack());
    }

    [Fact]
    public void GetEffectiveAttack_WithWeapon_AddsBonus()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("Sword");
        gsm.State.ItemPropertyCache["Sword"] = new Dictionary<string, string>
        {
            { "equip_slot", "weapon" },
            { "equip_attack", "10" }
        };
        gsm.EquipItem("Sword", EquipmentSlot.Weapon);

        Assert.Equal(15, gsm.GetEffectiveAttack());
    }

    [Fact]
    public void GetEffectiveAttack_MultipleGear_SumsBonuses()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("Sword");
        gsm.AddToInventory("PowerRing");
        gsm.State.ItemPropertyCache["Sword"] = new Dictionary<string, string>
        {
            { "equip_slot", "weapon" },
            { "equip_attack", "8" }
        };
        gsm.State.ItemPropertyCache["PowerRing"] = new Dictionary<string, string>
        {
            { "equip_slot", "accessory" },
            { "equip_attack", "3" }
        };
        gsm.EquipItem("Sword", EquipmentSlot.Weapon);
        gsm.EquipItem("PowerRing", EquipmentSlot.Accessory);

        // base 5 + sword 8 + ring 3 = 16
        Assert.Equal(16, gsm.GetEffectiveAttack());
    }

    [Fact]
    public void GetEffectiveDefense_WithArmor_AddsBonus()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("ChainMail");
        gsm.State.ItemPropertyCache["ChainMail"] = new Dictionary<string, string>
        {
            { "equip_slot", "armor" },
            { "equip_defense", "8" }
        };
        gsm.EquipItem("ChainMail", EquipmentSlot.Armor);

        // base defense 2 + armor 8 = 10
        Assert.Equal(10, gsm.GetEffectiveDefense());
    }

    [Fact]
    public void GetEquipmentBonus_MissingCacheEntry_IgnoresGracefully()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("MysteryItem");
        gsm.EquipItem("MysteryItem", EquipmentSlot.Weapon);
        // Deliberately do NOT populate ItemPropertyCache for "MysteryItem"

        var exception = Record.Exception(() => gsm.GetEffectiveAttack());

        Assert.Null(exception);
        // No bonus added; returns base attack
        Assert.Equal(5, gsm.GetEffectiveAttack());
    }

    [Fact]
    public void GetEquipmentBonus_NonNumericValue_IgnoresGracefully()
    {
        var gsm = new GameStateManager();
        gsm.AddToInventory("BrokenSword");
        gsm.State.ItemPropertyCache["BrokenSword"] = new Dictionary<string, string>
        {
            { "equip_slot", "weapon" },
            { "equip_attack", "abc" }
        };
        gsm.EquipItem("BrokenSword", EquipmentSlot.Weapon);

        var exception = Record.Exception(() => gsm.GetEffectiveAttack());

        Assert.Null(exception);
        // Non-numeric bonus is ignored; returns base attack
        Assert.Equal(5, gsm.GetEffectiveAttack());
    }
}
