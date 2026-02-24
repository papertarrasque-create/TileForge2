# RPG Game State & Data Architecture — To-Do List

**Stack:** Vanilla C# + MonoGame  
**Context:** Level editor is largely complete with tile painting, layers, collisions, entity placement, and save/load. Player movement and collision are working. This document covers Phase 2: establishing the game state and data architecture that everything else will build on.

---

## 1. Define the Core Data Model

The goal is a clean separation between **editor data** (what you author) and **runtime data** (what the game uses at play time). They can share structures, but the game should never depend on editor-only concepts.

### 1a. Tile Data

Your editor already has tile groups and tile names. Now formalize what a tile *means* at runtime.

```csharp
public class TileDefinition
{
    public string Id { get; set; }          // unique key, e.g. "grass_01"
    public string Group { get; set; }       // from your editor groups
    public Rectangle SourceRect { get; set; } // sprite sheet region
    public bool IsPassable { get; set; }
    public bool IsHazardous { get; set; }
    public float MovementCost { get; set; }  // 1.0 = normal, 2.0 = slow (swamp), etc.
    public string? DamageType { get; set; }  // null, "fire", "poison", etc.
    public int DamagePerTick { get; set; }
}
```

**To-do:**
- [ ] Create a `TileDefinition` class (or struct) that holds gameplay-relevant properties
- [ ] Build a `TileRegistry` (a `Dictionary<string, TileDefinition>`) that loads from a JSON or XML data file
- [ ] Ensure the editor writes tile IDs into the map, and the game resolves them through the registry at load time
- [ ] Decide: does the editor export the full registry, or does the game maintain its own copy?

### 1b. Entity Data

Your editor places entities. Now define what an entity *is* at runtime beyond its position.

```csharp
public enum EntityType
{
    NPC,
    Item,
    Trap,
    Trigger,       // doors, zone transitions, event triggers
    Interactable   // chests, signs, levers
}

public class EntityDefinition
{
    public string Id { get; set; }
    public EntityType Type { get; set; }
    public string SpriteId { get; set; }
    public Dictionary<string, string> Properties { get; set; } // flexible key-value pairs
}
```

The `Properties` dictionary is the extensibility lever. An NPC might have `"dialogue_id": "elder_01"`. A trap might have `"damage": "10"`, `"trigger_type": "proximity"`. This avoids creating a unique class for every entity variant early on.

**To-do:**
- [ ] Create `EntityDefinition` with a type enum and a flexible property bag
- [ ] Create an `EntityInstance` class for placed entities (definition reference + position + runtime state)
- [ ] Decide on serialization format (JSON recommended — human-readable, easy to hand-edit)
- [ ] Build an `EntityRegistry` similar to the tile registry

### 1c. Map Data (Runtime Format)

```csharp
public class MapData
{
    public string Id { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int TileSize { get; set; }
    public List<MapLayer> Layers { get; set; }
    public List<EntityInstance> Entities { get; set; }
    public Dictionary<string, string> Metadata { get; set; } // map-level properties
}

public class MapLayer
{
    public string Name { get; set; }       // "ground", "walls", "decoration"
    public string[] TileIds { get; set; }  // flattened 2D array (row-major)
    public bool HasCollision { get; set; }
}
```

**To-do:**
- [ ] Define a `MapData` class that the game loads at runtime
- [ ] Write a loader that reads your editor's save format into `MapData`
- [ ] If your editor save format is messy or tightly coupled, write an export step that produces a clean game-ready file
- [ ] Support map-level metadata (ambient lighting, music track, region name, etc.)

---

## 2. Build the Game State Manager

This is the central nervous system — it tracks what's happening right now and what has changed.

```csharp
public class GameState
{
    public PlayerState Player { get; set; }
    public string CurrentMapId { get; set; }
    public MapData CurrentMap { get; set; }
    public List<EntityInstance> ActiveEntities { get; set; }
    public HashSet<string> Flags { get; set; }  // global event flags
    public Dictionary<string, object> Variables { get; set; } // quest progress, counters, etc.
}

public class PlayerState
{
    public Vector2 Position { get; set; }
    public Direction Facing { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public List<string> Inventory { get; set; }
}
```

**To-do:**
- [ ] Create a `GameState` class that holds all mutable game state in one place
- [ ] Create a `PlayerState` class with position, health, inventory, and facing direction
- [ ] Implement a global flags system (`HashSet<string>`) for tracking events (e.g., `"spoke_to_elder"`, `"chest_04_opened"`)
- [ ] Build a `GameStateManager` that owns `GameState` and provides methods to query/modify it
- [ ] Keep `GameState` serializable from the start — this is your save file

---

## 3. Map Loading and Transitions

**To-do:**
- [ ] Build a `MapLoader` that reads map files, resolves tile/entity IDs through registries, and produces a ready-to-render `MapData`
- [ ] Implement map transitions via trigger entities (e.g., a door entity with properties `"target_map": "dungeon_01"`, `"target_x": "5"`, `"target_y": "12"`)
- [ ] Handle entity state persistence: when you leave a map and come back, are opened chests still open? (Use your flags system for this)
- [ ] Consider a `MapCache` if you want to keep recently visited maps in memory

---

## 4. Serialization / Save-Load

**To-do:**
- [ ] Choose a serializer: `System.Text.Json` is built-in and good enough; avoid heavy dependencies
- [ ] Serialize `GameState` to JSON for save files
- [ ] Keep data definitions (tile registry, entity registry) as separate JSON files that ship with the game
- [ ] Write a `SaveManager` with `Save(string slotName)` and `Load(string slotName)` methods
- [ ] Store saves in a sensible location (e.g., `Environment.GetFolderPath(SpecialFolder.ApplicationData)`)
- [ ] Version your save format from day one — add a `"version": 1` field so you can migrate later

---

## 5. Scene / Screen Management

You need a way to switch between game states (title screen, gameplay, pause, inventory, dialogue).

```csharp
public abstract class GameScreen
{
    public abstract void Update(GameTime gameTime);
    public abstract void Draw(SpriteBatch spriteBatch);
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
}
```

**To-do:**
- [ ] Create a `GameScreen` base class with `Update`, `Draw`, `OnEnter`, `OnExit`
- [ ] Build a `ScreenManager` that maintains a stack of screens (so pause overlays gameplay, dialogue overlays the world)
- [ ] Implement at minimum: `GameplayScreen`, `PauseScreen`, `TitleScreen`
- [ ] Wire the screen manager into your MonoGame `Game.Update()` and `Game.Draw()` loops

---

## 6. Input Abstraction

Separate raw input from game actions so you can rebind keys later and support multiple input methods.

```csharp
public enum GameAction
{
    MoveUp, MoveDown, MoveLeft, MoveRight,
    Interact, Cancel, Pause, OpenInventory
}

public class InputManager
{
    private Dictionary<GameAction, Keys> _bindings;
    
    public bool IsActionPressed(GameAction action) { /* ... */ }
    public bool IsActionJustPressed(GameAction action) { /* ... */ }
}
```

**To-do:**
- [ ] Create a `GameAction` enum for all player-facing actions
- [ ] Build an `InputManager` that maps actions to keys
- [ ] Track previous and current keyboard state for edge detection (`JustPressed` vs `Held`)
- [ ] Replace any direct `Keyboard.GetState()` calls in gameplay code with `InputManager` queries

---

## 7. Data File Structure

Organize your game's data files so they're easy to find and extend.

```
Content/
├── Data/
│   ├── tiles.json          # TileDefinition registry
│   ├── entities.json       # EntityDefinition registry
│   └── items.json          # Item definitions (when you get to inventory)
├── Maps/
│   ├── overworld_01.json
│   ├── dungeon_01.json
│   └── ...
├── Sprites/
│   ├── tileset.png
│   ├── characters.png
│   └── ...
└── Audio/                  # future
```

**To-do:**
- [ ] Establish a `Content/Data/` directory for all JSON data files
- [ ] Establish a `Content/Maps/` directory for exported map files
- [ ] Build a lightweight `DataLoader` utility that reads JSON files through MonoGame's content pipeline or raw file I/O
- [ ] Document your data file formats (even a brief comment header in each JSON is enough)

---

## Priority Order

If you tackle these roughly in order, each step gives you something testable:

1. **Tile and entity data models** (1a, 1b) — formalize what you already have
2. **Map runtime format + loader** (1c, 3) — load your editor maps into the game cleanly
3. **Game state + player state** (2) — centralized state you can inspect and save
4. **Save/load** (4) — prove the data round-trips correctly
5. **Input abstraction** (6) — small but pays off immediately
6. **Screen manager** (5) — needed before you build menus and dialogue
7. **Data file structure** (7) — organize as you go, formalize once you have a few files

---

## Design Principles to Hold Onto

- **Data-driven over code-driven.** New tile types, entity types, and items should be added by editing JSON, not by writing new classes. Code handles behavior categories; data handles instances.
- **Serialize everything from day one.** If you can't save it, it doesn't exist. Make `GameState` serializable before you add anything to it.
- **The property bag is your friend.** `Dictionary<string, string>` on entities lets you experiment without refactoring your class hierarchy every time you have a new idea.
- **Keep the editor and game loosely coupled.** The editor exports data files. The game reads data files. They don't share code at runtime. This means you can change either one without breaking the other.
- **One source of truth.** Tile definitions live in one file. Entity definitions live in one file. The map references them by ID. No duplicated data.