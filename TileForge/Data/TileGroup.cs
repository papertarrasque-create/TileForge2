using System.Collections.Generic;
using TileForge.Game;

namespace TileForge.Data;

public enum GroupType { Tile, Entity }

public class SpriteRef
{
    public int Col { get; set; }
    public int Row { get; set; }
}

public class TileGroup
{
    public string Name { get; set; }
    public GroupType Type { get; set; }
    public List<SpriteRef> Sprites { get; set; } = new();
    public bool IsSolid { get; set; }
    public bool IsPlayer { get; set; }
    public string LayerName { get; set; }

    // G1.1 — Tile gameplay properties
    public bool IsPassable { get; set; } = true;
    public bool IsHazardous { get; set; }
    public float MovementCost { get; set; } = 1.0f;
    public string DamageType { get; set; }
    public int DamagePerTick { get; set; }

    // G1.2 — Entity type (for Entity groups)
    public EntityType EntityType { get; set; } = EntityType.Interactable;

    // G6.4 — Default properties inherited by placed entity instances
    public Dictionary<string, string> DefaultProperties { get; set; } = new();
}
