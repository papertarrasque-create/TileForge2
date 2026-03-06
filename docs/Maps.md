---
updated: 2026-03-06
status: current
---

# Map System

Maps are tile grids with layers, entities, and transition mechanisms. In multimap projects, maps share groups and spritesheets.

## Map Structure

```
LoadedMap (runtime)
  Id: string
  Width, Height: int
  Layers: List<LoadedMapLayer>    -- Tile data per layer
  Groups: List<TileGroup>         -- Tile/entity definitions
  Entities: List<EntityInstance>  -- Placed entities
```

Each layer has a `Cells` array indexed as `x + y * width`, containing group names (or null for empty cells).

## Data Flow

```
Editor (TileGroup, MapData) -> MapExporter -> JSON -> MapLoader -> LoadedMap -> GameStateManager
```

At play mode entry, `PlayModeController.Enter()` pre-exports **all** project maps to an in-memory dictionary. Map transitions load from this dictionary -- no filesystem I/O during gameplay.

## Multimap Projects

- `ProjectFile` V2 stores multiple maps (V1 auto-upgrades on load)
- `MapDocumentState` holds per-map state (map data, undo stack, camera, selection)
- `EditorState` is a facade that delegates to the active document
- [[Map Tab Bar]] provides tab-based map switching with CRUD operations
- Groups and spritesheets are shared across all maps in a project

## Transition Types

### 1. Trigger Entity Transitions

Portal-style transitions using [[Entities]] with trigger properties:

| Property | Purpose |
|----------|---------|
| `target_map` | Destination map name |
| `target_x` | Spawn X coordinate |
| `target_y` | Spawn Y coordinate |

Player steps on trigger entity -> creates `MapTransitionRequest` -> `PlayModeController` executes transition.

In-project maps are resolved by name from the pre-exported dictionary. External maps fall back to filesystem loading.

### 2. Edge-of-Map Transitions

When the player walks off a map boundary:

1. `EdgeTransitionResolver` checks `WorldLayout` for a neighbor in that direction
2. If neighbor exists, creates `MapTransitionRequest`
3. Default spawn: opposite edge, parallel coordinate clamped to target bounds

**Default spawn logic:**
| Exit Direction | Spawn Position |
|---------------|----------------|
| East (right) | x=0, y clamped |
| West (left) | x=width-1, y clamped |
| South (down) | y=0, x clamped |
| North (up) | y=height-1, x clamped |

Custom `EdgeSpawn` overrides can specify exact spawn coordinates per direction.

### 3. Exit Point Transitions

Portal-style transitions at arbitrary map positions (not just edges):

- `MapPlacement` defines exit tiles: `NorthExit`, `SouthExit`, `EastExit`, `WestExit`
- Each exit has X/Y coordinates on the current map
- When player steps on an exit point tile, `ResolveExitPoint()` triggers
- Creates transition to the neighbor in that direction
- Uses neighbor's entry spawn (or default edge spawn)

Exit points coexist with edge-of-map transitions -- a map can have both.

## World Layout

The `WorldLayout` defines spatial relationships between maps via a 2D grid:

```
WorldLayout
  Maps: Dict<string, MapPlacement>

MapPlacement
  GridX, GridY: int            -- Position on world grid
  NorthEntry...WestEntry: EdgeSpawn?  -- Custom spawn when entering from direction
  NorthExit...WestExit: EdgeSpawn?    -- Portal tiles on this map
```

**Grid adjacency:** Maps at adjacent grid positions are neighbors:
- (0,0) is west of (1,0)
- (0,0) is north of (0,1)
- Adjacency is auto-bidirectional -- placing creates mutual neighbors

Configured in the [[World Map Editor]]. Stored in `ProjectFile.ProjectData.WorldLayout` (null when unconfigured for backward compatibility).

## Map Transition Execution

`PlayModeController` processes transitions:

1. Save current entity states (flags, variables preserved)
2. Load target map from pre-exported dictionary
3. Apply entity activity flags (`entity_inactive:{id}`)
4. Reposition player at spawn coordinates
5. Propagate noise at new position (see [[Noise and Alertness]])

## Tile Properties

Tiles (via TileGroup) have gameplay properties:

| Property | Default | Effect |
|----------|---------|--------|
| `IsSolid` | false | Blocks movement |
| `IsPassable` | true | Allows movement (false = blocks) |
| `IsHazardous` | false | Applies damage on step |
| `MovementCost` | 1.0 | Movement speed multiplier |
| `DamageType` | -- | "fire", "poison", "ice", "spikes" |
| `DamagePerTick` | 0 | Damage per step on hazard |
| `DefenseBonus` | 0 | Terrain defense for [[Combat]] |
| `NoiseLevel` | 1 | Noise level for [[Noise and Alertness]] |

Set in the [[Group Editor]].

## Layers

Maps have multiple layers (typically "Ground" and "Objects"):
- Rendered in order (painter's algorithm)
- `EntityRenderOrder` controls which layer boundary entities render at
- Visibility toggleable in editor
- Active layer determines which layer brush/fill/eraser affects

## Related

- [[World Map Editor]] -- Visual grid editor for map adjacency
- [[Map Tab Bar]] -- Multimap tab management
- [[Entities]] -- Entity placement and interaction
- [[File Formats]] -- Project file and map data specs
- [[Save System]] -- How map state persists across sessions
