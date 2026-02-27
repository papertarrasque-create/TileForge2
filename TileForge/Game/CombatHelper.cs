using System;

namespace TileForge.Game;

public enum AttackPosition { Front, Flank, Backstab }

public static class CombatHelper
{
    /// <summary>
    /// Calculates damage dealt: max(1, attack - defense).
    /// Floor of 1 ensures hits always do something.
    /// </summary>
    public static int CalculateDamage(int attack, int defense)
    {
        return Math.Max(1, attack - defense);
    }

    /// <summary>
    /// Calculates damage with terrain defense bonus added to defender's defense.
    /// </summary>
    public static int CalculateDamage(int attack, int defense, int terrainDefenseBonus)
    {
        return Math.Max(1, attack - (defense + terrainDefenseBonus));
    }

    /// <summary>
    /// Full damage calculation with terrain bonus and positional multiplier (backstab/flank).
    /// </summary>
    public static int CalculateDamage(int attack, int defense, int terrainBonus, float positionMultiplier)
    {
        int baseDamage = Math.Max(1, attack - (defense + terrainBonus));
        return Math.Max(1, (int)(baseDamage * positionMultiplier));
    }

    /// <summary>
    /// Determines the attacker's position relative to the defender's facing direction.
    /// Backstab = behind, Flank = side, Front = facing the attacker.
    /// </summary>
    public static AttackPosition GetAttackPosition(
        int attackerX, int attackerY,
        int defenderX, int defenderY,
        Direction defenderFacing)
    {
        int dx = attackerX - defenderX;
        int dy = attackerY - defenderY;

        return defenderFacing switch
        {
            Direction.Up => dy > 0 ? AttackPosition.Backstab
                          : dy < 0 ? AttackPosition.Front
                          : AttackPosition.Flank,
            Direction.Down => dy < 0 ? AttackPosition.Backstab
                            : dy > 0 ? AttackPosition.Front
                            : AttackPosition.Flank,
            Direction.Left => dx > 0 ? AttackPosition.Backstab
                            : dx < 0 ? AttackPosition.Front
                            : AttackPosition.Flank,
            Direction.Right => dx < 0 ? AttackPosition.Backstab
                             : dx > 0 ? AttackPosition.Front
                             : AttackPosition.Flank,
            _ => AttackPosition.Front,
        };
    }

    public static float GetPositionMultiplier(AttackPosition pos) => pos switch
    {
        AttackPosition.Backstab => 2.0f,
        AttackPosition.Flank => 1.5f,
        _ => 1.0f,
    };
}

public class AttackResult
{
    public int DamageDealt { get; set; }
    public int RemainingHealth { get; set; }
    public bool Killed { get; set; }
    public string TargetName { get; set; }
    public string Message { get; set; }
}
