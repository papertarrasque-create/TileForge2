using System.Collections.Generic;

namespace TileForge.Game;

public enum QuestStatus
{
    NotStarted,
    Active,
    Completed,
}

/// <summary>
/// Evaluates quest definitions against the current GameState to detect
/// quest starts, objective completions, and quest completions.
/// Pure logic — no MonoGame, no side effects beyond flag/variable mutations via GameStateManager.
/// </summary>
public class QuestManager
{
    private readonly List<QuestDefinition> _quests;

    // Track reported events to prevent duplicate notifications.
    // Session-only — not serialized. On load, re-evaluates from flags.
    private readonly HashSet<string> _reportedObjectives = new();
    private readonly HashSet<string> _reportedStarts = new();

    public IReadOnlyList<QuestDefinition> Quests => _quests;

    public QuestManager(List<QuestDefinition> quests)
    {
        _quests = quests ?? new List<QuestDefinition>();
    }

    /// <summary>
    /// Evaluates all quests against the current game state.
    /// Returns a list of new events (started, objective met, completed).
    /// Applies completion rewards when a quest completes.
    /// Call after any state-changing action (kill, collect, dialogue, map switch).
    /// </summary>
    public List<QuestEvent> CheckForUpdates(GameStateManager gsm)
    {
        var events = new List<QuestEvent>();

        foreach (var quest in _quests)
        {
            // Already completed — skip
            if (!string.IsNullOrEmpty(quest.CompletionFlag) &&
                gsm.HasFlag(quest.CompletionFlag))
                continue;

            // Not yet started — check start condition
            if (!string.IsNullOrEmpty(quest.StartFlag) &&
                !gsm.HasFlag(quest.StartFlag))
                continue;

            // Quest is active — report start if not yet reported
            if (_reportedStarts.Add(quest.Id))
            {
                events.Add(new QuestEvent
                {
                    Type = QuestEventType.QuestStarted,
                    QuestId = quest.Id,
                    QuestName = quest.Name,
                });
            }

            // Check each objective
            bool allComplete = true;
            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                var obj = quest.Objectives[i];
                bool met = EvaluateObjective(obj, gsm);

                if (met)
                {
                    string key = $"{quest.Id}:{i}";
                    if (_reportedObjectives.Add(key))
                    {
                        events.Add(new QuestEvent
                        {
                            Type = QuestEventType.ObjectiveCompleted,
                            QuestId = quest.Id,
                            QuestName = quest.Name,
                            ObjectiveDescription = obj.Description,
                        });
                    }
                }
                else
                {
                    allComplete = false;
                }
            }

            // All objectives met — complete the quest
            if (allComplete && quest.Objectives.Count > 0)
            {
                CompleteQuest(quest, gsm);
                events.Add(new QuestEvent
                {
                    Type = QuestEventType.QuestCompleted,
                    QuestId = quest.Id,
                    QuestName = quest.Name,
                });
            }
        }

        return events;
    }

    /// <summary>
    /// Returns the current status of a quest: NotStarted, Active, or Completed.
    /// </summary>
    public QuestStatus GetQuestStatus(QuestDefinition quest, GameStateManager gsm)
    {
        if (!string.IsNullOrEmpty(quest.CompletionFlag) && gsm.HasFlag(quest.CompletionFlag))
            return QuestStatus.Completed;
        if (!string.IsNullOrEmpty(quest.StartFlag) && !gsm.HasFlag(quest.StartFlag))
            return QuestStatus.NotStarted;
        return QuestStatus.Active;
    }

    /// <summary>
    /// Checks if a single objective is met based on current game state.
    /// Static and public so QuestLogScreen can reuse without duplicating logic.
    /// </summary>
    public static bool EvaluateObjective(QuestObjective objective, GameStateManager gsm)
    {
        return objective.Type switch
        {
            "flag" => !string.IsNullOrEmpty(objective.Flag) && gsm.HasFlag(objective.Flag),
            "variable_gte" => ParseVariable(gsm.GetVariable(objective.Variable)) >= objective.Value,
            "variable_eq" => ParseVariable(gsm.GetVariable(objective.Variable)) == objective.Value,
            _ => false,
        };
    }

    private void CompleteQuest(QuestDefinition quest, GameStateManager gsm)
    {
        if (!string.IsNullOrEmpty(quest.CompletionFlag))
            gsm.SetFlag(quest.CompletionFlag);

        if (quest.Rewards != null)
        {
            foreach (var flag in quest.Rewards.SetFlags)
                gsm.SetFlag(flag);
            foreach (var kvp in quest.Rewards.SetVariables)
                gsm.SetVariable(kvp.Key, kvp.Value);
        }
    }

    private static int ParseVariable(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        return int.TryParse(value, out var result) ? result : 0;
    }
}
