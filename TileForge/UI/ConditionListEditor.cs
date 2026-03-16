using System.Collections.Generic;
using TileForge.Game;

namespace TileForge.UI;

public class ConditionListEditor
{
    public static readonly string[] ConditionTypes = new[]
    {
        "has_flag", "not_flag", "has_item",
        "variable_eq", "variable_gte", "variable_lt",
        "quest_active", "quest_complete"
    };

    private readonly List<ConditionEntry> _entries = new();
    public int Count => _entries.Count;

    public void LoadFromConditions(List<Condition> conditions)
    {
        _entries.Clear();
        if (conditions == null) return;
        foreach (var c in conditions)
        {
            _entries.Add(new ConditionEntry
            {
                Type = c.Type ?? "has_flag",
                PrimaryValue = GetPrimaryValue(c),
                SecondaryValue = GetSecondaryValue(c),
            });
        }
    }

    public List<Condition> ToConditions()
    {
        var result = new List<Condition>();
        foreach (var entry in _entries)
        {
            var c = new Condition { Type = entry.Type };
            switch (entry.Type)
            {
                case "has_flag":
                case "not_flag":
                    c.Flag = entry.PrimaryValue;
                    break;
                case "has_item":
                    c.Item = entry.PrimaryValue;
                    break;
                case "variable_eq":
                case "variable_gte":
                case "variable_lt":
                    c.Variable = entry.PrimaryValue;
                    c.Value = entry.SecondaryValue;
                    break;
                case "quest_active":
                case "quest_complete":
                    c.Value = entry.PrimaryValue;
                    break;
            }
            result.Add(c);
        }
        return result;
    }

    public void AddCondition()
    {
        _entries.Add(new ConditionEntry { Type = "has_flag", PrimaryValue = "", SecondaryValue = "" });
    }

    public void RemoveConditionAt(int index)
    {
        if (index >= 0 && index < _entries.Count)
            _entries.RemoveAt(index);
    }

    public ConditionEntry GetEntry(int index) => _entries[index];

    private static string GetPrimaryValue(Condition c) => c.Type switch
    {
        "has_flag" or "not_flag" => c.Flag ?? "",
        "has_item" => c.Item ?? "",
        "variable_eq" or "variable_gte" or "variable_lt" => c.Variable ?? "",
        "quest_active" or "quest_complete" => c.Value ?? "",
        _ => ""
    };

    private static string GetSecondaryValue(Condition c) => c.Type switch
    {
        "variable_eq" or "variable_gte" or "variable_lt" => c.Value ?? "",
        _ => ""
    };

    public class ConditionEntry
    {
        public string Type { get; set; } = "has_flag";
        public string PrimaryValue { get; set; } = "";
        public string SecondaryValue { get; set; } = "";
    }
}
