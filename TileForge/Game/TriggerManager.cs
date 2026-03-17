using System.Collections.Generic;

namespace TileForge.Game;

public class TriggerManager
{
    public TriggerResult Fire(TriggerEvent evt, GameStateManager gsm,
                               Dictionary<string, DialogueData> dialogues)
    {
        if (evt?.Properties == null) return null;

        DialogueData dialogue = null;

        if (evt.Properties.TryGetValue(PropertyKeys.DialogueId, out var dialogueId) && !string.IsNullOrEmpty(dialogueId))
        {
            if (!dialogues.TryGetValue(dialogueId, out dialogue))
                return null;
        }
        else if (evt.Properties.TryGetValue(PropertyKeys.Dialogue, out var inlineText) && !string.IsNullOrEmpty(inlineText))
        {
            dialogue = CreateInlineDialogue(evt.EntityId ?? "unknown", inlineText);
        }

        if (dialogue == null) return null;

        if (dialogue.OneShot == true && gsm.HasFlag($"dialogue_shown:{dialogue.Id}"))
            return null;

        string startNodeId = ResolveStartNode(dialogue, gsm);
        if (startNodeId == null) return null;

        return new TriggerResult { Dialogue = dialogue, StartNodeId = startNodeId };
    }

    private static string ResolveStartNode(DialogueData dialogue, GameStateManager gsm)
    {
        if (dialogue.Routes != null && dialogue.Routes.Count > 0)
        {
            foreach (var route in dialogue.Routes)
            {
                if (ConditionEvaluator.EvaluateAll(route.Conditions, gsm))
                    return route.StartNode;
            }
            return null;
        }

        if (dialogue.Nodes != null && dialogue.Nodes.Count > 0)
            return dialogue.Nodes[0].Id;

        return null;
    }

    internal static DialogueData CreateInlineDialogue(string entityName, string text)
    {
        var pages = text.Split('|');
        var nodes = new List<DialogueNode>();
        for (int i = 0; i < pages.Length; i++)
        {
            var node = new DialogueNode
            {
                Id = $"inline_{i}",
                Speaker = entityName,
                Text = pages[i].Trim(),
            };
            if (i < pages.Length - 1) node.NextNodeId = $"inline_{i + 1}";
            nodes.Add(node);
        }
        return new DialogueData
        {
            Id = $"inline_{entityName}",
            Nodes = nodes,
            Routes = new() { new DialogueRoute { StartNode = "inline_0" } },
        };
    }
}
