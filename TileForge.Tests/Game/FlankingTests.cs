using System.Collections.Generic;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class FlankingTests
{
    // GetAttackPosition — facing Up

    [Fact]
    public void GetAttackPosition_DefenderFacingUp_AttackerBehind_IsBackstab()
    {
        // Defender at (5,5) facing Up, attacker at (5,6) — behind
        var pos = CombatHelper.GetAttackPosition(5, 6, 5, 5, Direction.Up);
        Assert.Equal(AttackPosition.Backstab, pos);
    }

    [Fact]
    public void GetAttackPosition_DefenderFacingUp_AttackerInFront_IsFront()
    {
        // Defender at (5,5) facing Up, attacker at (5,4) — in front
        var pos = CombatHelper.GetAttackPosition(5, 4, 5, 5, Direction.Up);
        Assert.Equal(AttackPosition.Front, pos);
    }

    [Fact]
    public void GetAttackPosition_DefenderFacingUp_AttackerToSide_IsFlank()
    {
        // Defender at (5,5) facing Up, attacker at (4,5) — left side
        var pos = CombatHelper.GetAttackPosition(4, 5, 5, 5, Direction.Up);
        Assert.Equal(AttackPosition.Flank, pos);
    }

    // GetAttackPosition — facing Down

    [Fact]
    public void GetAttackPosition_DefenderFacingDown_AttackerBehind_IsBackstab()
    {
        // Defender at (5,5) facing Down, attacker at (5,4) — behind
        var pos = CombatHelper.GetAttackPosition(5, 4, 5, 5, Direction.Down);
        Assert.Equal(AttackPosition.Backstab, pos);
    }

    [Fact]
    public void GetAttackPosition_DefenderFacingDown_AttackerInFront_IsFront()
    {
        // Defender at (5,5) facing Down, attacker at (5,6) — in front
        var pos = CombatHelper.GetAttackPosition(5, 6, 5, 5, Direction.Down);
        Assert.Equal(AttackPosition.Front, pos);
    }

    [Fact]
    public void GetAttackPosition_DefenderFacingDown_AttackerToSide_IsFlank()
    {
        var pos = CombatHelper.GetAttackPosition(6, 5, 5, 5, Direction.Down);
        Assert.Equal(AttackPosition.Flank, pos);
    }

    // GetAttackPosition — facing Left

    [Fact]
    public void GetAttackPosition_DefenderFacingLeft_AttackerBehind_IsBackstab()
    {
        // Defender at (5,5) facing Left, attacker at (6,5) — behind (right side = behind when facing left)
        var pos = CombatHelper.GetAttackPosition(6, 5, 5, 5, Direction.Left);
        Assert.Equal(AttackPosition.Backstab, pos);
    }

    [Fact]
    public void GetAttackPosition_DefenderFacingLeft_AttackerInFront_IsFront()
    {
        var pos = CombatHelper.GetAttackPosition(4, 5, 5, 5, Direction.Left);
        Assert.Equal(AttackPosition.Front, pos);
    }

    [Fact]
    public void GetAttackPosition_DefenderFacingLeft_AttackerToSide_IsFlank()
    {
        var pos = CombatHelper.GetAttackPosition(5, 4, 5, 5, Direction.Left);
        Assert.Equal(AttackPosition.Flank, pos);
    }

    // GetAttackPosition — facing Right

    [Fact]
    public void GetAttackPosition_DefenderFacingRight_AttackerBehind_IsBackstab()
    {
        // Defender at (5,5) facing Right, attacker at (4,5) — behind
        var pos = CombatHelper.GetAttackPosition(4, 5, 5, 5, Direction.Right);
        Assert.Equal(AttackPosition.Backstab, pos);
    }

    [Fact]
    public void GetAttackPosition_DefenderFacingRight_AttackerInFront_IsFront()
    {
        var pos = CombatHelper.GetAttackPosition(6, 5, 5, 5, Direction.Right);
        Assert.Equal(AttackPosition.Front, pos);
    }

    [Fact]
    public void GetAttackPosition_DefenderFacingRight_AttackerToSide_IsFlank()
    {
        var pos = CombatHelper.GetAttackPosition(5, 6, 5, 5, Direction.Right);
        Assert.Equal(AttackPosition.Flank, pos);
    }

    // Multiplier values

    [Fact]
    public void GetPositionMultiplier_Front_IsOne()
    {
        Assert.Equal(1.0f, CombatHelper.GetPositionMultiplier(AttackPosition.Front));
    }

    [Fact]
    public void GetPositionMultiplier_Flank_Is1Point5()
    {
        Assert.Equal(1.5f, CombatHelper.GetPositionMultiplier(AttackPosition.Flank));
    }

    [Fact]
    public void GetPositionMultiplier_Backstab_Is2()
    {
        Assert.Equal(2.0f, CombatHelper.GetPositionMultiplier(AttackPosition.Backstab));
    }

    // Damage with multiplier

    [Fact]
    public void CalculateDamage_BackstabMultiplier_DoublesDamage()
    {
        // attack=10, defense=2, terrain=0, mult=2.0 → base=8, final=16
        Assert.Equal(16, CombatHelper.CalculateDamage(10, 2, 0, 2.0f));
    }

    [Fact]
    public void CalculateDamage_FlankMultiplier_Applies()
    {
        // attack=10, defense=2, terrain=0, mult=1.5 → base=8, final=(int)(8*1.5)=12
        Assert.Equal(12, CombatHelper.CalculateDamage(10, 2, 0, 1.5f));
    }

    [Fact]
    public void CalculateDamage_BackstabWithTerrain_Combined()
    {
        // attack=10, defense=2, terrain=3, mult=2.0 → base=max(1,10-(2+3))=5, final=10
        Assert.Equal(10, CombatHelper.CalculateDamage(10, 2, 3, 2.0f));
    }

    [Fact]
    public void CalculateDamage_FlankWithHighDefense_MinimumOne()
    {
        // attack=3, defense=5, terrain=5, mult=1.5 → base=max(1,3-(5+5))=1, final=max(1,(int)(1*1.5))=1
        Assert.Equal(1, CombatHelper.CalculateDamage(3, 5, 5, 1.5f));
    }

    [Fact]
    public void AttackEntity_BackstabMultiplier_DoublesDamage()
    {
        var mgr = new GameStateManager();
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "goblin", X = 3, Y = 3, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["health"] = "50", ["max_health"] = "50", ["defense"] = "2"
            }
        };
        mgr.State.ActiveEntities.Add(entity);

        // attack=10, defense=2, terrain=0, mult=2.0 → damage=16
        var result = mgr.AttackEntity(entity, 10, 0, 2.0f);
        Assert.Equal(16, result.DamageDealt);
        Assert.Equal(34, result.RemainingHealth);
    }
}
