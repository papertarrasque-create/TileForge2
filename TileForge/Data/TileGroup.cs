using System.Collections.Generic;

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
}
