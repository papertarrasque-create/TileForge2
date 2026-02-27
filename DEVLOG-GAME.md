# TileForge — Game State Development Log

---

## Architecture Decisions (Established G1–G6)

1. **TileGroup gets richer, not replaced.** Gameplay properties live directly on TileGroup. One source of truth.
2. **Editor UI for all data authoring.** No hand-edited JSON. GroupEditor, DialogueEditor, QuestEditor expose everything.
3. **Evolve PlayModeController in place.** Play mode grows inside the editor. Delegates to ScreenManager + GameStateManager.
4. **ScreenManager is play-mode-only.** Editor UI (PanelDock, DialogManager, InputRouter) is not wrapped in GameScreen.
5. **Flag-based entity persistence.** State lives in `GameState.Flags` (e.g., `entity_inactive:{id}`). Entities are stateless in map files.
6. **ItemPropertyCache for cross-map inventory.** Entity properties cached at collection time so item-use works after map transitions.
7. **Property bags everywhere.** `Dictionary<string, string>` provides extensibility without class proliferation.
8. **Status effects are step-based.** Tick per player movement, not real-time. Deterministic and save-friendly.

---

## Phase Summary

| Phase | Key Work | Tests |
|-------|----------|-------|
| G1 | Data model, registries, GroupEditor extensions | 494 |
| G2 | GameState/PlayerState/GameStateManager | 537 |
| G3 | GameInputManager, ScreenManager, GameplayScreen | 574 |
| G4 | MapLoader, triggers, flag persistence, SaveManager | 631 |
| G5 | SaveLoadScreen, InventoryScreen, DialogueScreen, SettingsScreen | 722 |
| G6 | StatusEffect runtime, GameOverScreen, HUD | 771 |
| G7 | Bump combat, EntityAI, IPathfinder, damage flash | 870 |
| G8 | Quest system, entity hooks, QuestLogScreen | 925 |
| G8+ | Quest Editor UI (QuestPanel + QuestEditor) | 956 |
| UI Overhaul | MenuBar, ToolbarRibbon, smart GroupEditor, DojoUI widgets | 1031 |
| G8++ | Dialogue Editor UI (DialoguePanel + DialogueEditor) | 1050 |
| Form Layout | FormLayout, ScrollPanel, resizable modals, overflow tooltips | 1067 |
| G9 | Equipment (weapon/armor/accessory), effective stats | 1158 |
| Cleanup | Dead code removal, GameMenuList, ModalResizeHandler, GC fixes | 1162 |
| G10 | Visual Dialogue Tree (node-graph, BFS auto-layout) | 1198 |
| G11 | Multimap projects (MapDocumentState, MapTabBar, V2 format) | 1266 |
| G12 | World Map Editor (WorldLayout grid, EdgeTransitionResolver) | 1371 |
| G13 | AP Combat + Floating Messages (2 AP/turn, entity speed, auto-end-turn) | 1443 |

All phases: 0 failures, 0 regressions.

---

## Key Design Decisions by Phase

### G7 — Combat & AI
- **Bump combat separated from interaction.** `TryBumpAttack` runs before `CheckEntityInteractionAt` — attackable NPCs attacked, friendly NPCs show dialogue.
- **Entity turn fires from two paths.** After player move completion and after bump attacks. Both call `ExecuteEntityTurn`.
- **AI is pure and static.** `EntityAI.DecideAction` — state in, action out. Only side effect: writing patrol tracking properties.
- **Instant enemy movement.** No lerp animation yet. EntityAction struct captures everything needed for future animation.
- **Key bug:** Player position wasn't synced to GameState — enemies chased spawn point. Fixed by syncing `State.Player.X/Y` before `ExecuteEntityTurn`.

### G8 — Quests
- **QuestManager is a polling evaluator.** `CheckForUpdates()` called after state-changing actions. No event bus — quest state derives from flags/variables.
- **Entity properties drive tracking.** `on_kill_set_flag`/`on_kill_increment` processed inline. Data-driven, consistent with property bag philosophy.
- **Auto-completion, no turn-in.** "Return to NPC" modeled as a flag objective set by dialogue.

### G8+ — Quest Editor
- **Two-tier UI (QuestPanel + QuestEditor)** matches GroupEditor pattern. Same signal pattern.
- **quests.json stays external** to `.tileforge` file — game content, not editor state.

### G8++ — Dialogue Editor
- **Per-file I/O** (`dialogues/{id}.json`) rather than aggregate file.
- **CamelCase JSON** for dialogues (matching existing format) vs snake_case for quests.
- **Two-level nesting** in editor: nodes -> choices.

### G9 — Equipment
- **Effective stats computed, not stored.** `GetEffectiveAttack()`/`GetEffectiveDefense()` sum base + equipment bonuses via `ItemPropertyCache`.
- **Equipment properties are just entity properties.** `equip_slot`/`equip_attack`/`equip_defense` in property bag — no MapExporter/MapLoader changes.
- **GameState.Version bumped to 2.** Backward compatible — v1 saves get empty Equipment dict.

### G10 — Visual Dialogue Tree
- **Split-pane modal.** Canvas (60%) + FormLayout properties panel (40%). Visual overview + familiar form editing.
- **EditorX/EditorY on DialogueNode.** Nullable ints, omitted from JSON when null. Position lives with data.
- **NodeGraphCamera** for float-precision zoom (0.25x-3.0x). Separate from editor's integer-zoom Camera.
- **Deep copy on edit.** Edits modify copy; original untouched until save.

### G11 — Multimap
- **Facade pattern.** `EditorState.Map`/`UndoStack`/etc. delegate to active `MapDocumentState` — zero migration for existing code.
- **ProjectFile V2.** `Maps` list replaces single Map. V1 auto-upgrades on load.
- **Pre-exported project maps.** `PlayModeController.Enter()` exports all maps to in-memory dict — instant transitions, no filesystem I/O.

### UI Overhaul
- **IProjectContext** scans filesystem for maps/dialogues and project data for flags/variables. Enables "Create New..." flow.
- **FormLayout is a struct.** Zero allocation, immediate-mode. Created on stack each Draw frame.
- **ScrollPanel owns scissor state.** Draw order: `BeginScroll -> content -> EndScroll -> popups -> tooltips`.

### G12 — World Map Editor
- **Grid-based adjacency.** Maps placed on 2D grid — N/S/E/W neighbors derived from position. No manual neighbor linking.
- **EdgeTransitionResolver** resolves direction + neighbor + spawn position at runtime. Custom `EdgeSpawn` overrides per direction.
- **Custom exit points** (`NorthExit`/`SouthExit`/`EastExit`/`WestExit`) define portal-style transitions at arbitrary map positions, coexisting with edge-of-map transitions.
- **WorldLayout stored in ProjectFile.ProjectData**, null when unconfigured — backward compatible with existing projects.

### G13 — AP Combat + Floating Messages
- **Action Points replace simple bump-and-wait.** Player gets 2 AP per turn (configurable via `PlayerState.MaxAP`). Move = 1 AP, bump attack = 1 AP. Enables move+attack, double-move, attack+retreat combos.
- **Auto-end-turn preserves exploration feel.** When no hostile entity with a `behavior` property is within its `aggro_range`, the turn ends immediately after each action. Player never notices the AP system during exploration — it only surfaces in combat.
- **Directional attack via Interact key (Z).** Attacks the facing tile without moving. Friendly interactions (dialogue, NPC talk) remain free (0 AP).
- **EndTurn action (Space).** Forfeit remaining AP, entities act. Added to `GameAction` enum — SettingsScreen auto-enumerates for rebinding.
- **Entity speed property (1-3).** Each entity gets `speed`-based AP per turn. Speed-2 enemies can move twice or move+attack. `EntityAI.DecideAction` called per entity AP — stateless, so re-evaluation after each action works naturally.
- **Floating messages replace StatusMessage.** `FloatingMessage` with text, color, tile position, timer, vertical drift. World-space rendering (tile pos x tileSize x zoom + camera), alpha fade, ~1.0s lifetime. Solves overwritten messages during multi-hit combat.
- **GamePlayContext struct** bundles all GameplayScreen dependencies — cleaner constructor, easier testing.
- **IDialogueLoader/IPathResolver abstractions** extract filesystem access for testability.
- **Facing direction fix.** Extended from horizontal-only to all 4 directions — prerequisite for directional attack.
- **Save-load backward compat.** `PlayerState.MaxAP` defaults to 2. `LoadState()` fixes zero-MaxAP from old saves. `PlayState.PlayerAP`/`IsPlayerTurn` are ephemeral (not serialized).
- **Equipment-modifiable AP.** `equip_ap` property on items, `GetEffectiveMaxAP()` sums base + equipment bonuses.

### Cleanup
- **O(n^2) SyncEntityRenderState** replaced with `Dictionary<string, EntityInstance>` for O(1) lookups.
- **GameMenuList struct** shared cursor/scroll navigation across 6 game screens. Replaced duplicated logic.
- **ModalResizeHandler struct** eliminated ~110 lines of copy-paste between editors.
- **TileRegistry/EntityRegistry removed** — superseded by `groupsByName` dictionaries in GameStateManager.
