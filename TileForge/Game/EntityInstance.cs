using System.Collections.Generic;

namespace TileForge.Game;

public class EntityInstance
{
    public string Id { get; set; }
    public string DefinitionName { get; set; }   // reference to TileGroup/EntityRegistry
    public int X { get; set; }
    public int Y { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public bool IsActive { get; set; } = true;   // false = collected/destroyed
}
