namespace TileForge.Game;

public enum QuestEventType
{
    QuestStarted,
    ObjectiveCompleted,
    QuestCompleted,
}

/// <summary>
/// Represents a quest state change detected during evaluation.
/// Used by the HUD to display notifications.
/// </summary>
public class QuestEvent
{
    public QuestEventType Type { get; set; }
    public string QuestId { get; set; }
    public string QuestName { get; set; }
    public string ObjectiveDescription { get; set; }
}
