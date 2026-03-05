using System.Collections.Generic;
using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class AttackTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static EntityInstance MakeEntity(string id, string definitionName,
        Dictionary<string, string> props = null, bool isActive = true)
    {
        return new EntityInstance
        {
            Id = id,
            DefinitionName = definitionName,
            X = 1,
            Y = 1,
            IsActive = isActive,
            Properties = props ?? new Dictionary<string, string>(),
        };
    }

    private static Dictionary<string, TileGroup> MakeGroups(params (string name, EntityType type)[] entries)
    {
        var dict = new Dictionary<string, TileGroup>();
        foreach (var (name, type) in entries)
            dict[name] = new TileGroup { Name = name, EntityType = type };
        return dict;
    }

    // =========================================================================
    // IsAttackable — entity type checks
    // =========================================================================

    [Fact]
    public void IsAttackable_ActiveNpcWithHealth_ReturnsTrue()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Goblin", new Dictionary<string, string> { ["health"] = "10" });
        var groups = MakeGroups(("Goblin", EntityType.NPC));

        Assert.True(gsm.IsAttackable(entity, groups));
    }

    [Fact]
    public void IsAttackable_ActiveTrapWithHealth_ReturnsTrue()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "SpikeTrap", new Dictionary<string, string> { ["health"] = "5" });
        var groups = MakeGroups(("SpikeTrap", EntityType.Trap));

        Assert.True(gsm.IsAttackable(entity, groups));
    }

    [Fact]
    public void IsAttackable_ActiveItemWithHealth_ReturnsFalse()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Potion", new Dictionary<string, string> { ["health"] = "10" });
        var groups = MakeGroups(("Potion", EntityType.Item));

        Assert.False(gsm.IsAttackable(entity, groups));
    }

    [Fact]
    public void IsAttackable_ActiveTriggerWithHealth_ReturnsFalse()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "ExitDoor", new Dictionary<string, string> { ["health"] = "10" });
        var groups = MakeGroups(("ExitDoor", EntityType.Trigger));

        Assert.False(gsm.IsAttackable(entity, groups));
    }

    [Fact]
    public void IsAttackable_ActiveInteractableWithHealth_ReturnsFalse()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Chest", new Dictionary<string, string> { ["health"] = "10" });
        var groups = MakeGroups(("Chest", EntityType.Interactable));

        Assert.False(gsm.IsAttackable(entity, groups));
    }

    [Fact]
    public void IsAttackable_InactiveNpcWithHealth_ReturnsFalse()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Goblin", new Dictionary<string, string> { ["health"] = "10" }, isActive: false);
        var groups = MakeGroups(("Goblin", EntityType.NPC));

        Assert.False(gsm.IsAttackable(entity, groups));
    }

    [Fact]
    public void IsAttackable_ActiveNpcWithoutHealthProperty_ReturnsFalse()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Goblin");
        var groups = MakeGroups(("Goblin", EntityType.NPC));

        Assert.False(gsm.IsAttackable(entity, groups));
    }

    [Fact]
    public void IsAttackable_ActiveNpcWithZeroHealth_ReturnsFalse()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Goblin", new Dictionary<string, string> { ["health"] = "0" });
        var groups = MakeGroups(("Goblin", EntityType.NPC));

        Assert.False(gsm.IsAttackable(entity, groups));
    }

    [Fact]
    public void IsAttackable_UnknownDefinitionName_ReturnsFalse()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "UnknownEntity", new Dictionary<string, string> { ["health"] = "10" });
        var groups = new Dictionary<string, TileGroup>(); // empty — no matching definition

        Assert.False(gsm.IsAttackable(entity, groups));
    }

    // =========================================================================
    // AttackEntity — damage and result correctness
    // =========================================================================

    [Fact]
    public void AttackEntity_LowDefense_CorrectDamageDealt()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "20",
            ["max_health"] = "20",
            ["defense"] = "1",
        });
        gsm.State.ActiveEntities.Add(entity);

        var result = gsm.AttackEntity(entity, attackerAttack: 5);

        // CalculateDamage(5, 1) = 4
        Assert.Equal(4, result.DamageDealt);
    }

    [Fact]
    public void AttackEntity_HighDefense_MinimumOneDamage()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "20",
            ["max_health"] = "20",
            ["defense"] = "100",
        });
        gsm.State.ActiveEntities.Add(entity);

        var result = gsm.AttackEntity(entity, attackerAttack: 1);

        Assert.Equal(1, result.DamageDealt);
    }

    [Fact]
    public void AttackEntity_ReducesToZero_KilledTrueAndEntityDeactivated()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "3",
            ["max_health"] = "20",
            ["defense"] = "0",
        });
        gsm.State.ActiveEntities.Add(entity);

        var result = gsm.AttackEntity(entity, attackerAttack: 10);

        Assert.True(result.Killed);
        Assert.Equal(0, result.RemainingHealth);
        Assert.False(entity.IsActive);
    }

    [Fact]
    public void AttackEntity_NotKilled_CorrectRemainingHealthAndHitMessage()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "20",
            ["max_health"] = "20",
            ["defense"] = "1",
        });
        gsm.State.ActiveEntities.Add(entity);

        var result = gsm.AttackEntity(entity, attackerAttack: 5);

        // CalculateDamage(5, 1) = 4; 20 - 4 = 16 remaining
        Assert.False(result.Killed);
        Assert.Equal(16, result.RemainingHealth);
        Assert.Equal("Hit Goblin for 4! (16/20 HP)", result.Message);
    }

    [Fact]
    public void AttackEntity_KillWithXpProperty_MessageIncludesXp()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "1",
            ["max_health"] = "10",
            ["defense"] = "0",
            ["xp"] = "50",
        });
        gsm.State.ActiveEntities.Add(entity);

        var result = gsm.AttackEntity(entity, attackerAttack: 5);

        Assert.True(result.Killed);
        Assert.Equal("Goblin defeated! (+50 XP)", result.Message);
    }

    [Fact]
    public void AttackEntity_KillWithoutXpProperty_MessageOmitsXp()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "1",
            ["max_health"] = "10",
            ["defense"] = "0",
        });
        gsm.State.ActiveEntities.Add(entity);

        var result = gsm.AttackEntity(entity, attackerAttack: 5);

        Assert.True(result.Killed);
        Assert.Equal("Goblin defeated!", result.Message);
    }

    [Fact]
    public void AttackEntity_KillSetsInactivePersistenceFlag()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("enemy_01", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "1",
            ["defense"] = "0",
        });
        gsm.State.ActiveEntities.Add(entity);

        gsm.AttackEntity(entity, attackerAttack: 5);

        Assert.True(gsm.HasFlag(GameStateManager.EntityInactivePrefix + "enemy_01"));
    }

    [Fact]
    public void AttackEntity_ReturnsCorrectTargetName()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("e1", "Orc", new Dictionary<string, string>
        {
            ["health"] = "20",
            ["defense"] = "0",
        });
        gsm.State.ActiveEntities.Add(entity);

        var result = gsm.AttackEntity(entity, attackerAttack: 3);

        Assert.Equal("Orc", result.TargetName);
    }

    // =========================================================================
    // AttackEntity — kill event hooks
    // =========================================================================

    [Fact]
    public void AttackEntity_Kill_WithOnKillSetFlag_SetsFlag()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("goblin_01", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "1",
            ["defense"] = "0",
            ["on_kill_set_flag"] = "goblin_slain",
        });
        gsm.State.ActiveEntities.Add(entity);

        gsm.AttackEntity(entity, attackerAttack: 5);

        Assert.True(gsm.HasFlag("goblin_slain"));
    }

    [Fact]
    public void AttackEntity_Kill_WithOnKillIncrement_IncrementsVariable()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("goblin_01", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "1",
            ["defense"] = "0",
            ["on_kill_increment"] = "goblin_kills",
        });
        gsm.State.ActiveEntities.Add(entity);

        gsm.AttackEntity(entity, attackerAttack: 5);

        Assert.Equal("1", gsm.GetVariable("goblin_kills"));
    }

    [Fact]
    public void AttackEntity_Kill_WithOnKillIncrement_IncrementsMultipleTimes()
    {
        var gsm = new GameStateManager();
        var entity1 = MakeEntity("goblin_01", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "1",
            ["defense"] = "0",
            ["on_kill_increment"] = "goblin_kills",
        });
        var entity2 = MakeEntity("goblin_02", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "1",
            ["defense"] = "0",
            ["on_kill_increment"] = "goblin_kills",
        });
        gsm.State.ActiveEntities.Add(entity1);
        gsm.State.ActiveEntities.Add(entity2);

        gsm.AttackEntity(entity1, attackerAttack: 5);
        gsm.AttackEntity(entity2, attackerAttack: 5);

        Assert.Equal("2", gsm.GetVariable("goblin_kills"));
    }

    [Fact]
    public void AttackEntity_NotKilled_DoesNotProcessHooks()
    {
        var gsm = new GameStateManager();
        var entity = MakeEntity("goblin_01", "Goblin", new Dictionary<string, string>
        {
            ["health"] = "100",
            ["defense"] = "0",
            ["on_kill_set_flag"] = "flag",
        });
        gsm.State.ActiveEntities.Add(entity);

        gsm.AttackEntity(entity, attackerAttack: 1);

        Assert.False(gsm.HasFlag("flag"));
    }
}
