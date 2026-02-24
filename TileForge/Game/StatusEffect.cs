using System.Collections.Generic;

namespace TileForge.Game;

public class StatusEffect
{
    public string Type { get; set; }         // "fire", "poison", "ice"
    public int RemainingSteps { get; set; }
    public int DamagePerStep { get; set; }   // 0 for non-damage effects
    public float MovementMultiplier { get; set; } = 1.0f;  // 1.0 = normal
}
