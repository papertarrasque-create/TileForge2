using System.Collections.Generic;
using TileForge.Data;

namespace TileForge.Game;

public class GameWorldView
{
    public Dictionary<string, MapView> Maps { get; set; } = new();
    public string ActiveMapId { get; set; }
    public Dictionary<string, GroupView> Groups { get; set; } = new();
    public Dictionary<string, DialogueData> Dialogues { get; set; } = new();
    public List<QuestDefinition> Quests { get; set; } = new();
    public WorldLayout WorldLayout { get; set; }
}

public class MapView
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<LayerView> Layers { get; set; } = new();
}

public class LayerView
{
    public int[,] Tiles { get; set; }
    public List<EntityView> Entities { get; set; } = new();
}

public class EntityView
{
    public string Id { get; set; }
    public string GroupName { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class GroupView
{
    public string Name { get; set; }
    public GroupType Type { get; set; }
    public int SpriteIndex { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}
