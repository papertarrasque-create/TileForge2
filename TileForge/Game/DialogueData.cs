using System.Collections.Generic;
using System.Linq;

namespace TileForge.Game;

public class DialogueData
{
    public string Id { get; set; }
    public string Type { get; set; }                    // "conversation" (default), future: "bark", "cutscene", etc.
    public List<DialogueRoute> Routes { get; set; }     // Ordered conditional entry points
    public List<DialogueNode> Nodes { get; set; } = new();
    public bool? OneShot { get; set; }                  // Auto-set "dialogue_shown:{id}" after completion

    /// <summary>
    /// Evaluates routes and returns the ID of the first matching start node,
    /// or falls back to the first node in the list.
    /// </summary>
    public string ResolveStartNodeId(GameStateManager gsm)
    {
        if (Routes != null)
        {
            foreach (var route in Routes)
            {
                if (ConditionEvaluator.EvaluateAll(route.Conditions, gsm))
                    return route.StartNode;
            }
        }
        return Nodes.FirstOrDefault()?.Id;
    }
}

public class DialogueRoute
{
    public string StartNode { get; set; }
    public List<Condition> Conditions { get; set; }     // All must pass; empty/null = always matches
}

public class DialogueNode
{
    public string Id { get; set; }
    public string Speaker { get; set; }
    public string Text { get; set; }
    public List<DialogueChoice> Choices { get; set; }   // null = auto-advance (linear)
    public string NextNodeId { get; set; }              // for linear sequences
    public List<Condition> Conditions { get; set; }     // v2: replaces RequiresFlag
    public List<DialogueAction> Actions { get; set; }   // v2: replaces SetsFlag + SetsVariable
    public List<string> Tags { get; set; }              // extension hook (not interpreted yet)
    public int? EditorX { get; set; }
    public int? EditorY { get; set; }

    // v1 compat — deserialized from old JSON, converted to Conditions/Actions by migration
    public string RequiresFlag { get; set; }
    public string SetsFlag { get; set; }
    public string SetsVariable { get; set; }
}

public class DialogueChoice
{
    public string Text { get; set; }
    public string NextNodeId { get; set; }
    public List<Condition> Conditions { get; set; }     // v2: replaces RequiresFlag
    public List<DialogueAction> Actions { get; set; }   // v2: replaces SetsFlag

    // v1 compat — deserialized from old JSON, converted by migration
    public string RequiresFlag { get; set; }
    public string SetsFlag { get; set; }
}

public class Condition
{
    public string Type { get; set; }      // has_flag, not_flag, has_item, variable_eq, variable_gte, variable_lt, quest_active, quest_complete
    public string Flag { get; set; }      // for flag conditions
    public string Item { get; set; }      // for item conditions
    public string Variable { get; set; }  // for variable conditions
    public string Value { get; set; }     // comparison value
    public string Operator { get; set; }  // eq, gte, lt (for variable conditions in routes)
}

public class DialogueAction
{
    public string Type { get; set; }      // set_flag, clear_flag, set_variable, increment, give_item, remove_item, start_quest, complete_objective, heal, damage, log, map_transition
    public string Value { get; set; }     // primary value (flag name, variable value, heal amount, etc.)
    public string Key { get; set; }       // secondary key (variable name, item name)
    public string Color { get; set; }     // for log action
    public string Text { get; set; }      // for log action
}
