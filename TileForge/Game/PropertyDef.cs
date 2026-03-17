using System;
using System.Linq;

namespace TileForge.Game;

public record IntRange(int Min, int Max);

public enum PropType
{
    String,
    Int,
    Bool,
    Enum,
    MapRef,
    DialogueRef,
}

public class PropertyDef
{
    public string Key { get; }
    public PropType Type { get; }
    public EntityType[] AppliesTo { get; }

    // Typed constraints (null = unconstrained)
    public IntRange Range { get; }       // For PropType.Int
    public string[] AllowedValues { get; } // For PropType.Enum

    public PropertyDef(string key, PropType type, IntRange range, params EntityType[] appliesTo)
    {
        Key = key; Type = type; Range = range; AllowedValues = null; AppliesTo = appliesTo;
    }

    public PropertyDef(string key, PropType type, string[] allowedValues, params EntityType[] appliesTo)
    {
        Key = key; Type = type; Range = null; AllowedValues = allowedValues; AppliesTo = appliesTo;
    }

    // Overload for no constraint (Bool, String, refs)
    public PropertyDef(string key, PropType type, params EntityType[] appliesTo)
    {
        Key = key; Type = type; Range = null; AllowedValues = null; AppliesTo = appliesTo;
    }

    public bool Validate(string value, out string reason)
    {
        reason = null;
        if (string.IsNullOrEmpty(value)) return true; // Empty = use default

        switch (Type)
        {
            case PropType.Int:
                if (!int.TryParse(value, out var intVal))
                {
                    reason = $"Expected integer, got '{value}'";
                    return false;
                }
                if (Range != null && (intVal < Range.Min || intVal > Range.Max))
                {
                    reason = $"Value {intVal} outside range [{Range.Min}, {Range.Max}]";
                    return false;
                }
                return true;

            case PropType.Bool:
                if (!value.Equals("true", StringComparison.OrdinalIgnoreCase)
                    && !value.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    reason = $"Expected true/false, got '{value}'";
                    return false;
                }
                return true;

            case PropType.Enum:
                if (AllowedValues != null && !AllowedValues.Contains(value))
                {
                    reason = $"Value '{value}' not in [{string.Join(", ", AllowedValues)}]";
                    return false;
                }
                return true;

            default:
                return true; // String, MapRef, DialogueRef -- no structural validation
        }
    }
}
