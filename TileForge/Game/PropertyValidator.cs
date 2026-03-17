using System.Collections.Generic;

namespace TileForge.Game;

public enum PropertyErrorLevel { Warning, Error }

public record PropertyError(
    PropertyErrorLevel Level,
    string EntityId,
    string Key,
    string Message);

public static class PropertyValidator
{
    public static List<PropertyError> Validate(IReadOnlyList<EntityInstance> entities)
    {
        var errors = new List<PropertyError>();

        foreach (var entity in entities)
        {
            foreach (var kvp in entity.Properties)
            {
                var def = PropertySchema.Get(kvp.Key);
                if (def == null)
                {
                    errors.Add(new PropertyError(
                        PropertyErrorLevel.Warning,
                        entity.Id,
                        kvp.Key,
                        "Unknown property"));
                    continue;
                }

                if (!def.Validate(kvp.Value, out var reason))
                {
                    errors.Add(new PropertyError(
                        PropertyErrorLevel.Error,
                        entity.Id,
                        kvp.Key,
                        reason));
                }
            }
        }

        return errors;
    }
}
