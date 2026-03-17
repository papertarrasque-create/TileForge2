namespace TileForge.Game;

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
    public const string Weight = "weight";

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
    public const string EquipWeight = "equip_weight";
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
