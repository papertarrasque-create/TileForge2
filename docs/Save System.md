---
updated: 2026-03-06
status: current
---

# Save System

Save/load uses slot-based JSON serialization via `SaveManager`. All game state round-trips through `System.Text.Json`.

## Save File Location

```
~/.tileforge/saves/{slotName}.json
```

`SaveManager` handles directory creation and serialization.

## What Gets Saved

`GameState` (fully serialized):

```
GameState
  Version: int (current: 2)
  Player: PlayerState
  CurrentMapId: string
  ActiveEntities: List<EntityInstance>
  Flags: HashSet<string>
  Variables: Dict<string, string>
  ItemPropertyCache: Dict<string, Dict<string, string>>
```

### PlayerState

```
PlayerState
  X, Y: int                          -- Tile position
  Facing: Direction                   -- Up/Down/Left/Right (default Down)
  Health, MaxHealth: int              -- HP (default 100/100)
  Attack, Defense: int                -- Base stats (default 5/2)
  MaxAP: int                         -- Action points per turn (default 2)
  Poise, MaxPoise: int               -- Shield buffer (default 20/20)
  Inventory: List<string>            -- Item names (group names)
  Equipment: Dict<string, string>    -- Slot -> item name
  ActiveEffects: List<StatusEffect>  -- Active status effects
```

### Flags

`HashSet<string>` -- all set flags. Common patterns:
- `entity_inactive:{id}` -- Entity killed/collected
- `quest_started:{quest_id}` -- Quest activated
- `quest_complete:{quest_id}` -- Quest finished
- `visited_map:{map_id}` -- Map visited
- Custom flags from [[Dialogue]] and [[Quests]]

### Variables

`Dictionary<string, string>` -- all variables. Used by [[Quests]] for counters (e.g., `cave_kills`, `reputation`).

### Item Property Cache

Cached entity properties from collected items. Enables [[Equipment]] stat lookups across map transitions without access to original entities.

## What Is NOT Saved (Ephemeral)

- `PlayState` -- Movement lerp, camera position, floating messages
- AP values (`PlayerAP`, `IsPlayerTurn`) -- Refilled each turn
- Entity AI state (`patrol_origin`, `patrol_dir`) -- Set dynamically on first decision
- Alert turns -- Set dynamically by [[Noise and Alertness]]
- Screen stack state -- Always starts on GameplayScreen
- Sidebar scroll position

## Version Handling

`GameState.Version` enables backward compatibility:

| Version | Added |
|---------|-------|
| 1 | Original format |
| 2 | Equipment dict, MaxAP, Poise/MaxPoise |

`GameStateManager.LoadState()` handles upgrades:
- V1 saves get empty Equipment dict
- Zero MaxAP fixed to default 2
- Zero MaxPoise fixed to default 20

## Entity Persistence

Entities are **stateless in map files**. Runtime state persists via flags:

1. Entity killed -> `DeactivateEntity()` sets `entity_inactive:{id}` flag
2. Map reloaded -> `SwitchMap()` checks flag, sets `IsActive` accordingly
3. Save/load preserves flags -> entity state survives

This means entities always exist in map data but may be invisible/inactive based on flags.

## Save/Load UI

`SaveLoadScreen` provides slot-based save/load:
- Multiple save slots
- Shows slot metadata
- Accessible from PauseScreen overlay

## Related

- [[Equipment]] -- ItemPropertyCache and effective stats
- [[Quests]] -- Flag/variable persistence for quest state
- [[Entities]] -- Entity deactivation and flag-based persistence
- [[File Formats]] -- Save file JSON structure
