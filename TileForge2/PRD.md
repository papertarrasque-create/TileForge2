# TileForge v2 — Product Roadmap

## What This Is

An interactive map editor that serves as the foundation of a top-down 2D game. Paint tiles onto a grid, place entities, eventually play the map. Built in C#/MonoGame, using the same DojoUI library and spritesheet as TileForge v1.

TileForge v1 was a configuration tool — assign sprites to generator tile types and hit Generate. v2 is fundamentally different: you paint the map by hand, and procedural generation is an optional power feature that comes later.

## Core Concept: Tile Groups

A **TileGroup** is a named collection of sprites from the spritesheet, belonging to a layer:
- **Tile** groups paint onto grid layers (floors, walls, terrain)
- **Entity** groups place objects with identity (chests, doors, NPCs, player start)
- Each group belongs to a **layer** (`LayerName`). Groups are nested under their layer in the Map panel.

Multi-sprite groups give automatic variation when painting (position-seeded, deterministic). The Tile/Entity type distinction is preserved under the hood for tool behavior (auto-switching between BrushTool and EntityTool) but is not surfaced in the panel UI.

## UI Layout

No tabs. Panels and canvas coexist at all times. Panels are collapsible and reorderable.

```
+--------------------------------------------------------------+
| [<<][>>][Save][Play]                            TileForge2    |  Toolbar (28px)
+---Panel Dock (200px)---+--------------------------------------+
| [v Tools]              |                                      |
| [B] [E] [F] [N]       |              Map Canvas               |
|                        |         (pan/zoom, painting)          |
| [v Map]                |                                      |
| [v] [*] Objects        |                                      |
|   [door]   [chest]     |                                      |
|   [+ Add Group]        |                                      |
| [v] [*] Ground         |                                      |
|   [grass]  [wall]      |                                      |
|   [+ Add Group]        |                                      |
| [+ Add Layer]          |                                      |
+---Panel Dock End-------+--------------------------------------+
| (12,8) grass Ground    |  Ctrl+Z/Y  Ctrl+S save              |  StatusBar
+--------------------------------------------------------------+
```

GroupEditor is a modal overlay — appears over the map canvas when creating/editing a group. Shows spritesheet with camera/zoom, sprite selection, name field, type toggle. Disappears when done.

## Data Model

**Cells reference groups by name** (string), not index. Human-readable JSON, survives group reordering, trivially debuggable.

**Groups belong to layers** via `LayerName`. Selecting a group auto-activates its parent layer. Groups are nested under their layer in the Map panel.

**Entities are a separate list**, not a grid layer. They have identity (ID, properties), can stack, and the list is what a future game runtime iterates. Entity groups appear in the Map panel alongside tile groups — the Tile/Entity distinction is internal.

**Default layers**: Ground, Objects. User can add more.

**Sprite variation**: Multi-sprite tile groups use `(x * 31 + y * 37) % count` — deterministic, no per-cell storage needed.

## File Format (.tileforge2)

```json
{
  "version": 1,
  "spritesheet": { "path": "relative/to/sheet.png", "tileWidth": 16, "tileHeight": 16, "padding": 0 },
  "groups": [
    { "name": "grass", "type": "Tile", "layer": "Ground", "sprites": [{"col": 5, "row": 0}, {"col": 6, "row": 0}] },
    { "name": "wall", "type": "Tile", "layer": "Ground", "isSolid": true, "sprites": [{"col": 0, "row": 1}] },
    { "name": "player", "type": "Entity", "layer": "Objects", "isPlayer": true, "sprites": [{"col": 0, "row": 5}] }
  ],
  "map": {
    "width": 40, "height": 30,
    "entityRenderOrder": 0,
    "layers": [
      { "name": "Ground", "visible": true, "cells": [null, "grass", "wall", ...] }
    ]
  },
  "entities": [
    { "id": "e1", "groupName": "goblin", "x": 12, "y": 8, "properties": {} }
  ],
  "editorState": {
    "activeLayer": "Ground", "cameraX": 320, "cameraY": 240, "zoomIndex": 2,
    "panelOrder": ["Tools", "Map"], "collapsedLayers": []
  }
}
```

## Phased Roadmap

### Phase 1: The Grid Lives — COMPLETE
Minimum viable editor. Drop a PNG, see a grid, paint tiles, save and load.

**Delivered:**
- MonoGame scaffold (1440x900, resizable, PointClamp)
- Data model: TileGroup, MapData, MapLayer, Entity, ProjectFile (.tileforge2 JSON)
- UI: MapCanvas (grid rendering + Camera + tile culling), group palette (precomputed layout for aligned hit testing), Toolbar, StatusBar
- EditorState with tool system (ITool interface, BrushTool, EraserTool)
- Spritesheet loading via file drop + tile size input dialog
- 5 hardcoded test groups (grass, wall, floor, water, door) from Fantasy section
- Position-seeded sprite variation on multi-sprite groups
- Ctrl+S save / .tileforge2 file drop to load
- Pan (middle-mouse), zoom (scroll), grid toggle (G), layer switch (Tab), tool keybinds (B/E)
- Window resize support (backbuffer syncs to actual window size)

**End state**: Drop a PNG, get a grid, paint tiles from groups, save the map.

### Phase 2: Group Creation — COMPLETE
Full interactive group management. Replaced hardcoded test groups with user-driven workflow.

**Delivered:**
- GroupEditor modal overlay: spritesheet view with independent Camera (pan/zoom), Selection (click/shift-click rectangular), TextInputField (group name), type toggle (Tile/Entity, T key or click)
- Two modes: ForNewGroup (blank) and ForExistingGroup (pre-populated name, type, bounding box selection)
- Dynamic palette: "+ New Group" button, double-click to edit, right-click context menu (Edit/Delete)
- Delete confirmation via ConfirmDialog; RemoveGroup clears all map cell references and entities
- RenameGroup updates all map cell and entity references across all layers
- Multiple layers: Ground + Objects default, user can add via "+ Layer" toolbar button (InputDialog for name)
- Layer visibility toggle (V key), indicator in toolbar shows "(hidden)" state
- Ctrl+O to open files by path (PNG or .tileforge2), complements drag-and-drop
- Removed hardcoded test groups — editor starts blank, user builds their own vocabulary
- Name collision guard on group creation (auto-suffix)

**End state**: Complete self-service workflow from blank project to multi-layer painted map.

### Phase 3: Entities and Undo — COMPLETE
Editor becomes solid and trustworthy. Every editing action is reversible.

**Delivered:**
- Undo/redo system: ICommand interface, UndoStack (dual-stack), Ctrl+Z / Ctrl+Y
- CellStrokeCommand: unified command for brush, eraser, and fill (stroke-based — one undo entry per press-drag-release)
- EntityTool (N keybind): place Entity-type groups on map, click to select (cyan outline), drag to move, Delete key to remove
- PlaceEntityCommand, RemoveEntityCommand, MoveEntityCommand — all undoable
- Auto tool switching: selecting an Entity-type group switches to EntityTool, selecting a Tile-type group switches back to BrushTool
- BrushTool guards against Entity-type groups (no accidental tile-painting with entities)
- FillTool (F keybind): BFS flood fill on active layer, green-tinted preview, works with Tile-type groups only
- Entity render ordering: EntityRenderOrder on MapData (default 0) controls where entities sit in the layer stack — renders after the layer at that index. Entities appear above floor, below overlay layers.
- Toolbar expanded to four tools: Brush, Eraser, Fill, Entity
- StatusBar updated with all keybinds

**Known deferred:** GroupEditor type toggle (T key) doesn't work when name field is focused during new group creation. Clickable Tile/Entity buttons work as workaround.

**End state**: Full editing toolkit with undo safety net. Entities respect layer ordering.

### Phase 4: MapGen Integration — DEFERRED
Procedural generation as an optional power feature. Deferred to focus on play mode first.

- Add MapGen project reference
- Generate dialog: theme/generator picker, seed display
- Import mapping: MapGen.Tile -> TileGroup name
- Generated map fills Ground layer, then manually edit on top
- Seed bookmarks

### Phase 5: Play Mode Foundation — COMPLETE
The editor becomes a game. Press F5 and you're playing your map.

**Delivered:**
- Group properties: `IsSolid` (blocks movement) and `IsPlayer` (designates player entity) on TileGroup, toggled in GroupEditor (S/P keys or click), serialized in .tileforge2 JSON with nullable bools (omitted when false)
- Group badges: "S" for solid, "P" for player (shown in Map panel alongside group names)
- PlayState runtime class (`Play/PlayState.cs`): tracks player entity, lerp position, movement state, status messages with timer
- F5 keybind toggles play mode; Escape also exits
- Player movement: smooth tile-to-tile lerp (~150ms), arrow keys
- Collision: checks all layers for solid groups at target cell + solid entities (excluding player). Map edges block.
- Entity interaction: walking into a non-solid entity shows "Interacted with {name}"; bumping a solid entity shows "Blocked by {name}"
- Camera follows player (centered on canvas), editor camera state saved/restored on mode toggle
- Play mode UI: toolbar shows "PLAY MODE - F5 to return", panels hide (canvas fills width), status bar shows player position + hints
- FileBrowserDialog (`keepers/DojoUI/FileBrowserDialog.cs`): visual directory navigation for Ctrl+O and Ctrl+S (first save). Keyboard nav (arrows, Enter, Backspace to go up), mouse click/double-click, scroll wheel, file type filtering, Open and Save modes. Click-to-focus between file list and filename input — clicking the list focuses navigation (Enter navigates directories), clicking the input focuses save (Enter saves). Double-clicking a directory always navigates regardless of input focus.

**End state**: Paint a map, mark walls as solid, mark a group as player, place it, press F5, and play. Walk around, hit walls, interact with entities, press F5 to return to editing.

### Phase 6: UI Refinement — COMPLETE
The editor feels like a proper application. Panels, graphical buttons, visible layer management.

**Delivered:**
- Panel system: `Panel` base class (collapsible, header with arrow icon), `PanelDock` manager (200px left sidebar, height distribution, drag-to-reorder panels)
- `ToolPanel`: 2x2 grid of tool buttons with procedural pixel-art icons (Brush, Eraser, Fill, Entity) drawn via composed rectangles. Active tool highlighted.
- `MapPanel`: unified layers+groups panel. Each layer is a collapsible section with nested group rows, visibility toggle, and `+ Add Group` button. Groups belong to a layer (TileGroup.LayerName). Selecting a group auto-activates its parent layer. Entity/Tile type distinction hidden from UI (no T/E badges) but preserved under the hood for auto-tool-switching. Layer drag-to-reorder. Group context menu (Edit/Delete). `+ Add Layer` button at bottom.
- Layer reordering: drag layers within MapPanel + Shift+Up/Down keyboard shortcuts. `ReorderLayerCommand` for undo/redo.
- Toolbar simplified to global actions: procedural Undo/Redo/Save/Play icon buttons. Undo/Redo dimmed when unavailable. Play icon toggles to Stop square during play mode.
- Panel collapse/expand: click panel header to toggle. Collapsed panels show only header (24px). Height redistributed among remaining expanded panels.
- Panel reordering: drag panel headers to rearrange order. Panel order, panel collapse, and layer section collapse states persisted in .tileforge2 project file (EditorStateData).
- Backward compatibility: old project files without group Layer field load with all groups assigned to the first layer.
- StatusBar hints updated for new keybinds.
- Scissor-clipped canvas rendering: map canvas and GroupEditor draw in a scissor-clipped SpriteBatch pass so zoomed tiles/spritesheet never overflow into the toolbar, panels, or status bar. UI chrome draws in a separate unclipped pass, always on top.
- GroupEditor header draw-order fix: spritesheet renders before header so the name field, type toggles, property buttons, and hints remain visible at all zoom levels.

**End state**: Two collapsible, reorderable panels (Tools, Map) in a left dock. Groups nested under layers. Graphical tool icons. Toolbar shows global action icons. All UI chrome stays persistently visible regardless of camera state. Professional editor feel.

## Key References

| File | What it provides |
|------|-----------------|
| `keepers/DojoUI/Camera.cs` | Pan/zoom with ZoomIndex save/restore |
| `keepers/DojoUI/SpriteSheet.cs` | Texture loading + grid math |
| `keepers/DojoUI/Renderer.cs` | DrawRect/DrawRectOutline |
| `keepers/DojoUI/Selection.cs` | Range selection for GroupEditor |
| `keepers/DojoUI/FileBrowserDialog.cs` | Visual file browser (Open/Save modes) |
| `keepers/TileForge/TileForgeGame.cs` | Game class pattern (Init, FileDrop, dialogs, Update/Draw) |
| `keepers/TileForge/MapCanvas.cs` | Grid rendering with tile culling and position-seeded variation |
| `keepers/TileForge/ProjectFile.cs` | JSON serialization (System.Text.Json, relative paths) |
| `sprites/The Roguelike 1-15-16.png` | Test spritesheet (1744x752, 16x16 tiles) |
