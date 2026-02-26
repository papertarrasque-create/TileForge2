# TileForge — Game State Development Log

---

## Architecture Decisions (Established G1–G6)

These decisions were made during initial development and should be preserved going forward.

1. **TileGroup gets richer, not replaced.** Gameplay properties (hazard, movement cost, damage, entity type, default properties) live directly on TileGroup rather than parallel definition classes. One source of truth.

2. **Editor UI for all data authoring.** No hand-edited JSON definition files for tiles or entities. The GroupEditor exposes all gameplay properties. Dialogue and quest data are authored via in-editor form-based editors (DialogueEditor, QuestEditor).

3. **Evolve PlayModeController in place.** Rather than a separate game executable, play mode grows inside the editor. PlayModeController delegates to ScreenManager + GameStateManager.

4. **ScreenManager is play-mode-only.** The editor's UI system (PanelDock, DialogManager, InputRouter) is not wrapped in GameScreen. ScreenManager operates only between F5 enter and exit.

5. **Flag-based entity persistence.** Entities are stateless in map files. State lives in `GameState.Flags` (e.g., `entity_inactive:{id}`). On map re-entry, entity IsActive is derived from flags.

6. **ItemPropertyCache for cross-map inventory.** Entity properties are cached at collection time in `GameState.ItemPropertyCache` so inventory item-use works after map transitions clear ActiveEntities.

7. **Property bags everywhere.** `Dictionary<string, string>` on entities and `DefaultProperties` on TileGroup provide extensibility without class proliferation. Behavior is driven by EntityType + property keys.

8. **Status effects are step-based.** Effects tick per player movement step, not real-time. RemainingSteps decrements after each completed move. This keeps the system deterministic and save-friendly.

---

## Phase Summary

### G1 — Core Data Model & Registries
Added gameplay properties to TileGroup, EntityType enum, TileRegistry/EntityRegistry, GroupEditor UI extensions, export format update. **453 → 494 tests.**

Notable: `TileForge.Game` namespace shadowed MonoGame's `Game` class. Fixed by fully qualifying `Microsoft.Xna.Framework.Game` in TileForgeGame.cs.

### G2 — Game State & Player
Created GameState/PlayerState/EntityInstance/GameStateManager. Evolved PlayModeController with hazard damage, movement cost, entity interaction dispatch by EntityType. **494 → 537 tests.**

Notable: Movement cost modifies lerp duration per-move (`CurrentMoveDuration` on PlayState), not a global constant.

### G3 — Input Abstraction & Screen Management
GameInputManager with edge detection, ScreenManager with overlay support, GameplayScreen absorbed all game logic from PlayModeController, PauseScreen. PlayModeController thinned from ~300 to ~100 lines. **537 → 574 tests.**

Notable: Update signature dropped `prevKeyboard` parameter — GameInputManager tracks its own previous state. All existing tests updated.

### G4 — Map Loading, Transitions & Save
MapLoader, trigger-based map transitions, entity persistence via flags, SaveManager with slot-based saves. PlayModeController saves/restores editor state during play. **574 → 631 tests.**

Notable: New projects now seed a default Player entity group at map center (previously empty projects couldn't enter play mode).

### G5 — Screens, Dialogue & Input Rebinding
SaveLoadScreen, InventoryScreen, DialogueScreen (typewriter + branching + flags), SettingsScreen, StatusEffect data model, GameInputManager rebinding API. **631 → 722 tests.**

Notable: GameplayScreen constructor chain grew significantly — SaveManager, GameInputManager, bindings path, and dialogue base path all flow from PlayModeController through the screen stack.

### G6 — Gameplay Loop Completion
Connected all G5 scaffolding to live gameplay. StatusEffect runtime, GameOverScreen, HUD rendering, DefaultProperties + GroupEditor property editing, inventory item-use fix (ItemPropertyCache), sample dialogue content. **722 → 771 tests.**

Notable bugs fixed: inventory cross-map resolution, EntityTool placing entities with empty properties, status messages never being drawn.

### G7 — Entity AI & Bump Combat
Turn-based bump combat system (Brogue-style). Entities act after each player step. Four AI behaviors (idle, chase, patrol, chase_patrol) driven by property bags. Bump combat: walk into an enemy to attack it. IPathfinder interface with SimplePathfinder (axis-priority + Bresenham LOS). Per-sprite damage flash and combat message coloring. GroupEditor presets expanded for NPC combat fields and destructible traps. **771 → 870 tests.**

Notable design decisions:
- **Bump combat separated from entity interaction.** `TryBumpAttack` runs before `CheckEntityInteractionAt` in the blocked-move path, so attackable NPCs get attacked while friendly NPCs still show dialogue.
- **Entity turn fires from two paths.** After player move completion (in the IsMoving block) and after bump attacks (in the blocked-move branch). Both paths call `ExecuteEntityTurn`.
- **Pathfinder bridges MapData↔LoadedMap.** GameplayScreen uses EditorState's MapData, but SimplePathfinder takes LoadedMap. `CreatePathfinder` builds a lightweight LoadedMap sharing the same Cells arrays.
- **AI is pure and static.** `EntityAI.DecideAction` is a static function — state in, action out. Only side effect is writing patrol tracking properties (`patrol_origin`, `patrol_dir`) onto the entity's Properties dict.
- **Instant enemy movement.** Entities snap to new positions — no lerp animation. The EntityAction struct captures everything needed for future animation (queue actions, play as sequential lerps).

Notable bugs fixed:
- **Player position not synced to GameState.** `play.PlayerEntity.X/Y` (editor Entity) was updated on move completion, but `GameState.Player.X/Y` (PlayerState) was never synced. EntityAI always saw the player's starting position — enemies would chase/attack the spawn point, not the player's current tile. Fixed by syncing `State.Player.X/Y` immediately after each move, before `ExecuteEntityTurn` runs.
- **Entity positions not synced to renderer.** `ExecuteEntityTurn` updated `EntityInstance.X/Y` in `ActiveEntities`, but MapCanvas renders from `_state.Map.Entities` — separate `Entity` objects with their own X/Y. Entities appeared frozen at spawn positions. Fixed with `SyncEntityRenderState()` at end of every Update frame, copying runtime positions back to editor entities. Deactivated entities (killed/collected) are removed from the render list. Safe because editor state is restored from `_savedMap` on play mode exit.
- **Per-sprite damage flash.** Replaced fullscreen red overlay with per-sprite flash timers on PlayState (`PlayerFlashTimer`, `EntityFlashTimer`, `FlashedEntityId`). Player sprite tints red when hit; enemy sprite tints white when attacked. Flash rendering moved to MapCanvas.DrawEntities for correct per-entity visual feedback.

### G8 — Quest System
Data-driven quest system built on existing flags/variables infrastructure. Quest definitions loaded from `quests.json`. QuestManager evaluates three objective types (flag, variable_gte, variable_eq) and auto-completes with reward application. Entity event hooks (`on_kill_set_flag`, `on_kill_increment`, `on_collect_set_flag`, `on_collect_increment`) added to AttackEntity and CollectItem for quest tracking. Map visit tracking via `visited_map:{id}` flags. QuestLogScreen overlay with `[x]`/`[ ]` checklist. Cyan HUD notifications. **870 → 925 tests.**

Notable design decisions:
- **QuestManager is a polling evaluator, not event-driven.** No event bus or observer pattern. `CheckForUpdates()` is called after state-changing actions in GameplayScreen. This keeps the architecture simple — quest state derives entirely from GameState flags/variables.
- **Entity properties drive kill/collect tracking.** Rather than adding hooks or callbacks to GameStateManager, entities carry `on_kill_set_flag`/`on_kill_increment` properties that are processed inline in `AttackEntity` and `CollectItem`. Data-driven, consistent with the property bag philosophy.
- **Auto-completion, no turn-in.** Quests complete automatically when all objectives are met. "Return to NPC" can be modeled as a flag objective set by dialogue.
- **Duplicate notification prevention.** `_reportedStarts` and `_reportedObjectives` HashSets prevent the same quest event from firing twice per session. Session-only — on load, QuestManager is reconstructed fresh.
- **Custom JSON converter for snake_case support.** QuestLoader uses a `NormalizedQuestFileConverter` that strips underscores and lowercases property names, supporting both `start_flag` (JSON convention) and `StartFlag` (C# convention).

### G8+ — Quest Editor UI
In-editor quest authoring UI replacing hand-edited `quests.json`. QuestPanel (sidebar, Fixed panel in PanelDock) shows quest list with add/double-click-edit/right-click context menu (Edit/Delete). QuestEditor (modal overlay, follows GroupEditor pattern) provides full quest editing: id, name, description, start flag, completion flag, dynamic objective list, reward fields. QuestFileManager handles JSON serialization (snake_case via `JsonNamingPolicy.SnakeCaseLower`). **925 → 956 tests.**

Notable design decisions:
- **Two-tier UI matches existing pattern.** QuestPanel is analogous to MapPanel's group list; QuestEditor is analogous to GroupEditor's modal overlay. Same signal pattern (WantsNewQuest/WantsEditQuestIndex/WantsDeleteQuestIndex → TileForgeGame consumes).
- **quests.json stays external to .tileforge file.** Quest data is game content, not editor state. Matches dialogue JSON pattern (separate files in project directory).
- **QuestFileManager delegates reading to QuestLoader.** No duplicate deserialization code. Writing uses `System.Text.Json` with snake_case policy; reading uses QuestLoader's `NormalizedQuestFileConverter`.
- **Objective type cycling.** Type button cycles flag → var≥ → var== on click. UI adapts: flag type shows Flag field; variable types show Variable + Value fields.
- **Reward text as comma-separated strings.** Flags: "flag_a, flag_b". Variables: "gold=100, rep=5". Simple parsing, no complex nested UI.
- **Quests loaded on project open.** ProjectManager.Load reads quests.json alongside .tileforge. New projects start with empty quest list.

---

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

| UI Overhaul (P1-P5) | 1031 | +75 |
| G8++ (dialogue editor) | 1050 | +19 |
| Form layout overhaul | 1067 | +17 |
| G9 (equipment) | 1158 | +91 |
| Cleanup phase | 1162 | −27 registry, +31 GameMenuList |
| G10 (visual dialogue tree) | 1198 | +36 |
| G11 (multimap projects) | 1266 | +68 |

All phases: 0 failures, 0 regressions.

---

## UI Overhaul — RPG Maker-Inspired Ribbon & Discoverable Fields

Five-phase overhaul replacing the minimal toolbar with a full menu bar + icon toolbar ribbon, and transforming raw text fields into type-aware controls (dropdowns, checkboxes, numeric inputs).

### What Changed

**Phase 1 — DojoUI Widget Library:** Built 4 reusable controls: `Dropdown` (combo box with popup list), `MenuBar` (horizontal menus with hover-to-switch), `Checkbox` (14x14 toggle), `NumericField` (digit-only TextInputField wrapper with min/max clamping).

**Phase 2 — Menu Bar + Toolbar Ribbon:** Replaced 28px toolbar with 22px MenuBar + 32px ToolbarRibbon (54px total top chrome). Six menus: File, Edit, View, Tools, Play, Help — every action has a listed hotkey. Removed ToolPanel from sidebar (tools live in ribbon + menu). Play mode shows minimal 32px ribbon with Stop button only.

**Phase 3 — Smart GroupEditor:** Replaced all raw TextInputFields with type-aware controls. Behavior → Dropdown, health/attack/defense → NumericField, target_map/dialogue → BrowseDropdown with "Create New..." linkage via IProjectContext. Tile properties (Passable, Hazard) → Checkbox. Entity type → Dropdown. Click-to-cycle buttons eliminated.

**Phase 4 — Smart QuestEditor:** Objective type selector changed from click-to-cycle button to Dropdown ["flag", "var>=", "var=="]. Dropdown popups drawn with proper z-ordering.

**Phase 5 — Polish:** TooltipManager (500ms delay hover tooltips), ShortcutsDialog (categorized hotkey reference), AboutDialog, menu enable/disable state (Undo/Redo/Export grey out when unavailable), contextual StatusBar hints per active tool.

### Key Decisions
- **IProjectContext interface** scans filesystem for available maps/dialogues and project data for known flags/variables. Enables "Create New..." flow that opens InputDialog, creates skeleton file, and refreshes the browse-dropdown.
- **Dropdown z-ordering** uses separate DrawPopup pass after all content, matching ContextMenu pattern.
- **GroupEditor.Update** signature expanded to include `SpriteFont font, int screenW, int screenH` for dropdown popup geometry.
- **Menu enable/disable** driven by `UndoStack.CanUndo/CanRedo` and `state.Sheet != null` / `state.Map != null` checks each frame.

### New Files
| File | Purpose |
|------|---------|
| `DojoUI/Dropdown.cs` | Combo-box control |
| `DojoUI/MenuBar.cs` | Menu bar with submenus |
| `DojoUI/Checkbox.cs` | Toggle checkbox |
| `DojoUI/NumericField.cs` | Clamped integer input |
| `DojoUI/TooltipManager.cs` | Delayed hover tooltip |
| `TileForge/UI/EditorMenus.cs` | Menu definitions + indices |
| `TileForge/UI/ToolbarRibbon.cs` | Icon toolbar ribbon |
| `TileForge/UI/MenuActionDispatcher.cs` | Menu action routing |
| `TileForge/UI/IProjectContext.cs` | Project data provider |
| `TileForge/UI/ShortcutsDialog.cs` | Hotkey reference dialog |
| `TileForge/UI/AboutDialog.cs` | About info dialog |

---

### G8++ — Dialogue Editor UI
In-editor dialogue authoring following the Quest Editor pattern. **1031 → 1050 tests.**

Three-layer architecture: `DialoguePanel` (sidebar list) + `DialogueEditor` (form-based modal) + `DialogueFileManager` (per-file I/O). Key difference from quests: dialogues are individual files (`dialogues/{id}.json`) not a single aggregate file, and nodes have nested choices requiring two-level dynamic row management in the modal.

### Key Decisions
- **Per-file I/O** rather than aggregate file. `DialogueFileManager.LoadAll()` scans directory, `SaveOne()` writes individual files, `DeleteOne()` handles renames and deletions.
- **CamelCase JSON** for dialogue files (matching existing `elder_01.json` format) vs snake_case for quest files.
- **Two-level nesting** in DialogueEditor: nodes → choices. Each node has 7 fields (id, speaker, text, nextNodeId, requiresFlag, setsFlag, setsVariable), each choice has 4 (text, nextNodeId, requiresFlag, setsFlag). Dynamic add/remove at both levels.
- **Fixed GroupEditor "Create New..." bug** — was writing raw `"[]"` for new dialogue files, now uses `DialogueFileManager.SaveOne` with proper `DialogueData` object and adds to in-memory list.
- **EditorState.Dialogues** — loaded on project open via ProjectManager, reset on new project, `DialoguesChanged` event for dirty tracking.

### New Files
| File | Purpose |
|------|---------|
| `TileForge/Data/DialogueFileManager.cs` | Per-file dialogue load/save/delete |
| `TileForge/UI/DialoguePanel.cs` | Sidebar panel: dialogue list |
| `TileForge/UI/DialogueEditor.cs` | Form-based dialogue node/choice editor |
| `TileForge.Tests/Data/DialogueFileManagerTests.cs` | File I/O + serialization tests |
| `TileForge.Tests/UI/DialogueEditorTests.cs` | Factory method + state tests |

---

### G9 — Equipment & Gear
Equipment system adding weapon/armor/accessory slots with stat modifiers. Data-driven via existing property bags — no new item classes. **1067 → 1119 tests.**

Notable design decisions:
- **Dictionary<string, string> for equipment slots.** Mirrors the Variables pattern. Keyed by `EquipmentSlot.ToString()`, stores item definition name. Extensible to future slots (ring, boots) without data model changes.
- **Effective stats computed, not stored.** `GetEffectiveAttack()`/`GetEffectiveDefense()` sum base stats + bonuses from all equipped items via `ItemPropertyCache`. No redundant stored values to keep in sync.
- **Equipment properties are just entity properties.** `equip_slot`, `equip_attack`, `equip_defense` in the entity property bag, authored via GroupEditor's Item preset. Flow through existing export pipeline unchanged — no MapExporter/MapLoader changes needed.
- **InventoryScreen evolved, not replaced.** Added equipment slots section at top of menu, `HandleInventoryInteract` replaces `UseItem` with equip/heal branching. Existing heal logic preserved. Existing InventoryScreen tests updated to account for new slot entries.
- **GameState.Version bumped to 2.** Backward compatible — v1 saves deserialize with empty Equipment dictionary (System.Text.Json default behavior). No migration code needed.
- **Sprite overrides deferred.** G9 focuses on game mechanics only. Gear-based sprite changes can be added later without architectural changes (PlayState sprite override fields + MapCanvas rendering check).
- **Combat integration is two one-line changes.** `TryBumpAttack` and `ExecuteEntityTurn` simply call `GetEffectiveAttack()`/`GetEffectiveDefense()` instead of reading base stats directly.

New/Modified files:
| File | Action |
|------|--------|
| `TileForge/Game/EquipmentSlot.cs` | **NEW** — Weapon, Armor, Accessory enum |
| `TileForge/Game/PlayerState.cs` | **MODIFIED** — Added Equipment dictionary |
| `TileForge/Game/GameState.cs` | **MODIFIED** — Version bumped to 2 |
| `TileForge/Game/GameStateManager.cs` | **MODIFIED** — Equip/unequip methods, effective stats |
| `TileForge/Game/Screens/GameplayScreen.cs` | **MODIFIED** — Effective stats in combat, HUD ATK/DEF |
| `TileForge/Game/Screens/InventoryScreen.cs` | **MODIFIED** — Equipment slot UI, equip/unequip |
| `TileForge/UI/GroupEditor.cs` | **MODIFIED** — Item preset expanded, equip_slot dropdown |
| `TileForge.Tests/Game/EquipmentSlotTests.cs` | **NEW** — 11 tests |
| `TileForge.Tests/Game/EquipmentManagerTests.cs` | **NEW** — 20 tests |
| `TileForge.Tests/Game/EquipmentCombatTests.cs` | **NEW** — 6 tests |
| `TileForge.Tests/Game/EquipmentInventoryTests.cs` | **NEW** — 14 tests |
| `TileForge.Tests/Game/EquipmentExportTests.cs` | **NEW** — 4 tests |

---

### Form Layout & Readability Overhaul
Addressed severe text overflow, label truncation, and field crowding in the modal editors. Added three new DojoUI components and refactored both DialogueEditor and QuestEditor. **1050 → 1067 tests.**

#### Problems Solved
- **Label truncation** — "Speaker:" → "Speake", "Requires:" → "Requir", "SetVar:" → "SetVa" caused by hardcoded pixel offsets for label positioning. Labels now measured via `font.MeasureString()`.
- **Field crowding** — DialogueEditor choice rows crammed Next + Req + Flag + Del into one row (~70px per field). Nodes go from 4 cramped rows to 6 full-width rows; choices go from 2 rows to 3.
- **No content clipping** — Content drew outside panel bounds when scrolled. ScrollPanel adds scissor-rect clipping.
- **No scroll indicator** — Both editors had mouse-wheel scrolling but no visual feedback. ScrollPanel adds a 6px proportional thumb scroll bar.
- **No overflow feedback** — Long field values were invisible beyond the field edge. Hover tooltip shows full text after 0.4s delay.

#### New DojoUI Components
- **`FormLayout`** (struct) — Immediate-mode layout helper tracking a cursor Y position. Standard row methods (`DrawLabeledField`, `DrawTwoFieldRow`, `DrawLabeledDropdown`, `DrawLabeledNumeric`, `DrawLabeledCheckbox`, `DrawSectionHeader`, `DrawSeparator`, `Space`). `DrawTwoFieldRow` measures both labels from font metrics rather than hardcoded offsets — the core fix for truncation.
- **`ScrollPanel`** (class) — Begin/End pattern for scissor-clipped scrollable content regions. Mouse wheel scrolling, visual scroll bar (6px track, proportional thumb min 20px, hover highlight). Modeled after FileBrowserDialog's scroll bar implementation.
- **`TextInputField.IsTextOverflowing()`** — Returns true when text exceeds visible field width. Used by editors to trigger tooltip display.

#### Editor Changes
- **DialogueEditor** — Nodes: Id, Speaker, Text, Next each get full-width rows. Requires + Sets Flag share a measured two-field row. Sets Var gets its own row. Choices: Text full-width, Next + Requires measured two-field, Sets Flag + Del on third row. Modal default size 760×650 (was 700×600).
- **QuestEditor** — Same treatment: ScrollPanel, FormLayout for top-level fields, measured label widths for objective rows. Modal default 660×500 (was 600×500).
- **Both editors** — Resizable modals (drag right/bottom/corner edges, min 500×400, max fills screen). Resize grip dots in bottom-right corner. Overflow tooltips for all TextInputFields.

#### Key Design Decisions
- **FormLayout is a struct, not class.** Created on the stack each Draw frame. Zero allocation, immediate-mode — no retained widget tree.
- **ScrollPanel owns scissor state.** BeginScroll saves/sets scissor rect; EndScroll restores and draws scroll bar. Nests correctly with TextInputField's internal scissor clipping (field bounds are always a subset of viewport).
- **Draw order constraint:** `BeginScroll → content → EndScroll → dropdown popups → tooltips`. Popups and tooltips drawn after scissor restore to avoid being clipped.
- **Resize multiplies delta by 2** since the panel is centered — dragging one edge right effectively grows the panel from both sides.

#### New/Modified Files
| File | Action |
|------|--------|
| `DojoUI/FormLayout.cs` | **NEW** — Immediate-mode form layout helper |
| `DojoUI/ScrollPanel.cs` | **NEW** — Scroll state + visual scroll bar |
| `DojoUI/TextInputField.cs` | **MODIFIED** — Added `IsTextOverflowing()` |
| `TileForge/LayoutConstants.cs` | **MODIFIED** — Form/ScrollBar/Modal constants |
| `TileForge/UI/DialogueEditor.cs` | **MODIFIED** — Full layout refactor |
| `TileForge/UI/QuestEditor.cs` | **MODIFIED** — Full layout refactor |
| `TileForge.Tests/DojoUI/FormLayoutTests.cs` | **NEW** — Layout computation tests |
| `TileForge.Tests/DojoUI/ScrollPanelTests.cs` | **NEW** — Scroll state tests |

---

### Cleanup Phase — Code Quality & Dead Code Removal

Comprehensive audit and cleanup pass across the codebase.

#### Bugs Fixed
- **7 test failures:** 3 path-separator tests hardcoded Unix `/` (now use `Path.Combine` for cross-platform), 4 AutoSaveManager tests never set `Enabled = true`
- **O(n²) SyncEntityRenderState:** `GameplayScreen` ran nested `foreach` every frame to sync entity positions. Replaced with `Dictionary<string, EntityInstance>` for O(1) lookups.
- **QuestLogScreen scroll was dead code:** `_scrollOffset` field was updated on input but never read in `Draw()`. Now properly offsets rendering.
- **FileBrowserDialog SetFilenameText workaround:** Was clearing text with a backspace loop + retyping each char. `TextInputField.SetText()` already existed — replaced with a direct call.

#### Abstractions Extracted
- **GameMenuList struct** (`TileForge/Game/GameMenuList.cs`): Shared cursor/scroll navigation for all 6 game screens. Replaced duplicated `_selectedIndex` wrapping and `_scrollOffset` clamping logic across PauseScreen, GameOverScreen, SaveLoadScreen, SettingsScreen, InventoryScreen, QuestLogScreen. 31 new tests.
- **ModalResizeHandler struct** (`TileForge/UI/ModalResizeHandler.cs`): Shared resize-drag logic for modal editors. Eliminated ~110 lines of verbatim copy-paste between DialogueEditor and QuestEditor (7 state fields, ResizeEdge enum, HandleResize, DetectResizeEdge, DrawResizeGrip, ComputePanelRect).

#### GC Allocation Reductions
- `GameplayScreen`: static `JsonSerializerOptions` (was `new` per dialogue load)
- `GameStateManager.ProcessStatusEffects()`: field-level reusable list with `.Clear()`
- `QuestManager.CheckForUpdates()`: field-level buffer, returns copy only when non-empty, static empty list for common case
- `QuestLogScreen`: field-level `_activeQuests`/`_completedQuests` lists
- `InventoryScreen`: `_cachedMenuItems` built once in `Update()`, reused in `Draw()`

#### Dead Code Removed
- **Toolbar.cs, ToolPanel.cs** — Old editor UI replaced by ToolbarRibbon; neither class was instantiated anywhere.
- **TileRegistry.cs, EntityRegistry.cs** + their test files (27 tests) — Early abstractions superseded by the `groupsByName` dictionary pattern in `GameStateManager`. Neither class was referenced outside its own file and tests. If registry-style lookups are needed in the future, the `groupsByName` dictionaries already in `GameStateManager.Initialize()` and `SwitchMap()` serve this purpose.

#### Stale Artifacts Cleaned
- DEVLOG.md title fixed from "TileForge3" to "TileForge"; structure diagram updated
- LayoutConstants.cs: "legacy — kept for reference" comment corrected to "shared by ToolbarRibbon"
- ToolbarRibbon.cs: removed stale references to deleted Toolbar.cs/ToolPanel.cs in comments
- EntityInstance.cs: updated comment from "TileGroup/EntityRegistry" to "TileGroup name"
- Documentation baselines corrected (test count, Screens path in PRD)

#### Test Impact
- Started: 1158 tests, 7 failures
- After fixes + new GameMenuList tests: 1189 tests, 0 failures
- After removing 27 dead registry tests: **1162 tests, 0 failures**

---

### G10 — Visual Dialogue Tree Editor
Replaced form-based `DialogueEditor` with `DialogueTreeEditor` — a split-pane visual node-graph editor for dialogue authoring. **1162 → 1198 tests.**

Notable design decisions:
- **Split-pane modal.** Left 60% is a pannable/zoomable canvas showing dialogue nodes as rectangles with connection ports. Right 40% is a FormLayout-based properties panel for the selected node. This keeps the familiar form editing (from G8++) while adding visual structure overview.
- **EditorX/EditorY on DialogueNode.** Nullable int properties for layout persistence. Omitted from JSON when null via existing `WhenWritingNull` serialization policy. No separate layout file — position data lives alongside the dialogue data, consistent with the property-bag philosophy.
- **Deep copy on edit.** `ForExistingDialogue` creates a deep copy of the input `DialogueData`. Edits modify the copy; the original is untouched until the user saves. Cancel discards the copy.
- **NodeGraphCamera for float-precision zoom.** The existing `Camera` class uses integer zoom levels for pixel-art grids. Node graphs need smooth 0.25×–3.0× zoom. New lightweight class (~40 lines) with stable-center zoom-to-cursor.
- **Auto-layout via BFS.** `DialogueAutoLayout` traverses from root node (or "start" if present) via BFS, assigns column (depth) and row (index within column). Orphan nodes placed in a trailing column. Runs automatically on first edit of unpositioned dialogues and on-demand via header button.
- **DrawLine via rotated pixel texture.** Standard MonoGame technique: rotate the 1×1 white texture by `atan2(dy, dx)` and scale to `(length, thickness)`. `DrawBezier` approximates cubic Bezier with 16 line segments. Both added to `Renderer.cs`.
- **Canvas interaction model.** Middle-drag = pan, scroll = zoom, left-click = select, left-drag on node = move, left-drag from output port = connect, right-click = context menu. Context menus reuse existing `ContextMenu` class. Hit-testing operates in world space via `CanvasScreenToWorld` helper.
- **Continuous flush.** Properties panel values are flushed to the data model every frame so the canvas always reflects current edits (node labels update as user types).
- **ConnectionRenderer is standalone utility.** Created as a reusable static class for Bezier connections, though the editor draws connections inline with canvas-origin-aware coordinate transforms.

New files:
| File | Purpose |
|------|---------|
| `TileForge/UI/DialogueTreeEditor.cs` | Visual node-graph dialogue editor modal |
| `TileForge/UI/NodeGraphCamera.cs` | Float-precision pan/zoom camera |
| `TileForge/UI/DialogueAutoLayout.cs` | BFS auto-layout for node positions |
| `TileForge/UI/DialogueNodeWidget.cs` | Node bounds, ports, hit-testing |
| `TileForge/UI/ConnectionRenderer.cs` | Bezier connection rendering utility |
| `DojoUI/Renderer.cs` | Added `DrawLine` + `DrawBezier` |
| `TileForge/Game/DialogueData.cs` | Added `EditorX`/`EditorY` to `DialogueNode` |
| `TileForge/LayoutConstants.cs` | Added 20+ node graph color/size constants |
| `TileForge/TileForgeGame.cs` | Swapped `DialogueEditor` → `DialogueTreeEditor` |

---

### G11 — Multimap Projects
Single-project, multi-map architecture. Projects now contain multiple named maps sharing a common spritesheet, TileGroups, quests, and dialogues. Tab bar UI for switching between maps. In-project map transitions during play mode. **1198 → 1266 tests.**

Notable design decisions:
- **Facade pattern for backward compatibility.** `EditorState.Map`, `UndoStack`, `ActiveLayerName`, `SelectedEntityId`, `TileSelection` all delegate to the active `MapDocumentState`. Every existing call site (`MapCanvas`, `MapPanel`, tools, commands) continues working unchanged — zero migration required for existing code.
- **Auto-create pattern.** The `Map` setter auto-creates a `MapDocumentState` when none exists and value is non-null. This ensures backward compatibility with tests and legacy code that sets `state.Map = new MapData(...)` directly.
- **Fallback fields.** `_fallbackActiveLayerName`, `_fallbackSelectedEntityId`, `_fallbackTileSelection` hold values when no MapDocument is active. Events fire correctly in both scenarios (with and without active document).
- **UndoStack rewiring.** On tab switch, `StateChanged` event is unwired from the old document's UndoStack and wired to the new one. A `_fallbackUndoStack` handles the no-document case.
- **Project file V2.** `ProjectData.Maps` (List<MapDocumentData>) replaces the V1 single `Map` + `Entities` fields. V1 files auto-upgrade on load (single map wrapped into one MapDocumentState). V2 saves set V1 fields to null (omitted by `WhenWritingNull`).
- **Pre-exported project maps.** `PlayModeController.Enter()` exports all project maps to an in-memory `Dictionary<string, LoadedMap>` via `MapExporter.ExportJson()` + `MapLoader.Load()`. `ExecuteMapTransition()` checks this dictionary first — instant in-project transitions with no filesystem I/O. Falls back to filesystem for cross-project/external maps.
- **Signal-based tab bar.** `MapTabBar` uses the same signal pattern as `ToolbarRibbon` and `MapPanel` — `WantsSelectTab`, `WantsNewMap`, `WantsCloseTab`, `WantsRenameTab`, `WantsDuplicateTab`. `TileForgeGame` consumes signals in `HandleMapTabBarActions()`.
- **Group operations propagate.** `RemoveGroup()` and `RenameGroup()` iterate all `MapDocuments` instead of just the active `Map`, updating cell references and entity GroupNames across every map.
- **ProjectContext returns project map names.** `GetAvailableMaps()` returns `MapDocuments.Select(d => d.Name)` instead of scanning the filesystem, so `target_map` BrowseDropdown in GroupEditor shows project maps directly.
- **CRUD on ProjectManager.** `CreateNewMap`, `DeleteMap`, `RenameMap`, `DuplicateMap` with unique name enforcement and `target_map` reference updates on rename.

Key bug fixed during development:
- **106 test failures after EditorState refactor.** Tests set `state.Map = new MapData(...)` directly, but the new `Map` setter delegated to `ActiveMapDocument` which was null. Fixed by adding auto-create logic — when no MapDocumentState exists and value is non-null, auto-creates one named "main".
- **2 additional test failures.** `ActiveLayerName` and `SelectedEntityId` setters didn't fire events when no MapDocument existed. Fixed by adding fallback fields that hold values and fire events regardless of active document state.

New/Modified files:
| File | Action |
|------|--------|
| `TileForge/Editor/MapDocumentState.cs` | **NEW** — Per-map state container |
| `TileForge/Editor/EditorState.cs` | **MODIFIED** — MapDocuments, ActiveMapIndex, facade properties |
| `TileForge/Data/ProjectFile.cs` | **MODIFIED** — V2 format with Maps list, backward-compatible load |
| `TileForge/ProjectManager.cs` | **MODIFIED** — Multimap load/save, CRUD operations |
| `TileForge/UI/MapTabBar.cs` | **NEW** — Tab bar UI component |
| `TileForge/UI/IProjectContext.cs` | **MODIFIED** — GetAvailableMaps returns project map names |
| `TileForge/UI/StatusBar.cs` | **MODIFIED** — Shows active map name |
| `TileForge/LayoutConstants.cs` | **MODIFIED** — MapTabBar height/colors, TopChromeHeight 54→78 |
| `TileForge/TileForgeGame.cs` | **MODIFIED** — Tab bar integration, signal wiring |
| `TileForge/PlayModeController.cs` | **MODIFIED** — In-project map transitions |
| `TileForge.Tests/Editor/MultimapTests.cs` | **NEW** — 68 tests (MapDocumentState, EditorState multimap, ProjectFile V2) |