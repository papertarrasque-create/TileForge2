using System.Collections.Generic;
using TileForge.Game;

namespace TileForge.UI;

public class ActionListEditor
{
    public static readonly string[] ActionTypes = new[]
    {
        "set_flag", "set_variable", "increment",
        "give_item", "remove_item",
        "start_quest", "complete_objective",
        "heal", "damage", "log"
    };

    private readonly List<ActionEntry> _entries = new();
    public int Count => _entries.Count;

    public void LoadFromActions(List<DialogueAction> actions)
    {
        _entries.Clear();
        if (actions == null) return;
        foreach (var a in actions)
        {
            _entries.Add(new ActionEntry
            {
                Type = a.Type ?? "set_flag",
                Value = a.Value ?? "",
                Key = a.Key ?? "",
                Text = a.Text ?? "",
                Color = a.Color ?? "",
            });
        }
    }

    public List<DialogueAction> ToActions()
    {
        var result = new List<DialogueAction>();
        foreach (var entry in _entries)
        {
            var a = new DialogueAction { Type = entry.Type };
            switch (entry.Type)
            {
                case "set_flag":
                case "increment":
                case "give_item":
                case "remove_item":
                case "start_quest":
                case "complete_objective":
                case "heal":
                case "damage":
                    a.Value = entry.Value;
                    break;
                case "set_variable":
                    a.Key = entry.Key;
                    a.Value = entry.Value;
                    break;
                case "log":
                    a.Text = entry.Text;
                    a.Color = entry.Color;
                    break;
            }
            result.Add(a);
        }
        return result;
    }

    public void AddAction()
    {
        _entries.Add(new ActionEntry { Type = "set_flag" });
    }

    public void RemoveActionAt(int index)
    {
        if (index >= 0 && index < _entries.Count)
            _entries.RemoveAt(index);
    }

    public ActionEntry GetEntry(int index) => _entries[index];

    public class ActionEntry
    {
        public string Type { get; set; } = "set_flag";
        public string Value { get; set; } = "";
        public string Key { get; set; } = "";
        public string Text { get; set; } = "";
        public string Color { get; set; } = "";
    }
}
