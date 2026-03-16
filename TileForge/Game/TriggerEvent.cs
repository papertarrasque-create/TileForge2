using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileForge.Game;

public enum TriggerSource
{
    Interaction,
    Pickup,
    StepOn,
    Proximity,
    SkillCheck,
    Timer,
}

public class TriggerEvent
{
    public TriggerSource Source { get; set; }
    public string EntityId { get; set; }
    public Point? TilePosition { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class TriggerResult
{
    public DialogueData Dialogue { get; set; }
    public string StartNodeId { get; set; }
}
