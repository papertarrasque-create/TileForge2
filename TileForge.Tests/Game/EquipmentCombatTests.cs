using System.Collections.Generic;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class EquipmentCombatTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static EntityInstance MakeEnemy(string id = "enemy_1", int health = 20, int defense = 3)
    {
        return new EntityInstance
        {
            Id = id,
            DefinitionName = "Goblin",
            X = 1,
            Y = 0,
            IsActive = true,
            Properties = new Dictionary<string, string>
            {
                { "health", health.ToString() },
                { "defense", defense.ToString() },
            },
        };
    }

    private static void RegisterWeapon(GameStateManager gsm, string name, int attackBonus)
    {
        gsm.AddToInventory(name);
        gsm.State.ItemPropertyCache[name] = new Dictionary<string, string>
        {
            { "equip_slot", "weapon" },
            { "equip_attack", attackBonus.ToString() },
        };
        gsm.EquipItem(name, EquipmentSlot.Weapon);
    }

    private static void RegisterArmor(GameStateManager gsm, string name, int defenseBonus)
    {
        gsm.AddToInventory(name);
        gsm.State.ItemPropertyCache[name] = new Dictionary<string, string>
        {
            { "equip_slot", "armor" },
            { "equip_defense", defenseBonus.ToString() },
        };
        gsm.EquipItem(name, EquipmentSlot.Armor);
    }

    private static void RegisterAccessory(GameStateManager gsm, string name, int attackBonus)
    {
        gsm.AddToInventory(name);
        gsm.State.ItemPropertyCache[name] = new Dictionary<string, string>
        {
            { "equip_slot", "accessory" },
            { "equip_attack", attackBonus.ToString() },
        };
        gsm.EquipItem(name, EquipmentSlot.Accessory);
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void AttackEntity_WithWeaponEquipped_DealsMoreDamage()
    {
        // Base attack = 5, enemy defense = 3 → base damage = Max(1, 5-3) = 2
        // Weapon gives +10 attack → effective attack = 15, damage = Max(1, 15-3) = 12
        var gsm = new GameStateManager();
        var entity = MakeEnemy(health: 50, defense: 3);
        gsm.State.ActiveEntities.Add(entity);

        int baseDamage = CombatHelper.CalculateDamage(gsm.GetEffectiveAttack(), 3);

        RegisterWeapon(gsm, "BroadSword", attackBonus: 10);

        var result = gsm.AttackEntity(entity, gsm.GetEffectiveAttack());

        Assert.True(result.DamageDealt > baseDamage,
            $"Expected damage ({result.DamageDealt}) to exceed base damage ({baseDamage}) when weapon is equipped.");
    }

    [Fact]
    public void AttackEntity_WithoutWeapon_DealsBaseDamage()
    {
        // No equipment: effective attack = base 5, enemy defense = 3 → damage = 2
        var gsm = new GameStateManager();
        var entity = MakeEnemy(health: 50, defense: 3);
        gsm.State.ActiveEntities.Add(entity);

        int expectedDamage = CombatHelper.CalculateDamage(5, 3); // = 2

        var result = gsm.AttackEntity(entity, gsm.GetEffectiveAttack());

        Assert.Equal(expectedDamage, result.DamageDealt);
    }

    [Fact]
    public void EffectiveDefense_WithArmor_ReducesDamage()
    {
        // Base defense = 2. Enemy attacks at 6.
        // Without armor: CombatHelper(6, 2) = 4
        // With armor +5: CombatHelper(6, 7) = Max(1, -1) = 1
        var gsm = new GameStateManager();

        int damageWithoutArmor = CombatHelper.CalculateDamage(6, gsm.GetEffectiveDefense());

        RegisterArmor(gsm, "PlateArmor", defenseBonus: 5);

        int damageWithArmor = CombatHelper.CalculateDamage(6, gsm.GetEffectiveDefense());

        Assert.True(damageWithArmor < damageWithoutArmor,
            $"Damage with armor ({damageWithArmor}) should be less than without armor ({damageWithoutArmor}).");
    }

    [Fact]
    public void EffectiveAttack_WithMultipleGear_SumsBonuses()
    {
        // base 5 + weapon 10 + accessory 4 = 19
        var gsm = new GameStateManager();

        RegisterWeapon(gsm, "WarAxe", attackBonus: 10);
        RegisterAccessory(gsm, "PowerRing", attackBonus: 4);

        int effectiveAttack = gsm.GetEffectiveAttack();

        Assert.Equal(19, effectiveAttack);
    }

    [Fact]
    public void AttackEntity_AfterUnequip_ReturnsToBaseDamage()
    {
        // Equip weapon (+10 attack), attack, then unequip, attack again.
        // Second attack should deal the same as an unequipped baseline.
        var gsm = new GameStateManager();

        var entityEquipped = MakeEnemy("enemy_1", health: 100, defense: 0);
        gsm.State.ActiveEntities.Add(entityEquipped);

        RegisterWeapon(gsm, "Katana", attackBonus: 10);
        var resultWithWeapon = gsm.AttackEntity(entityEquipped, gsm.GetEffectiveAttack());

        gsm.UnequipItem(EquipmentSlot.Weapon);

        var entityUnequipped = MakeEnemy("enemy_2", health: 100, defense: 0);
        gsm.State.ActiveEntities.Add(entityUnequipped);
        var resultWithoutWeapon = gsm.AttackEntity(entityUnequipped, gsm.GetEffectiveAttack());

        int expectedBaseDamage = CombatHelper.CalculateDamage(5, 0); // = 5
        Assert.Equal(expectedBaseDamage, resultWithoutWeapon.DamageDealt);
        Assert.True(resultWithWeapon.DamageDealt > resultWithoutWeapon.DamageDealt,
            "Equipped weapon should deal more damage than after unequipping.");
    }

    [Fact]
    public void CombatHelper_WithEquipmentBonuses_Calculates()
    {
        // Directly verify CombatHelper formula with values typical of equipment bonuses.
        // Base attack 5 + weapon 10 + accessory 3 = 18 effective attack.
        // Enemy defense 4.
        // Expected: Max(1, 18 - 4) = 14
        int effectiveAttack = 5 + 10 + 3;
        int enemyDefense = 4;

        int damage = CombatHelper.CalculateDamage(effectiveAttack, enemyDefense);

        Assert.Equal(14, damage);
    }
}
