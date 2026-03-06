---
updated: 2026-03-06
status: current
---

# Group Editor

The GroupEditor is a modal property editor for TileGroup definitions. It handles both tiles and entities, with type-aware controls and entity presets.

## Layout

```
+-- Header Row 1 ------------------------------------------+
| Name: [________]  Type: [Tile/Entity]  [x] Solid  [x] Player |
+-- Tile Row 2 (tiles only) --------------------------------+
| [x] Pass  [x] Hazard  Cost:[___]  Dmg:[____]  Hit:[___]  |
+-- Tile Row 3 (tiles only) --------------------------------+
| Def:[___]  Noise:[______]                                  |
+-- Entity Row 2 (entities only) ---------------------------+
| Type: [NPC/Item/Trap/Trigger/Interactable]                 |
+-- Entity Properties (dynamic) ----------------------------+
| health:    [___]                                           |
| attack:    [___]                                           |
| behavior:  [idle/chase/patrol/chase_patrol]                |
| dialogue:  [________] (browse with "Create New...")        |
| ...                                                        |
+-- Sprite Sheet ------------------------------------------+
| [sprite grid with zoom/pan, click/shift/ctrl to select]    |
+-----------------------------------------------------------+
```

## Controls by Property Type

| Control | Used For |
|---------|----------|
| **TextInputField** | Name (32 chars), text properties (512 chars) |
| **Dropdown** | Type, EntityType, behavior, equip_slot, DamageType, hostile, facing |
| **NumericField** | health, attack, defense, speed, aggro_range, poise, etc. |
| **Checkbox** | Solid, Player, Passable, Hazard |
| **BrowseDropdown** | target_map, dialogue_id (with "+ Create New..." linkage via [[Editor Overview\|IProjectContext]]) |

## Tile Properties

| Property | Control | Values |
|----------|---------|--------|
| Solid | Checkbox | -- |
| Passable | Checkbox | -- |
| Hazard | Checkbox | -- |
| MovementCost | Dropdown | 0.5 / 1.0 / 1.5 / 2.0 / 3.0 / 5.0 |
| DamageType | Dropdown | fire / poison / spikes / ice |
| DamagePerTick | NumericField | 0-50 |
| DefenseBonus | Dropdown | 0 / 1 / 2 / 3 / 5 |
| NoiseLevel | Dropdown | Silent(0) / Normal(1) / Loud(2) |

## Entity Type Presets

When the EntityType dropdown changes, `RebuildPropertyFields()` populates type-specific default properties:

### NPC

| Property | Control | Notes |
|----------|---------|-------|
| dialogue_id | BrowseDropdown | Links to [[Dialogue]] files |
| health | NumericField | |
| attack | NumericField | |
| defense | NumericField | |
| poise | NumericField | |
| behavior | Dropdown | idle/chase/patrol/chase_patrol |
| speed | NumericField | 1-3 |
| default_facing | Dropdown | Up/Down/Left/Right |
| hostile | Dropdown | true/false |
| hostile_flag | TextInputField | Flag that makes entity hostile |
| friendly_flag | TextInputField | Flag that makes entity friendly |
| aggro_range | NumericField | |
| on_kill_set_flag | TextInputField | [[Quests\|Quest hook]] |
| on_kill_increment | TextInputField | [[Quests\|Quest hook]] |

### Item

| Property | Control | Notes |
|----------|---------|-------|
| heal | NumericField | HP restored on use |
| equip_slot | Dropdown | Weapon/Armor/Accessory (see [[Equipment]]) |
| equip_attack | NumericField | Attack bonus when equipped |
| equip_defense | NumericField | Defense bonus |
| equip_ap | NumericField | AP bonus |
| equip_poise | NumericField | Poise bonus |
| on_collect_set_flag | TextInputField | [[Quests\|Quest hook]] |
| on_collect_increment | TextInputField | [[Quests\|Quest hook]] |

### Trap

| Property | Control | Notes |
|----------|---------|-------|
| damage | NumericField | Damage dealt on step |
| health | NumericField | Trap HP (can be destroyed) |
| on_kill_set_flag | TextInputField | |
| on_kill_increment | TextInputField | |

### Trigger

| Property | Control | Notes |
|----------|---------|-------|
| target_map | BrowseDropdown | Destination [[Maps\|map]] |
| target_x | NumericField | Spawn X |
| target_y | NumericField | Spawn Y |

### Interactable

| Property | Control | Notes |
|----------|---------|-------|
| dialogue_id | BrowseDropdown | Links to [[Dialogue]] |

## Sprite Selection

Bottom portion shows the spritesheet grid:
- Click to select a sprite
- Shift+click for multi-select (range)
- Ctrl+click for multi-select (toggle)
- Middle-mouse drag to pan
- Scroll to zoom
- Selected sprites become the group's visual representation

## Factory Methods

- `ForNewGroup(context, preSelectedSprites)` -- Create new group with optional pre-selected sprites
- `ForExistingGroup(group, context)` -- Load existing group for editing

## Confirmation

`TryConfirm(state)` validates and returns the completed TileGroup. Name is required. Properties with empty values are stripped.

## Layout Constants

| Constant | Value |
|----------|-------|
| PropRowH | 28px |
| PropFieldH | 22px |
| PropLabelWMin | 80px |
| DropW | 160px |
| NumW | 80px |
| Label width | Dynamic (computed from font, clamped to half panel) |

## Related

- [[Property Reference]] -- Complete property key reference
- [[Editor Overview]] -- Where GroupEditor fits in the update chain
- [[DojoUI]] -- Widget controls used
- [[Entities]] -- Runtime behavior of authored properties
