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
| G9 (equipment) | 1119 | +52 |

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