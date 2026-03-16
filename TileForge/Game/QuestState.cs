using System.Collections.Generic;

namespace TileForge.Game;

public enum QuestStatus
{
    NotStarted,
    Active,
    Completed,
}

public class QuestState
{
    public string QuestId { get; set; }
    public QuestStatus Status { get; set; } = QuestStatus.NotStarted;
    public HashSet<string> CompletedObjectives { get; set; } = new();
}
