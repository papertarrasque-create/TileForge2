using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileForge.Game;

/// <summary>
/// Evaluates Condition objects against game state.
/// </summary>
public static class ConditionEvaluator
{
    /// <summary>
    /// Returns true if ALL conditions pass (AND logic). Null/empty = true.
    /// </summary>
    public static bool EvaluateAll(List<Condition> conditions, GameStateManager gsm)
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        foreach (var condition in conditions)
        {
            if (!Evaluate(condition, gsm))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns true if a single condition passes.
    /// </summary>
    public static bool Evaluate(Condition condition, GameStateManager gsm)
    {
        return condition.Type switch
        {
            "has_flag" => gsm.HasFlag(condition.Flag),
            "not_flag" => !gsm.HasFlag(condition.Flag),
            "has_item" => gsm.HasItem(condition.Item),
            "variable_eq" => gsm.GetVariable(condition.Variable) == condition.Value,
            "variable_gte" => CompareVariable(gsm.GetVariable(condition.Variable), condition.Value, (a, b) => a >= b),
            "variable_lt" => CompareVariable(gsm.GetVariable(condition.Variable), condition.Value, (a, b) => a < b),
            "quest_active" => gsm.HasFlag(QuestConstants.StartedFlag(condition.Value)) && !gsm.HasFlag(QuestConstants.CompleteFlag(condition.Value)),
            "quest_complete" => gsm.HasFlag(QuestConstants.CompleteFlag(condition.Value)),
            _ => false,
        };
    }

    private static bool CompareVariable(string variable, string value, System.Func<int, int, bool> compare)
    {
        if (!int.TryParse(variable, out int varInt))
            return false;
        if (!int.TryParse(value, out int valInt))
            return false;
        return compare(varInt, valInt);
    }
}

/// <summary>
/// Executes DialogueAction objects against game state.
/// </summary>
public static class ActionExecutor
{
    /// <summary>
    /// Executes all actions in order. Null/empty = no-op.
    /// </summary>
    public static void ExecuteAll(List<DialogueAction> actions, GameStateManager gsm, GameLog gameLog = null)
    {
        if (actions == null || actions.Count == 0)
            return;

        foreach (var action in actions)
        {
            Execute(action, gsm, gameLog);
        }
    }

    /// <summary>
    /// Executes a single action.
    /// </summary>
    public static void Execute(DialogueAction action, GameStateManager gsm, GameLog gameLog = null)
    {
        switch (action.Type)
        {
            case "set_flag":
                gsm.SetFlag(action.Value);
                break;
            case "set_variable":
                gsm.SetVariable(action.Key, action.Value);
                break;
            case "increment":
                gsm.IncrementVariable(action.Value);
                break;
            case "give_item":
                gsm.AddToInventory(action.Value);
                break;
            case "remove_item":
                gsm.RemoveFromInventory(action.Value);
                break;
            case "start_quest":
                gsm.SetFlag(QuestConstants.StartedFlag(action.Value));
                break;
            case "complete_objective":
                gsm.SetFlag(QuestConstants.ObjectiveFlag(action.Value));
                break;
            case "heal":
                if (int.TryParse(action.Value, out int healAmount))
                    gsm.HealPlayer(healAmount);
                break;
            case "damage":
                if (int.TryParse(action.Value, out int damageAmount))
                    gsm.DamagePlayer(damageAmount);
                break;
            case "log":
                gameLog?.Add(action.Text, ParseColor(action.Color));
                break;
        }
    }

    private static Color ParseColor(string colorName)
    {
        return colorName?.ToLowerInvariant() switch
        {
            "yellow" => Color.Yellow,
            "red" => Color.Red,
            "green" => Color.Green,
            "cyan" => Color.Cyan,
            "white" => Color.White,
            _ => Color.White,
        };
    }
}
