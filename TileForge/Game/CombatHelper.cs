using System;

namespace TileForge.Game;

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
}

public class AttackResult
{
    public int DamageDealt { get; set; }
    public int RemainingHealth { get; set; }
    public bool Killed { get; set; }
    public string TargetName { get; set; }
    public string Message { get; set; }
}
