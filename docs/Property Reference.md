---
updated: 2026-03-06
status: current
---

# Property Reference

Complete reference for all entity property keys used in TileForge. Properties are stored as `Dictionary<string, string>` -- all values are strings, parsed to int/bool as needed.

## Combat & Health

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `health` | int | -- | Current HP; 0 = dead |
| `max_health` | int | =health | Max HP (for display) |
| `attack` | int | -- | Base attack stat |
| `defense` | int | -- | Base defense stat |
| `poise` | int | -- | Shield buffer (absorbed before health) |
| `speed` | int | 1 | Actions per turn, clamped [1, 3] |
| `xp` | int | -- | Experience awarded on kill |

See [[Combat]] for damage formula and [[Equipment]] for effective stats.

## Hostility

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `hostile` | string | "true" | "false" makes entity non-hostile |
| `hostile_flag` | string | -- | Flag that makes entity hostile when set |
| `friendly_flag` | string | -- | Flag that makes entity non-hostile when set |

Hostility is resolved dynamically each frame. `hostile_flag` and `friendly_flag` override the base `hostile` value.

## AI & Behavior

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `behavior` | string | -- | idle / chase / patrol / chase_patrol |
| `aggro_range` | int | 5 | Chase activation distance (Manhattan) |
| `default_facing` | string | "Down" | Initial facing direction |
| `alert_turns` | int | 0 | Runtime: turns remaining at doubled aggro |

See [[Entities]] for behavior details and [[Noise and Alertness]] for the alert system.

## Patrol

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `patrol_axis` | string | "x" | Axis of patrol ("x" or "y") |
| `patrol_range` | int | 3 | Distance from origin |
| `patrol_origin` | int | (set once) | Starting position on patrol axis |
| `patrol_dir` | int | 1 | Current direction (+1 or -1) |

`patrol_origin` and `patrol_dir` are set automatically on first AI decision. They are runtime-only and not persisted in save files.

## Interaction & Dialogue

| Key | Type | Description |
|-----|------|-------------|
| `dialogue_id` | string | References `dialogues/{id}.json` file |
| `dialogue` | string | Inline dialogue text (fallback if dialogue_id missing) |
| `concluded_flag` | string | Flag name; when set, NPC shows concluded_dialogue instead |
| `concluded_dialogue` | string | Dialogue ID or inline text shown after main dialogue concludes |
| `on_pickup_dialogue` | string | Dialogue ID or inline text shown on first pickup of this item group |

See [[Dialogue]] for the dialogue system.

## Map Transitions

| Key | Type | Description |
|-----|------|-------------|
| `target_map` | string | Destination map name |
| `target_x` | int | Spawn X coordinate on destination |
| `target_y` | int | Spawn Y coordinate on destination |

Used by Trigger entities. See [[Maps]] for transition types.

## Equipment (on Item entities)

| Key | Type | Description |
|-----|------|-------------|
| `equip_slot` | string | "Weapon", "Armor", or "Accessory" |
| `equip_attack` | int | Attack bonus when equipped |
| `equip_defense` | int | Defense bonus when equipped |
| `equip_ap` | int | MaxAP bonus when equipped |
| `equip_poise` | int | MaxPoise bonus when equipped |

See [[Equipment]] for the equipment system.

## Item Properties

| Key | Type | Description |
|-----|------|-------------|
| `heal` | int | HP restored when item is used |

## Trap Properties

| Key | Type | Description |
|-----|------|-------------|
| `damage` | int | Damage dealt when player steps on trap |

## Quest Hooks

| Key | Type | Trigger | Effect |
|-----|------|---------|--------|
| `on_kill_set_flag` | string | Entity killed | Sets the named flag |
| `on_kill_increment` | string | Entity killed | Increments the named variable by 1 |
| `on_collect_set_flag` | string | Item collected | Sets the named flag |
| `on_collect_increment` | string | Item collected | Increments the named variable by 1 |

See [[Quests]] for how hooks feed quest objectives.

## Tile Group Properties

These are on `TileGroup` directly (not in the property bag):

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IsSolid` | bool | false | Blocks movement |
| `IsPassable` | bool | true | Allows movement |
| `IsHazardous` | bool | false | Applies damage on step |
| `MovementCost` | float | 1.0 | Movement speed multiplier |
| `DamageType` | string | -- | fire / poison / ice / spikes |
| `DamagePerTick` | int | 0 | Hazard damage per step |
| `DefenseBonus` | int | 0 | Terrain defense for [[Combat]] |
| `NoiseLevel` | int | 1 | 0=silent, 1=normal, 2=loud |
| `EntityType` | enum | -- | NPC / Item / Trap / Trigger / Interactable |

## Flag Naming Conventions

Common flag patterns used across the codebase:

| Pattern | Example | Source |
|---------|---------|--------|
| `entity_inactive:{id}` | `entity_inactive:goblin_01` | Entity deactivation |
| `quest_started:{quest_id}` | `quest_started:rescue` | Quest StartFlag |
| `quest_complete:{quest_id}` | `quest_complete:rescue` | Quest CompletionFlag |
| `visited_map:{map_id}` | `visited_map:cave` | Map visit tracking |
| `pickup_dialogue_shown:{name}` | `pickup_dialogue_shown:Sword` | First-pickup dialogue tracking |
| Custom | `has_sword`, `elder_spoke` | Dialogue/quest authored |

## Related

- [[Group Editor]] -- Where properties are authored
- [[Entities]] -- Runtime property interpretation
- [[Combat]] -- How combat stats are used
- [[Equipment]] -- Equipment bonus resolution
