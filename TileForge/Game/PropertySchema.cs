using System.Collections.Generic;
using System.Linq;

namespace TileForge.Game;

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
