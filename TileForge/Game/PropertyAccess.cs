using System;
using System.Collections.Generic;

namespace TileForge.Game;

public static class PropertyAccess
{
    public static int GetInt(Dictionary<string, string> props, string key, int defaultValue = 0)
    {
        if (props.TryGetValue(key, out var val) && int.TryParse(val, out var result))
            return result;
        return defaultValue;
    }

    // Note: Returns true for any non-"false" value when the key exists.
    // This matches existing IsEntityHostile semantics ("hostile" defaults to true).
    // PropertyValidator catches non-boolean values at load time.
    public static bool GetBool(Dictionary<string, string> props, string key, bool defaultValue = false)
    {
        if (props.TryGetValue(key, out var val))
            return !string.Equals(val, "false", StringComparison.OrdinalIgnoreCase);
        return defaultValue;
    }

    public static string GetString(Dictionary<string, string> props, string key, string defaultValue = "")
    {
        return props.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val) ? val : defaultValue;
    }

    public static void SetInt(Dictionary<string, string> props, string key, int value)
    {
        props[key] = value.ToString();
    }
}
