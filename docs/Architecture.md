---
updated: 2026-03-06
status: current
---

# TileForge -- Architecture

## Overview

TileForge is a single MonoGame application with two modes: **Editor** (default) and **Play** (F5). The editor is *not* a game screen -- it uses its own UI stack (PanelDock, DialogManager, InputRouter). Play mode pushes a screen stack on top via `PlayModeController`.

```
TileForgeGame.cs (thin orchestrator)
  |
  +-- Editor Mode
  |     +-- EditorState (central state, 8 events, facade over MapDocumentState)
  |     +-- InputRouter (keyboard shortcuts, tool switching)
  |     +-- ProjectManager (save/load/open)
  |     +-- DialogManager (modal lifecycle)
  |     +-- MenuBar + ToolbarRibbon + MenuActionDispatcher
  |     +-- PanelDock (sidebar: MapPanel, GroupEditor, TilePalette, etc.)
  |     +-- Modal Editors: QuestEditor, DialogueTreeEditor, WorldMapEditor
  |
  +-- Play Mode (F5)
        +-- PlayModeController (mediator: enter/exit, save/restore editor state)
        +-- GameStateManager (owns GameState: player, flags, variables, entities)
        +-- GameInputManager (action->key mapping, edge detection)
        +-- ScreenManager (play-mode-only screen stack)
              +-- GameplayScreen (movement, combat, entity turns, HUD)
              +-- PauseScreen (overlay -> child screens)
              +-- DialogueScreen, InventoryScreen, SaveLoadScreen
              +-- SettingsScreen, QuestLogScreen, GameOverScreen
```

## Three Projects

```
DojoUI/             Shared UI library (Camera, Renderer, SpriteSheet, widgets)
TileForge/          Main application (editor + game runtime)
TileForge.Tests/    xUnit tests (1507)
```

DojoUI provides low-level drawing (`Renderer`), input widgets (`TextInputField`, `Dropdown`, `Checkbox`, `NumericField`), layout helpers (`FormLayout`, `ScrollPanel`), and utilities (`TextUtils`, `TooltipManager`). It knows nothing about TileForge.

## Data Flow

### Editor -> Play Mode

```
TileGroup (editor) -+-> MapExporter -> JSON -> MapLoader -> LoadedMap
                    |
EditorState --------+-> PlayModeController.Enter()
                    |     saves editor state
                    |     exports ALL project maps to memory dict
                    |     creates GameStateManager + ScreenManager
                    |     pushes GameplayScreen
                    |
                    +-> PlayModeController.Exit()
                          clears screen stack
                          restores editor state
                          nulls game runtime
```

### Runtime Data

```
GameState (serializable)
  +-- PlayerState (position, health, poise, inventory, equipment, status effects)
  +-- Flags: HashSet<string> (entity persistence, quest state, dialogue choices)
  +-- Variables: Dictionary<string, int> (quest counters)
  +-- EntityStates: Dictionary<string, EntityInstance> (per-entity runtime state)
  +-- ItemPropertyCache: Dictionary<string, Dictionary<string, string>>
  +-- ActiveQuests, CompletedQuests
  +-- CurrentMapName, Version
```

Save/load serializes `GameState` to `~/.tileforge/saves/{slot}.json` via `SaveManager`.

## Key Boundaries

### Editor / Play Mode Boundary

`PlayModeController` is the sole mediator. It:
1. Saves editor state (map, groups, camera) on Enter
2. Exports all maps to an in-memory dictionary
3. Creates the game runtime (GameStateManager, ScreenManager, GameInputManager)
4. Restores editor state on Exit

**Coupling concern:** `GameplayScreen` takes a direct `EditorState` reference for `SyncEntityRenderState()`. This means play-mode code can mutate editor state -- a fragile dependency noted in the code review.

### Game Runtime Namespace

All game runtime code lives in `TileForge/Game/`. Key classes:
- `GameStateManager` -- State mutation API (damage, heal, inventory, flags, transitions)
- `GameInputManager` -- Action-based input abstraction with rebinding
- `ScreenManager` -- Stack-based screen management (only topmost gets Update)
- `GameplayScreen` -- Core game loop (movement, combat, entity AI, HUD)
- `EntityAI` -- Static/pure AI decisions (state in, action out)
- `CombatHelper` -- Damage calculation with terrain/position modifiers
- `EdgeTransitionResolver` -- World layout edge transition logic

### UI Layer

The editor UI is immediate-mode (no retained widget tree). `TileForgeGame.Update()` uses an early-return priority chain:
```
DialogManager > QuestEditor > DialogueEditor > WorldMapEditor >
GroupEditor > InputRouter > Play Mode > Editor
```

Modal editors (QuestEditor, DialogueTreeEditor, WorldMapEditor) are full-screen overlays with split-pane layouts -- pannable/zoomable canvas + properties panel.

## Entity System

Entities use **property bags** (`Dictionary<string, string>`) rather than class hierarchies. `TileGroup.DefaultProperties` are inherited by entity instances at placement time.

Key property conventions:
- `entity_type`: NPC, Item, Trap, Trigger, Interactable
- `behavior`: idle, chase, patrol, chase_patrol
- `health`, `attack`, `defense`, `poise`, `speed`, `aggro_range`
- `equip_slot`, `equip_attack`, `equip_defense`, `equip_poise`, `equip_ap`
- `target_map`, `target_x`, `target_y` (trigger transitions)
- `dialogue_id`, `on_kill_set_flag`, `on_collect_increment` (hooks)
- `alert_turns` (runtime, set by noise system)

This avoids class proliferation but is stringly typed -- errors are silent and validation is manual.

## Combat System

AP-based tactical combat:
- 2 AP/turn (configurable via equipment). Move = 1 AP, attack = 1 AP.
- `damage = max(1, (atk - (def + terrain)) * positionMult)`
- Terrain defense from `TileGroup.DefenseBonus`
- Backstab (2x) / Flanking (1.5x) based on 4-directional facing
- Poise: regenerating shield buffer, absorbs damage before health
- Auto-end-turn when no hostiles nearby (exploration feels seamless)
- Entity speed property (1-3 actions per turn)
- Noise/alertness: stepping on loud tiles alerts nearby dormant enemies

## Map System

### Multimap Projects

`ProjectFile` V2 stores multiple maps. `MapDocumentState` holds per-map state. `EditorState` is a facade that delegates to the active document. `MapTabBar` provides tab-based switching.

### Map Transitions

Two mechanisms:
1. **Trigger-based** -- Entity with `target_map`/`target_x`/`target_y` properties
2. **Edge-based** -- `WorldLayout` grid determines N/S/E/W neighbors. `EdgeTransitionResolver` handles spawn position. Custom exit points for portal-style interior transitions.

## Quest and Dialogue Systems

- **Quests:** JSON definitions in `quests.json`. `QuestManager` polls flags/variables (no event bus). Entity hooks (`on_kill_set_flag`, etc.) modify state inline.
- **Dialogues:** Per-file JSON in `dialogues/{id}.json`. Visual node-graph editor (`DialogueTreeEditor`) with BFS auto-layout. Nodes have `EditorX/EditorY` for layout persistence.

## Where Coupling Exists

1. **GameplayScreen <-> EditorState** -- Direct reference for entity render sync
2. **TileForgeGame.Update() priority chain** -- Modal routing is hardcoded cascading if/else
3. **Property bags are unvalidated** -- Misspelled property names fail silently
4. **DojoUI widgets assume immediate-mode** -- No retained state means some patterns require awkward workarounds (TextInputField scissor batch breaks)
5. **Game screens construct each other** -- GameplayScreen creates PauseScreen in its constructor chain

## File Map

See `PRD-GAME.md` for the full file map. Key files:

| File | Role |
|------|------|
| `TileForgeGame.cs` | Thin orchestrator (MenuBar + Ribbon + routing) |
| `EditorState.cs` | Central state facade with 8 events |
| `GroupEditor.cs` | Smart property editing UI |
| `GameplayScreen.cs` | Core game loop (largest file) |
| `GameStateManager.cs` | State mutation API |
| `PlayModeController.cs` | Editor/play mode mediator |
| `MapDocumentState.cs` | Per-map state container |

## Related Docs

- [[Brief]] -- Project identity and goals
- [[Status]] -- Current state and known issues
- [[ADRs/001-initial-architecture|ADR-001]] -- Why these decisions were made
