namespace TileForge.Game;

public static class QuestConstants
{
    public const string StartedPrefix = "quest_started:";
    public const string CompletePrefix = "quest_complete:";
    public const string ObjectivePrefix = "objective_complete:";

    public static string StartedFlag(string questId) => StartedPrefix + questId;
    public static string CompleteFlag(string questId) => CompletePrefix + questId;
    public static string ObjectiveFlag(string objectiveId) => ObjectivePrefix + objectiveId;
}
