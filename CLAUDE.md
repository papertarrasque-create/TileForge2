# TILEFORGE — CLAUDE.MD

### Source
https://github.com/papertarrasque-create/TileForge2.git
Branch: `game-state` (ready for merge to `main`)

### What This Is
TileForge is a tile-based level editor with an embedded RPG game runtime. The editor is the authoring tool for all game data — tile properties, entity types, dialogue references, map transitions. Press F5 to play.

### Your Persona
Expert game designer and C# developer. Vanilla C# + MonoGame. No engine magic.

### Status
G1–G9 complete + Quest Editor UI + Dialogue Editor UI + UI Overhaul. 1119 tests, 0 failures. The game runtime has: health, damage, inventory, equipment (weapon/armor/accessory slots with stat modifiers), status effects, map transitions, save/load, dialogue, input rebinding, pause/inventory/settings/quest log screens, game over/restart, entity AI (chase/patrol/chase_patrol), bump combat, pathfinding (axis-priority + Bresenham LOS), damage flash, combat message coloring, data-driven quest system with flag/variable objectives and auto-completion. Quest Editor UI: in-editor quest authoring via QuestPanel (sidebar) + QuestEditor (modal overlay). Dialogue Editor UI: in-editor dialogue authoring via DialoguePanel (sidebar) + DialogueEditor (form-based modal with nested node/choice editing). UI Overhaul: RPG Maker-inspired menu bar + icon toolbar ribbon, smart property editors with dropdowns/checkboxes/numerics, IProjectContext browse-dropdowns with "Create New..." linkage.

**Next up: G10+** — ranged combat, A* pathfinding, visual dialogue tree editor. See PRD-GAME.md §Future Phases.

---

## Key Architecture

```
Editor (TileGroups, Entities, Map) → F5 → PlayModeController
  │
  ├─ GameStateManager (owns GameState: player, flags, variables, entities)
  ├─ GameInputManager (action→key mapping, edge detection, rebinding)
  └─ ScreenManager (play-mode-only screen stack)
       ├─ GameplayScreen (movement, collision, hazard, bump combat, entity turn, HUD)
       ├─ PauseScreen (overlay → save/load/settings/quit)
       ├─ DialogueScreen (typewriter text, branching choices, flag conditions)
       ├─ InventoryScreen (equip/unequip, item use, grouped display)
       ├─ SaveLoadScreen (slot-based save/load via SaveManager)
       ├─ SettingsScreen (key rebinding)
       ├─ QuestLogScreen (quest objectives + completion)
       └─ GameOverScreen (restart or return to editor)
```

**Data flow:** Editor → MapExporter → JSON → MapLoader → LoadedMap → GameStateManager
**Entity state:** Persists via flags (`entity_inactive:{id}`), not stored on entities
**Map transitions:** Trigger entities with `target_map`/`target_x`/`target_y` properties
**Save files:** `~/.tileforge/saves/{slot}.json` — serialized GameState
**Key bindings:** `~/.tileforge/keybindings.json`
**Combat:** Bump-to-attack (Brogue-style). `damage = max(1, atk - def)`. Uses effective stats (base + equipment bonuses). Entities act after player.
**Equipment:** EquipmentSlot enum (Weapon, Armor, Accessory). `PlayerState.Equipment` dict. `GameStateManager.GetEffectiveAttack()/GetEffectiveDefense()` sum base + `equip_attack`/`equip_defense` from ItemPropertyCache. InventoryScreen handles equip/unequip. HUD shows ATK/DEF readout.
**Entity AI:** Property-driven behaviors: idle, chase, patrol, chase_patrol. IPathfinder interface.
**Quests:** JSON definitions in `quests.json`. QuestManager evaluates flag/variable objectives. Entity hooks: `on_kill_set_flag`, `on_kill_increment`, `on_collect_set_flag`, `on_collect_increment`. Auto-complete with rewards.
**Quest Editor:** QuestPanel (sidebar) + QuestEditor (modal overlay) for in-editor quest authoring. QuestFileManager reads/writes quests.json.
**Dialogue Editor:** DialoguePanel (sidebar) + DialogueEditor (form-based modal) for in-editor dialogue authoring. DialogueFileManager reads/writes per-file `dialogues/{id}.json`. Two-level nesting: nodes → choices.
**Editor UI:** MenuBar (22px, 6 menus with hotkey hints) + ToolbarRibbon (32px, icon groups with tooltips) replaces old Toolbar + ToolPanel. GroupEditor uses type-aware controls: Dropdown (behavior, damage_type, entity_type), NumericField (health, attack, defense), BrowseDropdown (target_map, dialogue_id with "Create New..."), Checkbox (Solid, Passable, Hazard, Player). IProjectContext scans filesystem for available maps/dialogues and project data for flags/variables. Play mode shows minimal 32px ribbon with Stop button only.

---

## Design Principles
- **Data-driven over code-driven.** New content = JSON data, not new classes.
- **Serialize everything.** If GameState can't round-trip to JSON, it doesn't exist.
- **Property bags for extensibility.** `Dictionary<string, string>` on entities avoids premature hierarchies.
- **Editor is the authoring tool.** Gameplay properties are set in the GroupEditor UI and exported.
- **Evolve, don't rebuild.** Enhance existing systems. Don't create parallel ones.
- **One source of truth.** TileGroup defines both tiles and entities. Maps reference by name.

## Architecture Rules
- Game runtime code: `TileForge/Game/` namespace
- All new classes testable without MonoGame (follow ISpriteSheet pattern)
- System.Text.Json for all serialization
- ScreenManager is play-mode-only — the editor is NOT a GameScreen
- xUnit tests for everything; 1119 baseline, 0 failures allowed

## Model Assignment (for Claude Code)
- **Sonnet** — Mechanical work: data classes, enums, serialization, tests following established patterns
- **Opus** — Architectural integration: GroupEditor UI, PlayModeController evolution, GameplayScreen, multi-system coordination
- Sonnet tasks can run as parallel subagents. Opus tasks run sequentially with full context.

## Documentation
- **PRD-GAME.md** — Architecture reference (what was built) + future phase specs
- **DEVLOG-GAME.md** — Key decisions and outcomes
- **GameStateTodo.md** — Original planning reference (historical, read-only)