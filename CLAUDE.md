# TILEFORGE — CLAUDE.MD

### Source
https://github.com/papertarrasque-create/TileForge2.git
Branch: `game-state` (ready for merge to `main`)

### What This Is
TileForge is a tile-based level editor with an embedded RPG game runtime. The editor is the authoring tool for all game data — tile properties, entity types, dialogue references, map transitions. Press F5 to play.

### Your Persona
Expert game designer and C# developer. Vanilla C# + MonoGame. No engine magic.

### Status
G1–G14 complete + Quest Editor UI + Dialogue Editor UI + UI Overhaul + Visual Dialogue Tree Editor + World Map Editor + AP Combat + Tactical Combat. 1507 tests, 0 failures. The game runtime has: health, damage, poise (regenerating shield), inventory, equipment (weapon/armor/accessory slots with stat modifiers including poise), status effects, map transitions (trigger-based + edge-based), save/load, dialogue, input rebinding, pause/inventory/settings/quest log screens, game over/restart, entity AI (chase/patrol/chase_patrol with noise alertness), AP combat (2 AP/turn, bump attack, directional attack, entity speed 1-3), terrain defense bonuses, backstab/flanking positional combat, noise/alertness stealth system, floating combat messages, pathfinding (axis-priority + Bresenham LOS), damage flash, data-driven quest system with flag/variable objectives and auto-completion. Quest Editor UI: in-editor quest authoring via QuestPanel (sidebar) + QuestEditor (modal overlay). Dialogue Editor UI: visual node-graph editor (DialogueTreeEditor) with split-pane layout — pannable/zoomable canvas with draggable nodes and Bezier connection lines + FormLayout properties panel. Auto-layout via BFS. UI Overhaul: RPG Maker-inspired menu bar + icon toolbar ribbon, smart property editors with dropdowns/checkboxes/numerics, IProjectContext browse-dropdowns with "Create New..." linkage. Multimap Projects: single-project multi-map with MapTabBar (tab switching, CRUD), shared groups/spritesheet, in-project play mode transitions, project file V2 format. World Map Editor: visual grid editor (WorldMapEditor) for spatial map adjacency — auto-bidirectional edge transitions from grid position, custom spawn points per edge (editable NumericField), custom exit point coordinates (portal-style interior transitions), EdgeTransitionResolver runtime.

**Next up: G15+** — ranged combat, A* pathfinding, animated enemy movement. See PRD-GAME.md §Future Phases.

---

## Key Architecture

```
Editor (TileGroups, Entities, Map) → F5 → PlayModeController
  │
  ├─ GameStateManager (owns GameState: player, flags, variables, entities)
  ├─ GameInputManager (action→key mapping, edge detection, rebinding)
  └─ ScreenManager (play-mode-only screen stack)
       ├─ GameplayScreen (movement, collision, hazard, AP combat, entity turn, HUD)
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
**Multimap:** `MapDocumentState` per map, `EditorState` facade delegates to active doc, `MapTabBar` tab strip, `ProjectFile` V2 format
**Map transitions:** Trigger entities with `target_map`/`target_x`/`target_y` properties (in-project maps resolved by name). Edge-based transitions via `WorldLayout` grid — walking off map edge auto-transitions to adjacent map. `EdgeTransitionResolver` resolves direction + neighbor + spawn position. Custom `EdgeSpawn` overrides per direction, defaults to opposite edge with clamped coordinate. Custom exit points (`NorthExit`/`SouthExit`/`EastExit`/`WestExit`) define portal-style transition tiles at arbitrary map positions (not just edges). Exit points coexist with edge-of-map transitions.
**Save files:** `~/.tileforge/saves/{slot}.json` — serialized GameState
**Key bindings:** `~/.tileforge/keybindings.json`
**Combat:** Action Point system (2 AP/turn). Move costs 1 AP, bump attack costs 1 AP. Directional attack via Interact key (Z) costs 1 AP. End turn with Space. `damage = max(1, (atk - (def + terrain)) * positionMult)`. Terrain defense bonus from tiles (`TileGroup.DefenseBonus`). Backstab (2x) and flanking (1.5x) positional multipliers based on 4-directional entity facing. Poise: regenerating shield buffer (20 base, configurable via `equip_poise`) — damage hits poise first, health only after poise breaks. Poise regenerates when no hostiles nearby. Entity poise via `poise` property bag. Uses effective stats (base + equipment bonuses). Auto-end-turn when no hostiles within aggro range (exploration feels seamless). Entity speed property (1-3 actions per turn). `PlayerState.MaxAP` persisted; `PlayState.PlayerAP`/`IsPlayerTurn` ephemeral. Equipment can modify MaxAP via `equip_ap`. HUD shows health bar, poise bar, AP pips, ATK/DEF/COVER readout, and "SPACE: End Turn" hint during combat.
**Noise/Alertness:** Tile-based noise system (`TileGroup.NoiseLevel`: 0=silent, 1=normal, 2=loud). Player steps propagate noise (radius = 3 * noiseLevel). Dormant entities within noise radius get `alert_turns = 3`, doubling their aggro range. Alert-aware `AnyHostileNearby` prevents auto-end-turn for alerted enemies. Alert decrements each entity turn.
**Floating Messages:** Per-entity floating text replaces single `StatusMessage`. `FloatingMessage` with text, color, tile position, timer, vertical drift. World-space rendering with alpha fade.
**Equipment:** EquipmentSlot enum (Weapon, Armor, Accessory). `PlayerState.Equipment` dict. `GameStateManager.GetEffectiveAttack()/GetEffectiveDefense()` sum base + `equip_attack`/`equip_defense` from ItemPropertyCache. InventoryScreen handles equip/unequip. HUD shows ATK/DEF readout.
**Entity AI:** Property-driven behaviors: idle, chase, patrol, chase_patrol. IPathfinder interface.
**Quests:** JSON definitions in `quests.json`. QuestManager evaluates flag/variable objectives. Entity hooks: `on_kill_set_flag`, `on_kill_increment`, `on_collect_set_flag`, `on_collect_increment`. Auto-complete with rewards.
**Quest Editor:** QuestPanel (sidebar) + QuestEditor (modal overlay) for in-editor quest authoring. QuestFileManager reads/writes quests.json.
**Dialogue Editor:** DialoguePanel (sidebar) + DialogueTreeEditor (visual node-graph modal) for in-editor dialogue authoring. Split-pane: canvas (pan/zoom, draggable nodes, Bezier connections, right-click context menu) + properties panel (FormLayout fields for selected node). DialogueAutoLayout for BFS-based node positioning. NodeGraphCamera for float-precision zoom. DialogueFileManager reads/writes per-file `dialogues/{id}.json`. DialogueNode has EditorX/EditorY for layout persistence.
**World Map Editor:** WorldMapEditor modal overlay (View > World Map, Ctrl+W). Split-pane: pannable/zoomable grid canvas (left 65%) + properties panel (right 35%). Maps placed on 2D grid — position determines N/S/E/W neighbors (auto-bidirectional). Features: drag to reposition, right-click context menu, placement mode for unplaced maps, per-edge custom spawn points. `WorldLayout` stored in `ProjectFile.ProjectData`, null when unconfigured (backward compatible).
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
- xUnit tests for everything; 1507 baseline, 0 failures allowed
- **ASCII-only in rendered strings.** Any string passed to `SpriteFont.MeasureString()` or `SpriteBatch.DrawString()` must use only characters present in the bundled SpriteFont (printable ASCII 32–126). No Unicode ellipsis (`…`), em-dashes, curly quotes, or other non-ASCII glyphs — use ASCII equivalents (`...`, `--`, `"`, `'`).

## Model Assignment (for Claude Code)
- **Sonnet** — Mechanical work: data classes, enums, serialization, tests following established patterns
- **Opus** — Architectural integration: GroupEditor UI, PlayModeController evolution, GameplayScreen, multi-system coordination
- Sonnet tasks can run as parallel subagents. Opus tasks run sequentially with full context.

## Documentation
- **PRD-GAME.md** — Architecture reference (what was built) + future phase specs
- **DEVLOG-GAME.md** — Key decisions and outcomes
- **GameStateTodo.md** — Original planning reference (historical, read-only)