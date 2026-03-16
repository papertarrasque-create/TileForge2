using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TileForge.Game;

/// <summary>
/// Root container for quest definitions loaded from JSON.
/// </summary>
public class QuestFile
{
    public List<QuestDefinition> Quests { get; set; } = new();
}

/// <summary>
/// A single quest definition: start condition, objectives, completion flag, and rewards.
/// Loaded from quests.json, immutable at runtime.
/// </summary>
public class QuestDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    /// <summary>
    /// Flag that must be set for this quest to become active.
    /// Computed from Id (e.g., "quest_started:rescue_villager").
    /// </summary>
    [JsonIgnore]
    public string StartFlag => Id != null ? QuestConstants.StartedFlag(Id) : null;

    public List<QuestObjective> Objectives { get; set; } = new();

    /// <summary>
    /// Flag set automatically when all objectives are met.
    /// Computed from Id (e.g., "quest_complete:rescue_villager").
    /// </summary>
    [JsonIgnore]
    public string CompletionFlag => Id != null ? QuestConstants.CompleteFlag(Id) : null;

    public List<DialogueAction> Rewards { get; set; } = new();
}

/// <summary>
/// A single quest objective evaluated against GameState flags/variables.
/// </summary>
public class QuestObjective
{
    public string Description { get; set; }

    /// <summary>
    /// Condition type: "flag" (flag must be set), "variable_gte" (variable >= value),
    /// "variable_eq" (variable == value).
    /// </summary>
    public string Type { get; set; }

    /// <summary>For "flag" type: the flag name that must exist in GameState.Flags.</summary>
    public string Flag { get; set; }

    /// <summary>For variable types: the variable key to check in GameState.Variables.</summary>
    public string Variable { get; set; }

    /// <summary>For variable types: the target value (integer comparison).</summary>
    public int Value { get; set; }
}