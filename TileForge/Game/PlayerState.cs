using System.Collections.Generic;

namespace TileForge.Game;

public class PlayerState
{
    public int X { get; set; }
    public int Y { get; set; }
    public Direction Facing { get; set; } = Direction.Down;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public List<string> Inventory { get; set; } = new();
    public List<StatusEffect> ActiveEffects { get; set; } = new();
    public int Attack { get; set; } = 5;
    public int Defense { get; set; } = 2;
}
