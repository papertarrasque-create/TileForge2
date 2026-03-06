---
updated: 2026-03-06
status: current
---

# Entity System

Entities are instances of [[Group Editor|TileGroups]] placed on the map. They use **property bags** (`Dictionary<string, string>`) rather than class hierarchies -- see [[Property Reference]] for all known keys.

## Entity Types

`EntityType` enum determines interaction behavior:

| Type | Behavior |
|------|----------|
| **NPC** | Can show dialogue, can be attacked if hostile |
| **Item** | Collectible -- appears in inventory, caches properties for [[Equipment]] |
| **Trap** | Triggers damage/effects on step, can be attacked |
| **Trigger** | Causes [[Maps|map transitions]] (not attackable) |
| **Interactable** | Generic -- shows dialogue if configured, otherwise interaction message |

Types are set in the [[Group Editor]] via the EntityType dropdown.

## EntityInstance Structure

```
EntityInstance
  Id: string              -- Unique identifier (UUID-style)
  DefinitionName: string  -- References TileGroup by name
  X, Y: int               -- Tile position
  Properties: Dict         -- Property bag (string -> string)
  IsActive: bool           -- false = deactivated (dead/collected)
```

Properties are inherited from `TileGroup.DefaultProperties` at placement time, then can be overridden per-instance.

## AI Behaviors

Entity behavior is driven by the `behavior` property. All AI logic lives in `EntityAI.DecideAction()` -- a **pure, static function** (state in, action out). The only side effect is writing patrol tracking properties.

### idle

Does nothing. Entity stays in place.

### chase

Follows the player when within aggro range:

```
aggro_range: default 5 (customizable)
If alerted: aggro_range * 2

If distance > aggroRange: Idle
If distance == 1: MeleeAttack
Else: Move toward player via IPathfinder
```

Distance is Manhattan (`|dx| + |dy|`).

### patrol

Walks back and forth along a configured axis:

| Property | Default | Purpose |
|----------|---------|---------|
| `patrol_axis` | "x" | Axis of movement ("x" or "y") |
| `patrol_range` | 3 | Distance from origin |
| `patrol_origin` | (set once) | Starting position on axis |
| `patrol_dir` | 1 | Current direction (+1 or -1) |

Logic: Calculate next position along axis + direction. If out of range or blocked (solid tile, entity), reverse direction. If both directions blocked, idle.

### chase_patrol

Hybrid -- patrols by default, switches to chase when player enters aggro range:

```
if distance <= aggroRange:
    return chase behavior
else:
    return patrol behavior
```

Uses the alert-aware aggro range from [[Noise and Alertness]].

### Friendly vs Hostile

Entity hostility is resolved dynamically:

| Property | Effect |
|----------|--------|
| `hostile` | "false" makes entity non-hostile (default: hostile) |
| `hostile_flag` | If this flag is set in GameState, entity becomes hostile |
| `friendly_flag` | If this flag is set in GameState, entity becomes non-hostile |

For non-hostile entities, `chase` and `chase_patrol` downgrade to patrol-only behavior.

## Pathfinding

`IPathfinder` interface with current implementation `SimplePathfinder`:

**Axis-Priority movement:**
- Primary axis = axis with greater absolute distance (ties go to X)
- Try primary first; if blocked, try secondary
- Returns next step toward target, or null if both blocked

**Walkability checks:**
1. Out of bounds -> not walkable
2. Solid tile -> not walkable
3. Player position -> not walkable (entities can't overlap player)
4. Active entity at position (excluding self) -> not walkable

**Line of Sight** (`HasLineOfSight`):
- Bresenham's line algorithm
- Checks intermediate tiles only (not start/end)
- Returns true if all tiles are passable
- Used for ranged combat extension point

**Extension point:** Build `AStarPathfinder` implementing `IPathfinder` -- one-line swap in GameplayScreen constructor.

## Entity Deactivation

`GameStateManager.DeactivateEntity(entity)`:

1. Sets `entity.IsActive = false`
2. Sets flag `entity_inactive:{id}` for cross-map persistence
3. Triggers [[Quests|quest hooks]] (`on_kill_set_flag`, `on_kill_increment`)

Entity remains in GameState but won't render or interact. On map reload, `SwitchMap()` checks the flag and sets `IsActive` accordingly.

## Collision

Entities block movement when:
- `IsActive == true`
- Target tile matches entity position
- Entity's TileGroup has `IsSolid == true`

Player also blocks entity movement (checked in pathfinder).

## Entity Interaction

`CheckEntityInteractionAt()` in GameplayScreen:
- **Bump attack:** If entity is attackable and hostile, `TryBumpAttack` runs first (costs 1 AP)
- **Dialogue:** NPC and Interactable types check for `dialogue` or `dialogue_id` property -> opens [[Dialogue]] screen (0 AP)
- **Item collection:** Item entities trigger `CollectItem()` -> inventory + property cache
- **Trigger:** Trigger entities create [[Maps|MapTransitionRequest]]

Bump attack is separated from interaction: attackable NPCs get attacked, friendly NPCs show dialogue.

## Related

- [[Property Reference]] -- Complete property key reference
- [[Combat]] -- How entity combat works
- [[Noise and Alertness]] -- Alert system that modifies aggro range
- [[Group Editor]] -- Where entity properties are authored
