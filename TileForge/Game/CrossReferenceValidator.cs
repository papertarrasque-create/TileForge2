using System;
using System.Collections.Generic;
using System.Linq;

namespace TileForge.Game;

/// <summary>
/// Describes a dialogue action that references a quest.
/// </summary>
public class QuestReference
{
    public string DialogueId { get; init; }
    public string NodeId { get; init; }
    public string ActionType { get; init; }
    public string QuestId { get; init; }
}

/// <summary>
/// Describes a dialogue action that references a quest or dialogue that does not exist.
/// </summary>
public class BrokenReference
{
    public string DialogueId { get; init; }
    public string NodeId { get; init; }
    public string ActionType { get; init; }
    public string ReferencedId { get; init; }
}

/// <summary>
/// Static validator that checks cross-references between dialogues and quests.
/// </summary>
public static class CrossReferenceValidator
{
    /// <summary>
    /// Scans all dialogues for actions that reference the given quest ID via
    /// start_quest (exact match) or complete_objective ("questId:..." prefix).
    /// Both node.Actions and node.Choices[].Actions are checked.
    /// </summary>
    public static List<QuestReference> FindDialoguesReferencingQuest(
        string questId,
        List<DialogueData> dialogues)
    {
        var results = new List<QuestReference>();

        foreach (var dialogue in dialogues ?? Enumerable.Empty<DialogueData>())
        {
            foreach (var node in dialogue.Nodes ?? Enumerable.Empty<DialogueNode>())
            {
                CollectQuestRefsFromActions(dialogue.Id, node.Id, node.Actions, questId, results);

                foreach (var choice in node.Choices ?? Enumerable.Empty<DialogueChoice>())
                    CollectQuestRefsFromActions(dialogue.Id, node.Id, choice.Actions, questId, results);
            }
        }

        return results;
    }

    /// <summary>
    /// For a specific dialogue, finds all quest IDs referenced by its node and choice actions,
    /// filtering to those that exist in the provided quests list.
    /// </summary>
    public static List<QuestReference> FindQuestsReferencedByDialogue(
        DialogueData dialogue,
        List<QuestDefinition> quests)
    {
        var questIds = new HashSet<string>(quests?.Select(q => q.Id) ?? Enumerable.Empty<string>());
        var results = new List<QuestReference>();

        foreach (var node in dialogue?.Nodes ?? Enumerable.Empty<DialogueNode>())
        {
            CollectAllQuestRefs(dialogue.Id, node.Id, node.Actions, questIds, results);

            foreach (var choice in node.Choices ?? Enumerable.Empty<DialogueChoice>())
                CollectAllQuestRefs(dialogue.Id, node.Id, choice.Actions, questIds, results);
        }

        return results;
    }

    /// <summary>
    /// Finds all dialogue actions referencing quests that don't exist in the provided quests list.
    /// Checks both node.Actions and node.Choices[].Actions.
    /// </summary>
    public static List<BrokenReference> FindBrokenQuestReferences(
        List<DialogueData> dialogues,
        List<QuestDefinition> quests)
    {
        var questIds = new HashSet<string>(quests?.Select(q => q.Id) ?? Enumerable.Empty<string>());
        var results = new List<BrokenReference>();

        foreach (var dialogue in dialogues ?? Enumerable.Empty<DialogueData>())
        {
            foreach (var node in dialogue.Nodes ?? Enumerable.Empty<DialogueNode>())
            {
                CollectBrokenQuestRefs(dialogue.Id, node.Id, node.Actions, questIds, results);

                foreach (var choice in node.Choices ?? Enumerable.Empty<DialogueChoice>())
                    CollectBrokenQuestRefs(dialogue.Id, node.Id, choice.Actions, questIds, results);
            }
        }

        return results;
    }

    /// <summary>
    /// Finds entity dialogue_id values that don't match any loaded dialogue ID.
    /// Returns the list of unresolved dialogue ID strings.
    /// </summary>
    public static List<string> FindBrokenDialogueReferences(
        List<string> entityDialogueIds,
        List<DialogueData> dialogues)
    {
        var knownIds = new HashSet<string>(dialogues?.Select(d => d.Id) ?? Enumerable.Empty<string>());
        return (entityDialogueIds ?? Enumerable.Empty<string>())
            .Where(id => !string.IsNullOrEmpty(id) && !knownIds.Contains(id))
            .ToList();
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    /// <summary>
    /// Extracts the quest ID from an action. Returns null if the action doesn't reference a quest.
    /// </summary>
    private static string ExtractQuestId(DialogueAction action)
    {
        if (action == null) return null;

        if (action.Type == "start_quest")
            return action.Value;

        if (action.Type == "complete_objective" && !string.IsNullOrEmpty(action.Value))
        {
            // format: "quest_id:objective_id"
            var colonIndex = action.Value.IndexOf(':');
            return colonIndex >= 0 ? action.Value[..colonIndex] : action.Value;
        }

        return null;
    }

    private static void CollectQuestRefsFromActions(
        string dialogueId, string nodeId,
        List<DialogueAction> actions,
        string targetQuestId,
        List<QuestReference> results)
    {
        foreach (var action in actions ?? Enumerable.Empty<DialogueAction>())
        {
            var qid = ExtractQuestId(action);
            if (qid == targetQuestId)
            {
                results.Add(new QuestReference
                {
                    DialogueId = dialogueId,
                    NodeId = nodeId,
                    ActionType = action.Type,
                    QuestId = qid,
                });
            }
        }
    }

    private static void CollectAllQuestRefs(
        string dialogueId, string nodeId,
        List<DialogueAction> actions,
        HashSet<string> validQuestIds,
        List<QuestReference> results)
    {
        foreach (var action in actions ?? Enumerable.Empty<DialogueAction>())
        {
            var qid = ExtractQuestId(action);
            if (qid != null && validQuestIds.Contains(qid))
            {
                results.Add(new QuestReference
                {
                    DialogueId = dialogueId,
                    NodeId = nodeId,
                    ActionType = action.Type,
                    QuestId = qid,
                });
            }
        }
    }

    private static void CollectBrokenQuestRefs(
        string dialogueId, string nodeId,
        List<DialogueAction> actions,
        HashSet<string> validQuestIds,
        List<BrokenReference> results)
    {
        foreach (var action in actions ?? Enumerable.Empty<DialogueAction>())
        {
            var qid = ExtractQuestId(action);
            if (qid != null && !validQuestIds.Contains(qid))
            {
                results.Add(new BrokenReference
                {
                    DialogueId = dialogueId,
                    NodeId = nodeId,
                    ActionType = action.Type,
                    ReferencedId = qid,
                });
            }
        }
    }
}
