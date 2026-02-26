# TileForge — Game Runtime PRD

## Mission
Evolve TileForge's embedded play mode into a full RPG game runtime. The editor remains the authoring tool. F5 launches a game loop with state management, screen stacking, map transitions, and save/load.

---

## Completed Architecture (G1–G11 + Quest Editor + Dialogue Editor + UI Overhaul + Form Layout)

1266 tests, 0 failures. All systems below are implemented and tested.

### Data Model & Registries (G1)
TileGroup gained gameplay properties: `IsPassable`, `IsHazardous`, `MovementCost`, `DamageType`, `DamagePerTick`. Entity groups gained `EntityType` enum (NPC, Item, Trap, Trigger, Interactable) and `DefaultProperties` dictionary. `TileRegistry` and `EntityRegistry` wrap dictionary lookups. GroupEditor UI exposes all properties. Export format includes gameplay fields with backward compatibility.

### Game State & Player (G2)
`GameState` holds PlayerState, CurrentMapId, ActiveEntities, Flags (HashSet), Variables (Dictionary), ItemPropertyCache. `PlayerState` tracks position, facing, health/maxHealth, inventory, active status effects. `GameStateManager` provides mutation API: flags, variables, health, inventory, entity deactivation, status effects, map switching. PlayModeController delegates to GameStateManager.

### Input & Screen Management (G3)
`GameInputManager` maps `GameAction` enum to keys with edge detection and rebinding. `ScreenManager` maintains a play-mode-only screen stack with overlay support. GameplayScreen absorbed all movement/collision/interaction logic from PlayModeController. PauseScreen overlays with full menu.

### Map Loading & Save (G4)
`MapLoader` deserializes export JSON into `LoadedMap`. Trigger entities with `target_map`/`target_x`/`target_y` drive map transitions. Entity persistence via flags (`entity_inactive:{id}`). `SaveManager` handles slot-based save/load to `~/.tileforge/saves/`.

### Screens & Dialogue (G5)
SaveLoadScreen, InventoryScreen (item use via ItemPropertyCache), DialogueScreen (typewriter text, branching choices, flag conditions/mutations), SettingsScreen (key rebinding with JSON persistence). StatusEffect data model for burn/poison/ice/spikes.

### Gameplay Loop (G6)
StatusEffect runtime (ApplyStatusEffect, ProcessStatusEffects, GetEffectiveMovementMultiplier). GameOverScreen with restart/quit. HUD rendering (health bar, colored status messages, effect indicators). DefaultProperties on TileGroup + GroupEditor property editing UI. Inventory item-use fix via ItemPropertyCache. Sample dialogue content.

### Entity AI & Bump Combat (G7)
Turn-based bump combat (Brogue-style). `PlayerState` gained Attack/Defense stats. `CombatHelper.CalculateDamage` with floor-of-1 formula. `GameStateManager` gained entity int property helpers, `IsAttackable`, and `AttackEntity` with `AttackResult`. `IPathfinder` interface with `SimplePathfinder` (axis-priority movement + Bresenham LOS). `EntityAI.DecideAction` — static pure function dispatching on `behavior` property: idle, chase, patrol, chase_patrol. `EntityAction` struct captures Move/Attack/Idle decisions. GameplayScreen runs `ExecuteEntityTurn` after every player action (move or bump attack). Enemy melee attacks via `DamagePlayer`. Per-sprite damage flash on PlayState (player red tint, enemy white flash, 0.3s decay via MapCanvas). Combat message coloring (red for damage taken, gold for hits landed). `SyncEntityRenderState` syncs runtime entity positions/visibility to editor entities each frame. Player position synced to GameState after each move for correct AI targeting. GroupEditor NPC presets expanded with combat/AI fields. Trap presets gained optional health for destructible traps.

### Quest System (G8)
Data-driven quest system evaluating flags and variables. `QuestDefinition` loaded from `quests.json` (snake_case JSON, case-insensitive). Three objective types: `flag` (boolean), `variable_gte` (counter ≥ threshold), `variable_eq` (exact match). `QuestManager` evaluates all quests via `CheckForUpdates()` — detects quest starts, objective completions, and auto-completion with rewards. Entity event hooks: `on_kill_set_flag`/`on_kill_increment` properties on entities processed in `AttackEntity`, `on_collect_set_flag`/`on_collect_increment` in `CollectItem`, `visited_map:{id}` flag in `SwitchMap`. `QuestLogScreen` overlay (Q key) shows active quests with `[x]`/`[ ]` objective checklists and completed quests. Quest notifications via Cyan-colored status messages. `GameAction.OpenQuestLog` with `Keys.Q` default binding. GroupEditor presets expanded with quest hook properties.

### Quest Editor UI (G8+)
In-editor quest authoring replaces hand-edited `quests.json`. Two-tier UI: `QuestPanel` (sidebar list with add/edit/delete, right-click context menu) and `QuestEditor` (modal overlay with id, name, description, start/completion flags, dynamic objective list with type cycling, reward fields). `QuestFileManager` handles load/save in snake_case JSON format. Quests loaded on project open via ProjectManager. Editor integration follows existing GroupEditor modal pattern (factory methods, TextInputField focus management, Enter/Escape/Tab, null-check blocking in TileForgeGame Update loop). Quest definitions stored in `quests.json` alongside `.tileforge` file — no runtime changes needed.

### Dialogue Editor UI (G8++)
In-editor dialogue authoring replaces hand-edited JSON in `dialogues/`. Follows the Quest Editor pattern: `DialoguePanel` (sidebar list with add/edit/delete, right-click context menu) and `DialogueEditor` (modal overlay for editing individual `DialogueData`). `DialogueFileManager` handles per-file load/save in camelCase JSON format matching existing dialogue files. Key differences from Quest Editor: dialogues are individual files (`dialogues/{id}.json`) not a single file; the modal has two-level nesting (nodes → choices within each node). Each node row edits: id, speaker, text, nextNodeId, requiresFlag, setsFlag, setsVariable. Each choice row edits: text, nextNodeId, requiresFlag, setsFlag. Dynamic add/remove for both nodes and choices. Dialogues loaded on project open via `DialogueFileManager.LoadAll()`, stored in `EditorState.Dialogues`. GroupEditor "Create New..." dialogue handler fixed to write proper JSON via `DialogueFileManager.SaveOne` instead of raw `"[]"`.

### UI Overhaul — RPG Maker-Inspired Ribbon & Smart Editors
Five-phase overhaul replacing the minimal toolbar with discoverable UI. **DojoUI Widget Library:** 4 reusable controls — `Dropdown` (combo box with popup list), `MenuBar` (horizontal menus with hover-to-switch and hotkey hints), `Checkbox` (14x14 toggle), `NumericField` (digit-only TextInputField wrapper with min/max clamping). **TooltipManager** (500ms delay hover tooltips). **Menu Bar + Toolbar Ribbon:** 22px `MenuBar` with 6 menus (File, Edit, View, Tools, Play, Help) + 32px `ToolbarRibbon` with icon groups (New/Open/Save, Undo/Redo, tool palette, Play/Stop, Export). `EditorMenus` defines menu structure with hotkey strings. `MenuActionDispatcher` routes `(menuIndex, itemIndex)` → action callbacks. ToolPanel removed from sidebar — tools live in ribbon + menu. Play mode shows minimal 32px ribbon with Stop button. **Smart GroupEditor:** Type-aware property editing via `PropField` tagged union. Property type mapping: behavior → Dropdown, health/attack/defense → NumericField, target_map/dialogue_id → BrowseDropdown with "Create New..." flow, Solid/Passable/Hazard/Player → Checkbox. `IProjectContext` interface scans filesystem for available maps/dialogues and project data for known flags/variables. **Smart QuestEditor:** Objective type selector changed from click-to-cycle to Dropdown. **Polish:** `ShortcutsDialog` (categorized hotkey reference), `AboutDialog`, menu enable/disable state, contextual `StatusBar` hints per active tool.

### Form Layout & Readability Overhaul
Fixed text overflow, label truncation, and field crowding across modal editors. **DojoUI additions:** `FormLayout` (immediate-mode layout struct with standardized row methods — measures labels from font metrics instead of hardcoded offsets), `ScrollPanel` (Begin/End scissor-clipped scroll region with 6px visual scroll bar), `TextInputField.IsTextOverflowing()` for tooltip detection. **DialogueEditor refactored:** nodes expanded from 4 cramped rows to 6 full-width rows (Id, Speaker, Text, Next each full-width; Requires + Sets Flag as measured two-field row; Sets Var full-width). Choices from 2 rows to 3 (Text full-width; Next + Requires measured; Sets Flag + Del). **QuestEditor refactored:** same ScrollPanel + FormLayout treatment, measured label widths for objective rows. **Both editors:** resizable modals (drag right/bottom/corner, min 500×400), scissor-clipped scroll regions with visual scroll bar, overflow tooltips on hover (0.4s delay via per-editor TooltipManager). Draw order: `BeginScroll → content → EndScroll → dropdown popups → tooltips`.

---

## Model Assignment Guide

| Model | Use For | Examples |
|-------|---------|---------|
| **Sonnet** | Mechanical, well-specified work | Data classes, enums, serialization, dictionary wrappers, tests following patterns |
| **Opus** | Architectural integration | GroupEditor UI, PlayModeController, GameplayScreen, multi-system coordination |

Sonnet tasks can run as parallel subagents. Opus tasks run sequentially with full context.

---

## Future Phases

### Extension Points (Built into G7)

Three seams that enable future systems with zero refactoring:

**A* Pathfinding** — Build `AStarPathfinder` implementing `IPathfinder`. Swap one line in GameplayScreen constructor. EntityAI unchanged, behaviors unchanged, tests still pass.

**Ranged Combat** — New behavior `"ranged_chase"` reading `attack_range`, `preferred_distance` from property bag. Uses `IPathfinder.HasLineOfSight()` (already implemented via Bresenham). Returns `EntityAction` with `AttackTargetX/Y` populated (non-null). `ExecuteEntityTurn` already branches on null vs non-null attack target.

**Animated Enemy Movement** — Queue `EntityAction` list instead of executing immediately. Play each action as sequential lerp before returning control to player. EntityAction struct already contains all data needed for animation.

---

### G9 — Equipment & Gear ✅
`EquipmentSlot` enum (Weapon, Armor, Accessory). `PlayerState.Equipment` dictionary (slot key → item name). `GameStateManager` gained: `EquipItem`/`UnequipItem`, `GetEffectiveAttack()`/`GetEffectiveDefense()` (base + equipment bonuses via `ItemPropertyCache`), `GetItemEquipSlot()`, `IsEquipped()`. InventoryScreen evolved with equipment slots section (unequip on interact), inventory items (equip on interact for equippable items, heal for consumables). GroupEditor Item preset expanded: `equip_slot` dropdown, `equip_attack`/`equip_defense` NumericFields. Combat uses effective stats. HUD shows `ATK:{n} DEF:{n}` below health bar. `GameState.Version` bumped to 2 (backward compatible). Sprite overrides deferred.

### G10 — Visual Dialogue Tree Editor ✅
Upgraded the form-based Dialogue Editor to a visual node-graph editor. `DialogueTreeEditor` replaces `DialogueEditor` as the modal overlay opened from `DialoguePanel`. Split-pane layout: pannable/zoomable node-graph canvas (left 60%) + FormLayout-based properties panel (right 40%). `DialogueNode` gained `EditorX`/`EditorY` nullable int properties for layout persistence (omitted from JSON when null via `WhenWritingNull`). `NodeGraphCamera` provides float-precision zoom (0.25×–3.0×) with stable-center zoom-to-cursor. `DialogueAutoLayout` runs BFS from root node to assign column/row positions. `DialogueNodeWidget` computes world-space node bounds, input/output ports, hit-testing. `Renderer` gained `DrawLine` (rotated pixel texture) and `DrawBezier` (cubic approximation with 16 segments). Canvas features: middle-drag pan, scroll-wheel zoom, left-click select, left-drag move nodes, left-drag from output port to connect, right-click context menu (Add Node / Delete Node / Disconnect All), Bezier connection curves, grid dot background, node shadow, port hover highlights. Properties panel: selected node's fields (Id, Speaker, Text, Next, flags), choice sub-section with add/remove, ScrollPanel overflow. Auto-layout button in header. Deep copy on edit prevents mutation of original data until save. Backward compatible — existing dialogues without positions auto-layout on first open.

### G11 — Multimap Projects ✅
Single-project, multi-map architecture. `MapDocumentState` holds per-map data (MapData, UndoStack, camera, active layer, selection, collapsed layers). `EditorState` gained facade properties (`Map`, `UndoStack`, `ActiveLayerName`, `SelectedEntityId`, `TileSelection`) that delegate to the active `MapDocumentState` — all existing code continues working unchanged. `ProjectFile` V2 format stores multiple maps in a single `.tileforge` file with backward-compatible V1 loading. `ProjectManager` handles multimap load/save and CRUD operations (create/delete/rename/duplicate maps). `MapTabBar` UI component (24px tab strip between toolbar ribbon and canvas) with active/inactive/hover tab states, close buttons, right-click context menu (Rename/Duplicate/Delete), double-click rename, and "+" add button. `PlayModeController` pre-exports all project maps on Enter() for instant in-project map transitions via `target_map` references. Group rename/delete propagates across all maps. `ProjectContext.GetAvailableMaps()` returns project map names for entity property dropdowns. StatusBar shows active map name.

### G12 - Map Relationships
Enable map switching through events such as walking to edge of map, entering a door/portal, or casting a spell.

### G13 — Floating Combat Messages
Refactor status messages from a single shared text line to per-entity floating text that appears over the affected entity's tile. Solves the core problem: bump combat triggers player attack + immediate enemy retaliation, and the single `StatusMessage` gets overwritten before the player sees their own hit land.

**FloatingMessage data class:** text, color, tile position (X/Y from affected entity), timer, vertical offset (drifts upward over lifetime). **PlayState changes:** replace `StatusMessage`/`StatusMessageTimer` with `List<FloatingMessage>`. Add `AddFloatingMessage(text, color, tileX, tileY)` helper. **Message spawning:** each message spawns at the tile of the entity that was affected — player-hits-enemy floats over the enemy tile (gold), enemy-hits-player floats over the player tile (red), item collection floats over the item tile (lime green), hazard/status-effect damage floats over the player tile (red), quest updates float over the player tile (cyan). **Rendering:** MapCanvas draws floating messages in world space (tile position × tileSize × zoom + camera offset) so they track with the camera. Text drifts upward ~16px over lifetime and fades out via alpha decay. **Lifetime:** ~1.0s per message. **Coexistence:** multiple messages render simultaneously — both the player's attack and the enemy's retaliation are visible at once on their respective tiles. **Migration scope:** all `play.StatusMessage = ...` / `play.StatusMessageTimer = ...` call sites in GameplayScreen (~12 occurrences) refactored to `AddFloatingMessage()`. Bottom-of-screen status text rendering block replaced with world-space floating text loop. Status message timer tick logic replaced with list iteration + removal of expired messages. Color classification logic (red/gold/lime/cyan) moves from Draw-time string matching to spawn-time explicit color parameter.

### Standalone Registry Export
Export `tiles.json` / `entities.json` as standalone files for external tooling. Low complexity — serialization task.

---

## File Map

```
TileForge/Game/
  ├── EntityType.cs              # NPC, Item, Trap, Trigger, Interactable
  ├── TileRegistry.cs            # Tile property lookups
  ├── EntityRegistry.cs          # Entity type lookups
  ├── GameState.cs               # Central serializable state
  ├── PlayerState.cs             # Position, health, inventory, equipment, effects
  ├── EquipmentSlot.cs           # Weapon, Armor, Accessory enum
  ├── Direction.cs               # Up/Down/Left/Right
  ├── EntityInstance.cs          # Runtime entity with properties + active flag
  ├── GameStateManager.cs        # State mutation API
  ├── GameAction.cs              # Input action enum
  ├── GameInputManager.cs        # Action→key mapping + edge detection
  ├── GameScreen.cs              # Abstract screen base
  ├── ScreenManager.cs           # Play-mode screen stack
  ├── LoadedMap.cs               # Runtime map data
  ├── MapLoader.cs               # Export JSON → LoadedMap
  ├── MapTransitionRequest.cs    # Transition target data
  ├── SaveManager.cs             # Slot-based save/load
  ├── StatusEffect.cs            # Burn/poison/ice/spikes
  ├── DialogueData.cs            # Dialogue nodes + choices (EditorX/EditorY for graph layout)
  ├── CombatHelper.cs            # CalculateDamage + AttackResult
  ├── IPathfinder.cs             # Pathfinding interface
  ├── SimplePathfinder.cs        # Axis-priority movement + Bresenham LOS
  ├── EntityAction.cs            # EntityActionType enum + EntityAction data
  ├── EntityAI.cs                # Static AI decision function (idle/chase/patrol/chase_patrol)
  ├── QuestData.cs               # QuestFile, QuestDefinition, QuestObjective, QuestRewards
  ├── QuestLoader.cs             # JSON quest file loader (snake_case + PascalCase support)
  ├── QuestEvent.cs              # QuestEventType enum + QuestEvent data
  ├── QuestManager.cs            # Quest evaluation engine (flag/variable condition checks)
TileForge/Data/
  ├── QuestFileManager.cs        # Load/save quests.json (editor-side, snake_case serialization)
  ├── DialogueFileManager.cs     # Load/save dialogues/*.json (per-file, camelCase serialization)
DojoUI/
  ├── Renderer.cs                # DrawRect, DrawRectOutline, DrawLine, DrawBezier
  ├── Dropdown.cs                # Combo-box control with popup list
  ├── MenuBar.cs                 # Horizontal menu bar with submenus + hotkey hints
  ├── Checkbox.cs                # 14×14 toggle checkbox
  ├── NumericField.cs            # Clamped integer input (wraps TextInputField)
  ├── TooltipManager.cs          # 500ms delay hover tooltip
  ├── FormLayout.cs              # Immediate-mode form layout helper (label+field rows)
  ├── ScrollPanel.cs             # Scissor-clipped scroll region with visual scroll bar
TileForge/Editor/
  ├── MapDocumentState.cs        # Per-map state container (data, undo, camera, selection)
TileForge/UI/
  ├── EditorMenus.cs             # Menu definitions + index constants
  ├── ToolbarRibbon.cs           # Icon toolbar ribbon (replaces Toolbar + ToolPanel)
  ├── MenuActionDispatcher.cs    # Menu (index, item) → action routing
  ├── MapTabBar.cs               # Multimap tab strip (tabs, close, add, context menu)
  ├── IProjectContext.cs         # Project data for browse-dropdowns + Create New
  ├── GroupEditor.cs             # Smart property editing (Dropdown/Checkbox/NumericField)
  ├── QuestPanel.cs              # Sidebar panel: quest list with add/edit/delete
  ├── QuestEditor.cs             # Quest editing with Dropdown objective types
  ├── DialoguePanel.cs           # Sidebar panel: dialogue list with add/edit/delete
  ├── DialogueEditor.cs          # Dialogue node/choice editing (form-based modal, legacy)
  ├── DialogueTreeEditor.cs      # Visual node-graph dialogue editor (replaces DialogueEditor)
  ├── NodeGraphCamera.cs         # Float-precision pan/zoom camera for node graph
  ├── DialogueAutoLayout.cs      # BFS-based auto-layout for dialogue node positions
  ├── DialogueNodeWidget.cs      # Node visual representation + port hit-testing
  ├── ConnectionRenderer.cs      # Bezier connection drawing between node ports
  ├── ShortcutsDialog.cs         # Categorized hotkey reference dialog
  ├── AboutDialog.cs             # About info dialog
TileForge/Game/Screens/
  ├── GameplayScreen.cs        # Main game loop + rendering
  ├── PauseScreen.cs           # Overlay: resume/save/load/settings/quit
  ├── SaveLoadScreen.cs        # Slot management UI
  ├── InventoryScreen.cs       # Equipment slots + item display + equip/unequip/use
  ├── DialogueScreen.cs        # Typewriter text + branching
  ├── SettingsScreen.cs        # Key rebinding
  ├── GameOverScreen.cs        # Death: restart or quit
  └── QuestLogScreen.cs        # Quest log overlay (active + completed)
```

---

## Success Criteria (G1–G6) ✓

All 32 criteria met. See DEVLOG-GAME.md for verification details.

## Test Progression

| Phase | Tests | Delta |
|-------|-------|-------|
| Baseline (editor) | 453 | — |
| G1 (data model) | 494 | +41 |
| G2 (game state) | 537 | +43 |
| G3 (screens) | 574 | +37 |
| G4 (maps/save) | 631 | +57 |
| G5 (dialogue/UI) | 722 | +91 |
| G6 (gameplay loop) | 771 | +49 |
| G7 (entity AI/combat) | 870 | +99 |
| G8 (quest system) | 925 | +55 |
| G8+ (quest editor) | 956 | +31 |
| UI Overhaul (P1–P5) | 1031 | +75 |
| G8++ (dialogue editor) | 1050 | +19 |
| Form layout overhaul | 1067 | +17 |
| G9 (equipment) | 1158 | +91 |
| Cleanup phase | 1162 | +4 |
| G10 (visual dialogue tree) | 1198 | +36 |
| G11 (multimap projects) | 1266 | +68 |