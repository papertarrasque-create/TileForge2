# TileForge v2

An interactive map editor and game foundation for top-down 2D games. Paint tiles, place entities, play the map. Built on the lessons of TileForge v1 and TileGrid, but a completely new codebase.

## Stack

C#/MonoGame DesktopGL, .NET 9.0. References DojoUI for shared components. No MapGen dependency until Phase 4.

## Architecture

```
TileForgeGame : Game            — Main loop. Layout routing, project I/O, dialog management, play mode.
├── Toolbar                     — Top bar (28px): global actions (Undo/Redo/Save/Play icons), title.
├── PanelDock                   — Left sidebar (200px): manages collapsible, reorderable panels.
│   ├── ToolPanel               — 2x2 grid of tool buttons with procedural pixel icons.
│   └── MapPanel                — Unified layers+groups: collapsible layer sections, nested group rows,
│                                  visibility toggles, drag reorder, context menu, add buttons.
├── GroupEditor                 — Modal overlay: create/edit groups from spritesheet
│   ├── Camera                  — Pan/zoom for spritesheet (from DojoUI)
│   ├── SpriteSheet             — Loaded texture + grid (from DojoUI)
│   ├── TileAtlas               — Auto-named entries for all grid positions (from DojoUI)
│   └── Selection               — Rectangular sprite selection (from DojoUI)
├── MapCanvas                   — Main workspace: grid rendering, camera, tool dispatch, entity lerp
│   └── Camera                  — Independent pan/zoom for map view (from DojoUI)
├── StatusBar                   — Bottom: coordinates, current group, hints. Player pos in play mode.
├── EditorState                 — Central mutable state: tool, layer, selected group, map, play mode
│   └── Tools/
│       ├── BrushTool           — Click/drag to paint group onto active layer
│       ├── EraserTool          — Click/drag to clear cells
│       ├── FillTool            — BFS flood fill on active layer
│       └── EntityTool          — Click to place entity instances
├── Play/
│   └── PlayState               — Runtime play state: player, lerp, movement, status messages
└── Data/
    ├── TileGroup               — Named sprite collection with type, IsSolid, IsPlayer, LayerName
    ├── MapData                 — Width, Height, layers, entities
    ├── MapLayer                — Name, visible, string[] cells (group name references)
    ├── Entity                  — ID, group name, grid position, properties
    └── ProjectFile             — .tileforge2 JSON save/load (includes panel state)
```

## UI Layout

No tabs. Panels and canvas coexist. Panels are collapsible and reorderable.

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
| (12, 8) grass  Ground  |  Ctrl+Z/Y  Ctrl+S save              |  StatusBar
+--------------------------------------------------------------+
```

The GroupEditor is a modal overlay that appears over the map canvas only when creating or editing a group. The spritesheet view does not compete for space during the core painting workflow.

## Data Model

### TileGroup
A named collection of sprite references (col, row positions in the spritesheet) with a type, properties, and layer assignment:
- `Tile` — paints onto grid layers. Multiple sprites = automatic variation.
- `Entity` — places objects with identity onto the map.
- `IsSolid` — blocks player movement in play mode (applies to both Tile and Entity groups).
- `IsPlayer` — marks an Entity group as the player character (first matching entity becomes the player on F5).
- `LayerName` — which layer this group belongs to. Groups are nested under layers in the Map panel.

Sprites store grid coordinates only (col, row). Pixel rects are derived at runtime from SpriteSheet.

### Map Layers
Flat `string[]` arrays indexed as `[x + y * Width]`. Each cell holds a group name or null. String references survive group reordering and are human-readable in the JSON.

Default layers: Ground, Objects. User can add more.

### Entities
Separate list, not a grid layer. Each entity has an ID, references a group by name, sits at a grid position, and carries a property dictionary for future extensibility. Multiple entities can occupy the same cell.

### Variation Rendering
Multi-sprite tile groups: `(x * 31 + y * 37) % count`. Position-seeded, deterministic, no per-cell storage.

## Project Structure

```
workbench/TileForge2/
├── CLAUDE.md
├── PRD.md
├── PROJECT_MEMORY.md
├── TileForge2.sln / .csproj
├── Program.cs
├── TileForgeGame.cs
├── Data/
│   ├── TileGroup.cs
│   ├── MapData.cs
│   ├── MapLayer.cs
│   ├── Entity.cs
│   └── ProjectFile.cs
├── Editor/
│   ├── EditorState.cs
│   ├── Tools/
│   │   ├── ITool.cs
│   │   ├── BrushTool.cs
│   │   ├── EraserTool.cs
│   │   ├── FillTool.cs
│   │   └── EntityTool.cs
│   ├── Commands/
│   │   ├── ICommand.cs
│   │   ├── CellStrokeCommand.cs
│   │   ├── PlaceEntityCommand.cs
│   │   ├── RemoveEntityCommand.cs
│   │   ├── MoveEntityCommand.cs
│   │   └── ReorderLayerCommand.cs
│   └── UndoStack.cs
├── Play/
│   └── PlayState.cs
├── UI/
│   ├── Panel.cs
│   ├── PanelDock.cs
│   ├── ToolPanel.cs
│   ├── MapPanel.cs
│   ├── MapCanvas.cs
│   ├── GroupEditor.cs
│   ├── Toolbar.cs
│   └── StatusBar.cs
└── Content/
    ├── Content.mgcb
    └── Font.spritefont
```

## Dependencies

```
keepers/DojoUI/       ← Shared UI: Camera, Renderer, SpriteSheet, TileAtlas, Selection, Dialogs, FileBrowserDialog
keepers/mapgen/       ← Phase 4: procedural generation (deferred, not referenced)
sprites/              ← Test spritesheet: The Roguelike 1-15-16.png (16x16 tiles)
```

## Run

```
dotnet run
```

Window opens at 1440x900. Drop a spritesheet PNG or open a .tileforge2 project file.

## Keybinds

### Global
| Key | Action |
|-----|--------|
| Ctrl+S | Save project |
| Ctrl+O | Open file (file browser) |
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| F5 | Toggle play mode |
| Escape | Exit play mode / cancel overlay / deselect / quit |

### Map Canvas (Editor Mode)
| Key | Action |
|-----|--------|
| B | Brush tool |
| E | Eraser tool |
| F | Fill tool |
| N | Entity tool |
| G | Toggle grid overlay |
| V | Toggle active layer visibility |
| Tab | Cycle active layer |
| Shift+Up | Move active layer up in render order |
| Shift+Down | Move active layer down in render order |
| Delete | Remove selected entity |
| Middle drag | Pan |
| Scroll wheel | Zoom |
| Left click/drag | Use active tool |

### Play Mode
| Key | Action |
|-----|--------|
| Arrow keys | Move player (tile-to-tile with lerp) |
| F5 / Escape | Exit play mode |

### GroupEditor
| Key | Action |
|-----|--------|
| S | Toggle Solid |
| P | Toggle Player (Entity type only) |
| T | Toggle Tile/Entity type |
| Enter | Confirm group |
| Escape | Cancel |

## Principles

### The map is the product
Everything in the editor exists to serve the map. The palette is compact. The toolbar is minimal. The spritesheet browser appears only when needed and disappears when done. Screen real estate belongs to the canvas.

### Groups are the vocabulary
The user doesn't think in sprite coordinates. They think in "grass", "wall", "door". Tile groups translate the spritesheet's visual grid into a semantic vocabulary the user paints with. The group is the unit of thought.

### Data serves two masters
Every data structure must work for both the editor (mutable, interactive) and a future game runtime (iterable, queryable). Entities are a list because that's what a game loop iterates. Layers are arrays because that's what a renderer draws. No editor-only formats that need conversion later.

### Strings over indices
Cells reference groups by name, not by index. Human-readable in JSON. Survives reordering. Debuggable with a text editor. At map scale, the performance difference is zero.

### No invisible state
If a tile is painted, you can see it. If a layer is active, the toolbar says so. If an entity exists, it renders. The editor never holds state that isn't visible somewhere on screen.

### Progressive capability
Phase 1 paints tiles. Phase 2 creates groups. Phase 3 adds entities and undo. Phase 4 plugs in generation. Phase 5 lets you play. Each phase is complete on its own — no feature requires a future phase to be useful.

### No black boxes
Every file is readable. Every system is explainable. The project file is JSON you can edit by hand. The data model is flat enough to trace in your head. If it's too complex to explain, it's too complex to ship.
