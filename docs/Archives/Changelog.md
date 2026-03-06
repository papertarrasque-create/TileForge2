---
updated: 2026-03-06
status: current
---

# TileForge -- Changelog

Reconstructed from git history, PRD documents, and development logs. Focuses on architectural shifts and feature milestones.

---

## 2026-03-06 -- Sidebar HUD + Text Overflow Fixes

- **Sidebar HUD** -- Retro CRPG sidebar (280px, right side) with player stats, equipment, inventory, and scrollable word-wrapped message log. Inspired by Caves of Qud and SKALD. `GameLog` captures all combat/dialogue/quest events (200 max). Auto-scroll with manual scroll via mouse wheel.
- **TextUtils.TruncateToFit** -- Shared utility for safe text truncation in sidebar panels and tabs
- **Form label overflow fix** -- Dynamic label width calculation, dropdown popup auto-sizing

## 2026-03-05 -- UI Polish (G15)

- **Tile palette overhaul** -- Improved sprite selection UI
- **Minimap caching** -- `RenderTarget2D` cache, regenerate only on map mutation (was: 7,200-cell scan per frame)
- **Resizable panel dock** -- Edge-drag panel resizing
- **Dialog input fixes** -- Various input handling improvements

## 2026-02-27 -- Tactical Combat (G14)

- **Terrain defense bonuses** -- `TileGroup.DefenseBonus` adds to defender's defense. HUD shows `COVER:+n`.
- **Backstab/flanking** -- `AttackPosition` enum with 4-directional facing. Backstab = 2x, Flank = 1.5x damage.
- **Poise system** -- Regenerating shield buffer (20 base). Damage hits poise first. Poise regenerates when no hostiles nearby. Equipment-modifiable via `equip_poise`.
- **Noise/alertness stealth** -- `TileGroup.NoiseLevel` (silent/normal/loud). Noise propagation alerts dormant entities, doubling their aggro range for 3 turns.
- **1507 tests**

## 2026-02-26 -- World Map + AP Combat (G12-G13)

- **World Map Editor** (G12) -- Visual grid editor for spatial map adjacency. Auto-bidirectional edges from grid position. Custom spawn points per edge. Custom exit points for portal-style interior transitions. `EdgeTransitionResolver` runtime.
- **AP Combat** (G13) -- Action Point system (2 AP/turn). Move + bump attack + directional attack. Auto-end-turn during exploration. Entity speed (1-3 actions/turn). Equipment-modifiable AP.
- **Floating messages** -- Per-entity floating text with color, drift, and alpha fade. Replaces single StatusMessage.
- **Infrastructure** -- `GamePlayContext` struct, `IDialogueLoader`/`IPathResolver` abstractions for testability.
- **1443 tests**

## 2026-02-26 -- Visual Dialogue Tree + Multimap (G10-G11)

- **Visual Dialogue Tree** (G10) -- Split-pane node-graph editor with pannable/zoomable canvas, draggable nodes, Bezier connection lines, right-click context menu, BFS auto-layout. `NodeGraphCamera` for float-precision zoom. Deep copy on edit.
- **Multimap Projects** (G11) -- `MapDocumentState` per map, `EditorState` facade pattern, `MapTabBar` tab strip, `ProjectFile` V2 format with auto-upgrade from V1. Pre-exported maps for instant play-mode transitions.
- **Cleanup phase** -- Dead code removal (TileRegistry/EntityRegistry), `GameMenuList` struct (shared cursor/scroll across 6 screens), `ModalResizeHandler` struct, O(n^2) -> O(1) SyncEntityRenderState.
- **1266 tests**

## 2026-02-24 -- Equipment System (G9)

- **Equipment** -- Weapon/Armor/Accessory slots. Effective stats computed from base + equipment bonuses via `ItemPropertyCache`. `GameState.Version` bumped to 2.
- **Input event refactor** -- Improved input handling patterns
- **1158 tests**

## 2026-02-24 -- Game Runtime Foundation (G1-G8) + Editors + UI Overhaul

This was a large commit covering the entire game runtime buildout:

- **G1** -- Data model: gameplay properties on TileGroup, EntityType enum, registries, GroupEditor extensions (494 tests)
- **G2** -- GameState/PlayerState/GameStateManager, PlayModeController delegation (537 tests)
- **G3** -- GameInputManager with edge detection, ScreenManager screen stack, GameplayScreen (574 tests)
- **G4** -- MapLoader, trigger-based transitions, flag-based entity persistence, SaveManager (631 tests)
- **G5** -- SaveLoadScreen, InventoryScreen, DialogueScreen, SettingsScreen, StatusEffects (722 tests)
- **G6** -- StatusEffect runtime, GameOverScreen, HUD, DefaultProperties, item-use fix (771 tests)
- **G7** -- Bump combat (Brogue-style), EntityAI (idle/chase/patrol/chase_patrol), IPathfinder interface, damage flash (870 tests)
- **G8** -- Quest system with flag/variable objectives, entity hooks, QuestLogScreen, auto-completion (925 tests)
- **G8+** -- Quest Editor UI: QuestPanel + QuestEditor + QuestFileManager (956 tests)
- **UI Overhaul** -- RPG Maker-inspired MenuBar + ToolbarRibbon, smart GroupEditor with type-aware controls, IProjectContext browse-dropdowns (1031 tests)
- **G8++** -- Dialogue Editor UI: DialoguePanel + DialogueEditor + DialogueFileManager (1050 tests)
- **Form Layout** -- FormLayout struct, ScrollPanel, resizable modals, overflow tooltips (1067 tests)
- **Manual** -- Comprehensive user manual (MANUAL.md)

## 2026-02-22 -- Editor Refactor + Features (R1-R4, P1-P3)

- **R1** -- Constants extraction: 83 magic numbers -> `LayoutConstants.cs` (112 tests)
- **R2** -- God class split: TileForgeGame 922 -> 317 lines + ProjectManager, DialogManager, InputRouter, PlayModeController (199 tests)
- **R3** -- Events + dirty state: 7 events on EditorState, `IsDirty` flag (233 tests)
- **R4** -- ISpriteSheet interface + MockSpriteSheet for testability (286 tests)
- **P1** -- Picker tool, selection tool + copy/paste, map resize (360 tests)
- **P2** -- New project wizard, recent files, auto-save, grid overlay (410 tests)
- **P3** -- Stamp brush, tile palette, JSON/PNG export, minimap (453 tests)
- **Rename** -- TileForge2 -> TileForge (loads both formats, saves as `.tileforge`)

## 2026-02-22 -- Initial Release

- Interactive tile map editor with layers, groups, undo/redo
- Basic play mode (F5 to walk the map)
- Bundled DejaVu Sans Mono font for cross-platform rendering
