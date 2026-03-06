---
updated: 2026-03-06
status: current
---

# Editor Overview

TileForgeGame.cs is the thin orchestrator that manages the editor UI, play mode, and the main game loop.

## UI Regions

```
+--MenuBar (22px)--------------------------------------------+
+--ToolbarRibbon (32px)--------------------------------------+
+--MapTabBar (22px)------------------------------------------+
|              |                                              |
|  PanelDock   |          MapCanvas                           |
|  (sidebar)   |          (center)                            |
|              |                             [Minimap]        |
|  - MapPanel  |                                              |
|  - Palette   |                                              |
|  - Quests    |                                              |
|  - Dialogues |                                              |
|              |                                              |
+--StatusBar-------------------------------------------------+
```

**Play mode** replaces the full editor with the game viewport + a minimal 32px ribbon (Stop button only) + [[Sidebar HUD]] on the right.

## Update Priority Chain

`TileForgeGame.Update()` uses early-return priority -- first match wins:

1. **DialogManager** -- Active modal dialog (file browser, confirm, input)
2. **QuestEditor** -- Quest authoring overlay
3. **DialogueTreeEditor** -- Dialogue node-graph overlay
4. **WorldMapEditor** -- World map grid overlay
5. **GroupEditor** -- Tile/entity property editor
6. **InputRouter** -- Keyboard shortcuts, tool switching
7. **PlayModeController** -- Play mode update (if active)
8. **Editor** -- Map canvas, panel dock, tools

This cascading if/else is a known coupling point (see [[Architecture]]).

## Draw Pipeline

Two-pass rendering:

**Pass 1 (Scissor-clipped canvas):**
- MapCanvas (tiles, entities, grid overlay)
- Selection overlay
- Minimap

**Pass 2 (UI chrome, no scissor):**
- PanelDock (sidebar panels)
- MapTabBar
- MenuBar + ToolbarRibbon
- StatusBar
- Modal overlays (editors, dialogs)

In play mode, the [[Sidebar HUD]] draws in Pass 2 outside the game viewport scissor rect.

## Key Components

### MenuBar + ToolbarRibbon

RPG Maker-inspired UI replacing the old Toolbar + ToolPanel:
- **MenuBar** (22px) -- 6 menus with hotkey hints (File, Edit, View, Map, Tools, Help)
- **ToolbarRibbon** (32px) -- Icon groups with tooltips (tool buttons, play/stop)
- **MenuActionDispatcher** -- Routes menu selections to actions
- **EditorMenus** -- Menu definitions + index constants

### PanelDock (Sidebar)

Resizable sidebar containing:
- **MapPanel** -- Layer management, group list, entity list
- **TilePalettePanel** -- Spritesheet grid, click-to-select, double-click-to-edit
- **QuestPanel** -- Quest list with CRUD (see [[Quest Editor]])
- **DialoguePanel** -- Dialogue file list with CRUD

Panels are collapsible. Order configurable. Edge-drag resizing.

### InputRouter

Handles keyboard shortcuts:
- Tool switching (B/E/F/N/I/M/G)
- File operations (Ctrl+S/O/N)
- Edit operations (Ctrl+Z/Y/C/V)
- View toggles (Ctrl+M minimap, V visibility, Tab layer cycle)
- Play mode (F5)

Skips editor keybinds during play mode.

## Play Mode Entry/Exit

`PlayModeController` mediates the editor/play boundary:

### Enter (F5)

1. Find player entity (group with `IsPlayer = true`)
2. Save editor state (camera, zoom, map documents, groups)
3. Pre-export **all** project maps to in-memory JSON -> LoadedMap dict
4. Build `EdgeTransitionResolver` from WorldLayout
5. Initialize game runtime (GameStateManager, GameInputManager, ScreenManager, SaveManager)
6. Create [[Sidebar HUD]] + GameLog
7. Push GameplayScreen onto ScreenManager
8. Set `IsPlayMode = true`

### Exit (F5 or Escape)

1. Clear screen manager
2. Restore camera, zoom, editor state (multimap-aware)
3. Null out all game managers
4. Set `IsPlayMode = false`

## EditorState

Central facade with 8+ events:

| Event | Trigger |
|-------|---------|
| `ActiveMapChanged` | Map tab switched |
| `ActiveLayerChanged` | Layer selection changed |
| `SelectedGroupChanged` | Group selection changed |
| `ActiveToolChanged` | Tool switched |
| `SelectedEntityChanged` | Entity selected/deselected |
| `PlayModeChanged` | Enter/exit play mode |
| `MapDirtied` | Any map modification |
| `UndoRedoStateChanged` | Undo/redo stack changed |
| `QuestsChanged` | Quest data modified |
| `DialoguesChanged` | Dialogue data modified |

In multimap mode, EditorState is a facade over `MapDocumentState` -- `Map`, `UndoStack`, etc. delegate to the active document.

## IProjectContext

Provides project-level data for browse-dropdowns in the [[Group Editor]]:

| Method | Returns |
|--------|---------|
| `GetAvailableMaps()` | Map names + "+ Create New..." |
| `GetAvailableDialogues()` | Dialogue JSON file names from `dialogues/` |
| `GetKnownFlags(quests, groups)` | Flags from quests + entity properties |
| `GetKnownVariables(quests, groups)` | Variables from quest objectives + entity hooks |

The "+ Create New..." flow: user selects it -> signal set -> parent shows create dialog -> refreshes dropdown on success.

## Related

- [[Architecture]] -- System-level view
- [[Group Editor]] -- Property editing
- [[Map Tab Bar]] -- Multimap tab management
- [[DojoUI]] -- Widget library used by all editors
- [[Controls]] -- Keyboard shortcuts reference
