# Property Schema Registry -- Design Spec

## Problem

Entity properties are `Dictionary<string, string>` everywhere -- data model, serialization, gameplay code. Property keys are scattered as string literals across ~10 files. Type conversion (`int.TryParse`, string comparisons) is duplicated at every read site. Typos in keys silently fail to defaults. No central definition of what properties exist, what types they are, or what constraints they have.

As entity complexity grows, this becomes increasingly fragile.

## Goals

1. **Compile-time key safety** -- Property keys as constants; typos caught by the compiler
2. **Central schema** -- One place defines all known properties, their types, constraints, and which entity types they apply to
3. **Editor-time validation** -- GroupEditor UI driven by the schema (replaces hardcoded presets)
4. **Load-time validation** -- Validate all entity properties on play-mode enter, report issues to HUD log
5. **Warn on unknown** -- Custom properties allowed but flagged; path to strict mode later
6. **No serialization changes** -- `Dictionary<string, string>` stays on disk. No migration needed.

## Non-Goals

- Replacing `Dictionary<string, string>` on data model classes
- Changing JSON format for project files, saves, or exports
- Blocking game start on validation errors (diagnostic only)
- Strongly-typed entity component classes

## Design

### PropertyKeys -- Constant Key Names

Static class with `const string` fields for every known property key. All gameplay code references these instead of string literals.

```csharp
// TileForge/Game/PropertyKeys.cs
public static class PropertyKeys
{
    // Combat
    public const string Health = "health";
    public const string MaxHealth = "max_health";
    public const string Attack = "attack";
    public const string Defense = "defense";
    public const string Poise = "poise";
    public const string Damage = "damage";
    public const string Xp = "xp";

    // AI & Behavior
    public const string Behavior = "behavior";
    public const string Speed = "speed";
    public const string DefaultFacing = "default_facing";
    public const string Hostile = "hostile";
    public const string HostileFlag = "hostile_flag";
    public const string FriendlyFlag = "friendly_flag";
    public const string AggroRange = "aggro_range";
    public const string AlertTurns = "alert_turns";

    // Patrol (runtime-mutated)
    public const string PatrolAxis = "patrol_axis";
    public const string PatrolRange = "patrol_range";
    public const string PatrolOrigin = "patrol_origin";
    public const string PatrolDir = "patrol_dir";

    // Items & Equipment
    public const string Heal = "heal";
    public const string EquipSlot = "equip_slot";
    public const string EquipAttack = "equip_attack";
    public const string EquipDefense = "equip_defense";
    public const string EquipAp = "equip_ap";
    public const string EquipPoise = "equip_poise";
    public const string OnCollectSetFlag = "on_collect_set_flag";
    public const string OnCollectIncrement = "on_collect_increment";

    // Triggers
    public const string TargetMap = "target_map";
    public const string TargetX = "target_x";
    public const string TargetY = "target_y";

    // Dialogue
    public const string DialogueId = "dialogue_id";
    public const string Dialogue = "dialogue";
    public const string OnPickupDialogue = "on_pickup_dialogue";

    // Kill hooks
    public const string OnKillSetFlag = "on_kill_set_flag";
    public const string OnKillIncrement = "on_kill_increment";
}
```

### PropertyDef -- Schema Entry

Defines a single property: its key, value type, constraints, and applicable entity types.

```csharp
// TileForge/Game/PropertyDef.cs
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
```

### PropertySchema -- The Registry

Static class holding all property definitions. Single source of truth.

```csharp
// TileForge/Game/PropertySchema.cs
public static class PropertySchema
{
    public static readonly IReadOnlyList<PropertyDef> All = new PropertyDef[]
    {
        // Combat
        new(PropertyKeys.Health, PropType.Int, new IntRange(1, 9999), EntityType.NPC, EntityType.Trap),
        new(PropertyKeys.MaxHealth, PropType.Int, new IntRange(1, 9999), EntityType.NPC, EntityType.Trap),
        new(PropertyKeys.Attack, PropType.Int, new IntRange(0, 999), EntityType.NPC),
        new(PropertyKeys.Defense, PropType.Int, new IntRange(0, 999), EntityType.NPC),
        new(PropertyKeys.Poise, PropType.Int, new IntRange(0, 9999), EntityType.NPC),
        new(PropertyKeys.Damage, PropType.Int, new IntRange(1, 9999), EntityType.Trap),
        new(PropertyKeys.Xp, PropType.Int, new IntRange(0, 9999), EntityType.NPC, EntityType.Trap),

        // AI
        new(PropertyKeys.Behavior, PropType.Enum,
            new[] { "idle", "chase", "patrol", "chase_patrol" }, EntityType.NPC),
        new(PropertyKeys.Speed, PropType.Int, new IntRange(1, 3), EntityType.NPC),
        new(PropertyKeys.DefaultFacing, PropType.Enum, new[] { "right", "left" }, EntityType.NPC),
        new(PropertyKeys.Hostile, PropType.Bool, EntityType.NPC),
        new(PropertyKeys.HostileFlag, PropType.String, EntityType.NPC),
        new(PropertyKeys.FriendlyFlag, PropType.String, EntityType.NPC),
        new(PropertyKeys.AggroRange, PropType.Int, new IntRange(1, 50), EntityType.NPC),
        new(PropertyKeys.AlertTurns, PropType.Int, new IntRange(1, 99), EntityType.NPC),

        // Patrol (runtime -- not in editor presets, but validated if present)
        new(PropertyKeys.PatrolAxis, PropType.Enum, new[] { "x", "y" }, EntityType.NPC),
        new(PropertyKeys.PatrolRange, PropType.Int, new IntRange(1, 99), EntityType.NPC),
        new(PropertyKeys.PatrolOrigin, PropType.Int, EntityType.NPC),
        new(PropertyKeys.PatrolDir, PropType.Int, EntityType.NPC),

        // Items
        new(PropertyKeys.Heal, PropType.Int, new IntRange(1, 9999), EntityType.Item),
        new(PropertyKeys.EquipSlot, PropType.Enum,
            new[] { "", "weapon", "armor", "accessory" }, EntityType.Item),
        new(PropertyKeys.EquipAttack, PropType.Int, new IntRange(0, 999), EntityType.Item),
        new(PropertyKeys.EquipDefense, PropType.Int, new IntRange(0, 999), EntityType.Item),
        new(PropertyKeys.EquipAp, PropType.Int, new IntRange(0, 5), EntityType.Item),
        new(PropertyKeys.EquipPoise, PropType.Int, new IntRange(0, 999), EntityType.Item),
        new(PropertyKeys.OnCollectSetFlag, PropType.String, EntityType.Item),
        new(PropertyKeys.OnCollectIncrement, PropType.String, EntityType.Item),

        // Triggers
        new(PropertyKeys.TargetMap, PropType.MapRef, EntityType.Trigger),
        new(PropertyKeys.TargetX, PropType.Int, new IntRange(0, 999), EntityType.Trigger),
        new(PropertyKeys.TargetY, PropType.Int, new IntRange(0, 999), EntityType.Trigger),

        // Dialogue (all entity types)
        new(PropertyKeys.DialogueId, PropType.DialogueRef,
            EntityType.NPC, EntityType.Item, EntityType.Trap, EntityType.Trigger, EntityType.Interactable),
        new(PropertyKeys.Dialogue, PropType.DialogueRef,
            EntityType.NPC, EntityType.Item, EntityType.Trap, EntityType.Trigger, EntityType.Interactable),
        new(PropertyKeys.OnPickupDialogue, PropType.DialogueRef, EntityType.Item),

        // Kill hooks
        new(PropertyKeys.OnKillSetFlag, PropType.String, EntityType.NPC, EntityType.Trap),
        new(PropertyKeys.OnKillIncrement, PropType.String, EntityType.NPC, EntityType.Trap),
    };

    // Lookup helpers
    private static readonly Dictionary<string, PropertyDef> _byKey =
        All.ToDictionary(d => d.Key);

    public static PropertyDef Get(string key) =>
        _byKey.TryGetValue(key, out var def) ? def : null;

    public static IEnumerable<PropertyDef> ForEntityType(EntityType type) =>
        All.Where(d => d.AppliesTo.Contains(type));

    public static bool IsKnown(string key) => _byKey.ContainsKey(key);
}
```

### PropertyAccess -- Typed Read Helpers

Replaces the scattered `TryGetValue` + `int.TryParse` pattern with clean one-liners.

```csharp
// TileForge/Game/PropertyAccess.cs
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
```

### PropertyValidator -- Load-Time Diagnostics

Validates all entity properties against the schema. Runs on play-mode enter.

```csharp
// TileForge/Game/PropertyValidator.cs
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
```

**Integration point:** After `GameStateManager.Initialize()` in GameplayScreen, call `PropertyValidator.Validate(gameState.ActiveEntities)` and log results to the HUD message log.

**Out of scope:** The validator does not check `AppliesTo` constraints (e.g., `equip_slot` on an NPC). EntityInstance does not carry EntityType at runtime, so this would require group lookups. This can be added later if needed; the editor UI already prevents most misapplication by only showing relevant presets per entity type.

### GroupEditor Changes

GroupEditor's hardcoded `Presets` dictionary and `NumericSpecs` are replaced by schema queries:

```csharp
// Replace Presets dictionary
var presetKeys = PropertySchema.ForEntityType(entityType)
    .Select(d => d.Key)
    .ToArray();

// Replace NumericSpecs + dropdown logic in CreatePropField
var def = PropertySchema.Get(key);
if (def != null)
{
    return def.Type switch
    {
        PropType.Int => CreateNumericField(key, value, def.Range),
        PropType.Bool => CreateDropdown(key, value, new[] { "true", "false" }),
        PropType.Enum => CreateDropdown(key, value, def.AllowedValues),
        PropType.MapRef => CreateMapDropdown(key, value),
        PropType.DialogueRef => CreateDialogueDropdown(key, value),
        _ => CreateTextField(key, value),
    };
}
// Unknown property -- text field with warning marker
return CreateTextField("* " + key, value);
```

## Migration Strategy

Incremental. No big-bang refactor.

1. **Add new files (no behavior change):** PropertyKeys, PropertyDef, PropertySchema, PropertyAccess, PropertyValidator
2. **Refactor GroupEditor:** Replace presets/NumericSpecs with schema reads
3. **Migrate gameplay code file-by-file:** Replace string literals with PropertyKeys constants and raw TryGetValue+parse with PropertyAccess calls. Each file is independent.
   - EntityAI.cs
   - GameStateManager.cs
   - GameplayScreen.cs
   - InventoryScreen.cs
   - TriggerManager.cs
   - MapCanvas.cs
4. **Wire PropertyValidator** into GameplayScreen initialization
5. **Remove dead code:** `GameStateManager.GetEntityIntProperty` / `SetEntityIntProperty` (subsumed by PropertyAccess)

## Files

### New Files (5)
- `TileForge/Game/PropertyKeys.cs`
- `TileForge/Game/PropertyDef.cs`
- `TileForge/Game/PropertySchema.cs`
- `TileForge/Game/PropertyAccess.cs`
- `TileForge/Game/PropertyValidator.cs`

### Modified Files (~7)
- `TileForge/UI/GroupEditor.cs` -- Replace `Presets`/`NumericSpecs` with schema
- `TileForge/Game/GameStateManager.cs` -- Use PropertyKeys + PropertyAccess, remove old helpers
- `TileForge/Game/Screens/GameplayScreen.cs` -- Use PropertyKeys + PropertyAccess, wire validator
- `TileForge/Game/EntityAI.cs` -- Use PropertyKeys + PropertyAccess
- `TileForge/Game/TriggerManager.cs` -- Use PropertyKeys + PropertyAccess
- `TileForge/UI/MapCanvas.cs` -- Use PropertyKeys for `default_facing` (reads group DefaultProperties, not instance)
- `TileForge/Game/Screens/InventoryScreen.cs` -- Use PropertyKeys for `ItemPropertyCache` lookups

**Note:** The schema includes `max_health` and `xp` which are not in the current GroupEditor `Presets`. After migration, these will appear as editor fields for NPC/Trap entities. This is intentional -- they were previously only set at runtime and are useful to expose in the editor.

### Test Files
- `PropertyAccessTests.cs` -- GetInt, GetBool, GetString edge cases
- `PropertyValidatorTests.cs` -- Valid/invalid/unknown property scenarios
- `PropertySchemaTests.cs` -- Schema completeness, ForEntityType queries, no duplicate keys

## What Doesn't Change

- `Dictionary<string, string>` on Entity, EntityInstance, TileGroup.DefaultProperties, GameState.ItemPropertyCache
- JSON serialization format (project files, saves, exports)
- Custom/unknown properties pass through everywhere
- Runtime behavior -- same defaults, same parse logic, just centralized
