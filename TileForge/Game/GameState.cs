using System.Collections.Generic;

namespace TileForge.Game;

public class GameState
{
    public int Version { get; set; } = 2;
    public PlayerState Player { get; set; } = new();
    public string CurrentMapId { get; set; }
    public List<EntityInstance> ActiveEntities { get; set; } = new();
    public HashSet<string> Flags { get; set; } = new();
    public Dictionary<string, string> Variables { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> ItemPropertyCache { get; set; } = new();
}
