# TileForge — Product Requirements Document

## Mission
Refine TileForge into a **professional-level tile map editor** for indie top-down 2D RPGs. The focus is on editor UX, code quality, and testability — not adding game engine features.

## Current State
TileForge is a functional tile map editor built on C#/.NET 9/MonoGame 3.8 with:
- Custom immediate-mode UI (no WinForms/WPF)
- Tile painting (brush, eraser, flood fill, stamp)
- Entity placement with type system (Tile vs Entity groups)
- Multi-layer support with visibility toggles
- Command-pattern undo/redo
- JSON project files (`.tileforge`, with `.tileforge2` backward compat)
- Play mode (F5) with collision and entity interaction
- Collapsible/reorderable panel dock
- Minimap, export (JSON/PNG), tile palette, auto-save

---

## UI/Workflow Enhancement Proposals

Benchmarked against industry editors: **Tiled**, **LDtk**, **RPG Maker**, **Aseprite** (for tool UX patterns).

### Priority 1 — Essential Editor Tools

#### 1.1 Eyedropper / Picker Tool (I key)
**Problem:** To paint with a tile visible on the canvas, the user must scroll through the Map panel to find and select the group manually.
**Solution:** Hold Alt (or press I) → click a tile on canvas → its group becomes selected and BrushTool activates. Standard in every tile editor.
**Scope:** New `PickerTool` class, or Alt-modifier on existing tools. ~50 lines.

#### 1.2 Rectangular Selection Tool (M key)
**Problem:** No way to select, move, copy, or delete rectangular regions of tiles.
**Solution:** Selection tool that defines a rectangle on the active layer. Once selected: Ctrl+C copies, Ctrl+V pastes as a floating stamp, Delete clears, arrow keys nudge. Each operation is an undoable command.
**Scope:** New `SelectionTool`, `SelectionStampCommand`, clipboard data structure. Medium complexity.

#### 1.3 Dirty State Indicator
**Problem:** No visual feedback for unsaved changes. Users don't know if they need to save.
**Solution:** Track a dirty flag (set on any command push, cleared on save). Show `*` in window title and dim the Save button when clean.
**Scope:** Boolean on EditorState + title update logic. ~20 lines.

#### 1.4 Map Resize Dialog
**Problem:** Maps are fixed at 40×30 with no way to change dimensions after creation.
**Solution:** Dialog to set width/height (with anchor: top-left, center, etc.). Existing cell data is preserved or cropped. New cells are null.
**Scope:** `ResizeMapCommand` + `InputDialog` extension for two fields. Medium complexity.

### Priority 2 — Workflow Improvements

#### 2.1 New Project Wizard
**Problem:** Creating a new project requires drag-dropping a PNG then manually configuring everything. No way to set map dimensions.
**Solution:** Guided flow: 1) Choose spritesheet (browse or drag), 2) Set tile size, 3) Set map dimensions, 4) Create. Replaces the implicit "drop a PNG = new project" pattern.
**Scope:** New `NewProjectDialog` or multi-step dialog. Medium.

#### 2.2 Recent Files List
**Problem:** No quick access to previously opened projects. Must use file browser every time.
**Solution:** Store last 5-10 project paths in a local settings file (`~/.tileforge/recent.json`). Show on startup or via File menu.
**Scope:** Small settings file + startup UI. Low-medium.

#### 2.3 Auto-Save
**Problem:** No protection against crashes or accidental closes.
**Solution:** Periodic auto-save (every 2 minutes if dirty) to a `.tileforge.autosave` sidecar file. On startup, detect and offer recovery.
**Scope:** Timer + save logic. Low.

#### 2.4 Grid Overlay Options
**Problem:** Grid is a simple on/off toggle (G key). No control over style.
**Solution:** Grid options: color, opacity, snap subdivisions (half-tile, quarter-tile for entity placement). Accessible via a small toolbar dropdown.
**Scope:** Low. Config struct + minor rendering changes.

### Priority 3 — Professional Polish

#### 3.1 Minimap ✅
**Problem:** On large maps, it's hard to orient. No overview of the full map.
**Solution:** Small minimap overlay in bottom-right of canvas (toggleable via **Ctrl+M**). Shows tile colors derived from group name hash, entity dots, camera viewport rectangle, and player position dot in play mode. Click-to-pan centers camera on clicked world position.
**Implementation:** `TileForge/UI/Minimap.cs` — 170 lines. Max 160px, aspect-ratio preserved, 10px margin. Minimap intercepts clicks before tool dispatch. 12 tests.

#### 3.2 Tile Palette Panel ✅
**Problem:** To create groups, users must open the GroupEditor modal every time. No persistent view of available sprites.
**Solution:** Dockable "Tileset" panel showing the full spritesheet grid. Selected group's sprites are highlighted. Click selects the group owning that sprite. Double-click opens GroupEditor. Ungrouped sprites show a subtle dot indicator.
**Implementation:** `TileForge/UI/TilePalettePanel.cs` — Panel subclass with sprite-to-group index (first-wins when groups share sprites). Scrollable, flexible size mode. 9 tests.

#### 3.3 Export Options ✅ (JSON + PNG; TMX deferred)
**Problem:** `.tileforge` is the only output format. Game engines need clean JSON or PNG renders.
**Solution:** **Ctrl+E** opens export dialog with format toggle (Tab switches JSON/PNG). JSON export strips all editor state (camera, panel order, zoom). PNG renders visible layers at native resolution with transparent background, matching editor's sprite variation formula.
**Implementation:**
- `TileForge/Export/MapExporter.cs` — Clean data model classes (ExportData, ExportLayer, etc.)
- `TileForge/Export/PngExporter.cs` — RenderTarget2D-based, entities rendered after EntityRenderOrder layer
- `DojoUI/ExportDialog.cs` — IDialog with format toggle, path field. 10 tests.
**Deferred:** Tiled TMX export — well-documented XML format, can add later without architectural changes.

#### 3.4 Stamp Brush (Multi-Tile Patterns) ✅
**Problem:** Painting large patterns (e.g., a 3×3 house) requires placing tiles one by one.
**Solution:** When `EditorState.Clipboard` has content and BrushTool is active, the brush paints the full clipboard pattern at the cursor. Stamp preview shows multi-tile outline. **Escape** clears clipboard, returning to normal brush.
**Implementation:** Added `PaintStamp()` and `DrawStampPreview()` to `BrushTool.cs`. Escape chain priority: PlayMode → Clipboard → TileSelection → SelectedEntity → ExitGame. 12 tests.

---

## Refactor Plan

The refactor focuses on **testability**, **separation of concerns**, and **reducing the god class**. Each phase is independently valuable and produces a buildable, runnable editor.

### Phase R1: Extract Constants and Configuration
**Goal:** Eliminate magic numbers, create a single source of truth for layout/sizing.
- Extract hardcoded values (200px dock, 28px toolbar, 22px statusbar, zoom levels, default map size) into a `LayoutConstants` static class
- Extract color constants into themed groups
- No behavior change — pure refactor

**Tests:**
- Verify constants are consistent (e.g., `ToolbarHeight > 0`)
- Snapshot test: serialized default config matches expected values

### Phase R2: Split TileForgeGame (922 lines → ~200 each)
**Goal:** Break the god class into focused managers.
- `ProjectManager` — Save/Load/OpenFile/PromptTileSize logic
- `DialogManager` — Dialog lifecycle (ShowDialog, completion callbacks)
- `InputRouter` — Keyboard shortcut dispatch, tool switching, layer keybinds
- `PlayModeController` — EnterPlayMode, ExitPlayMode, UpdatePlayMode, collision, interaction
- `TileForgeGame` — Retains only Initialize, LoadContent, Update (delegates to managers), Draw

**Tests:**
- `ProjectManager`: Save/Load roundtrip with mock filesystem
- `InputRouter`: Verify correct tool selected for each keybind
- `PlayModeController`: Collision checks with mock map data
- `DialogManager`: Dialog lifecycle state machine

### Phase R3: Introduce Event Bus for State Changes
**Goal:** Decouple UI from direct EditorState mutation.
- Create `EditorEvents` — typed events: `GroupSelected`, `LayerChanged`, `ToolChanged`, `MapDirtied`, `UndoRedoChanged`
- UI components subscribe to events instead of polling EditorState each frame
- EditorState raises events on property changes
- MapPanel, ToolPanel, Toolbar, StatusBar become purely reactive

**Tests:**
- Event subscriptions fire correctly on state changes
- UI components receive expected events
- No duplicate or missed events

### Phase R4: Extract Interfaces for Testability
**Goal:** Enable unit testing without MonoGame runtime.
- `ISpriteSheet` interface (wrapping SpriteSheet for mock injection)
- `IRenderer` interface (wrapping Renderer)
- `IFileSystem` interface (wrapping File.ReadAllText/WriteAllText)
- Tools and commands accept interfaces, not concrete types

**Tests:**
- BrushTool paints correct cells with mock sheet/layer
- FillTool BFS produces correct fill pattern
- EraserTool clears cells
- EntityTool places/moves/removes entities
- CellStrokeCommand undo restores original cells
- All command types: Execute + Undo roundtrip

### Phase R5: Comprehensive Test Suite
**Goal:** Full test coverage for all data and editor logic.
- MapData: bounds checking, layer add/remove, cell get/set
- TileGroup: sprite variation formula
- ProjectFile: serialize/deserialize roundtrip, version migration, relative path resolution
- UndoStack: push/undo/redo sequences, redo clear on new push
- EditorState: AddGroup, RemoveGroup, RenameGroup with map cell updates
- Selection rectangle math
- Camera: ScreenToWorld/WorldToScreen at various zoom levels

**Tests:** 30-50 unit tests covering all data model and editor logic.

---

## Out of Scope (Deferred)
- Tiled TMX export (well-documented XML, can add later)
- MapGen / procedural generation integration
- Auto-tile / terrain rules (9-slice, Wang tiles)
- Tile animation support
- Multiple map tabs
- Plugin / scripting system
- Networked collaboration

---

## Success Criteria

### Refactor (R1–R4) ✅
1. TileForgeGame is under 250 lines → **317 lines** (thin orchestrator, acceptable)
2. All magic numbers extracted to named constants → **83+ constants in LayoutConstants.cs**
3. Event-raising property setters on EditorState → **7 events**
4. ISpriteSheet interface enables testing without MonoGame → **MockSpriteSheet**
5. 286 tests covering all Data and Editor namespaces

### P1 Features ✅
6. Eyedropper tool (I key, Alt+click) picks tiles and entities
7. Selection tool (M key) with Ctrl+C/V, Delete, Escape
8. Map resize dialog (Ctrl+R) with top-left anchor preservation
9. Dirty state indicator (`*` in window title)

### P2 Features ✅
10. New project wizard (Ctrl+N) with browse, tile size, map size
11. Recent files (Ctrl+Shift+O) with persistence
12. Auto-save (2-minute interval, sidecar files, recovery dialog)
13. Grid overlay cycling (G key: Normal → Fine → Off)

### P3 Features ✅
14. Stamp brush paints clipboard pattern; Escape clears
15. Tile palette panel shows spritesheet; click selects group; double-click edits
16. Export JSON (clean, no editor state) and PNG (native resolution)
17. Minimap overlay (Ctrl+M toggle) with click-to-pan and viewport rectangle
18. **453 total tests passing**
