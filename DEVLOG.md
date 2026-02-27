# TileForge — Development Log

---

## Codebase Audit (Starting Point)

C# / .NET 9.0 / MonoGame 3.8. ~30 source files. TileForgeGame.cs was a 922-line god class. 0 tests. Clean Data/Editor/UI separation, well-structured undo/redo, extensible tool system.

---

## Refactor Phases (R1-R4)

### R1 — Constants Extraction
Extracted 83 magic numbers (26 numeric, 57 colors) into `LayoutConstants.cs`. No behavior changes. Set up xUnit test project with 112 baseline tests.

### R2 — TileForgeGame Split
922-line god class -> 317-line orchestrator + 4 focused managers:
- **PlayModeController** (212 lines) — Enter/Exit play, movement, collision
- **ProjectManager** (239 lines) — Save/Load/Open, tile size parsing
- **DialogManager** (53 lines) — Dialog lifecycle, text input routing
- **InputRouter** (170 lines) — Keyboard shortcuts, tool switching

Key: Managers communicate via delegates/callbacks, no direct coupling. 199 total tests.

### R3 — Events + Dirty State
7 events on EditorState with `if (old != value)` guard setters. `StateChanged` on UndoStack. IsDirty flag with `*` window title indicator. Fully backward-compatible. 233 tests.

### R4 — ISpriteSheet Interface
`ISpriteSheet` + `MockSpriteSheet` enables testing without GraphicsDevice. PlayModeController rewritten from 8 to 40 tests (full collision, movement, interaction coverage). 286 tests.

---

## Feature Phases (P1-P3)

### P1 — Essential Tools (286 -> 360 tests)
- **Picker Tool** — I key / Alt+click, entity priority over tiles, auto-switch to correct tool
- **Selection Tool** — M key, Ctrl+C/V, Delete, Escape. `TileClipboard` + `PasteCommand`/`ClearSelectionCommand`
- **Map Resize** — Ctrl+R, `ResizeMapCommand` with cell snapshot undo, top-left anchored

### P2 — Workflow (360 -> 410 tests)
- **New Project Wizard** — Ctrl+N, `NewProjectDialog` with browse/tile size/map size
- **Recent Files** — Ctrl+Shift+O, `RecentFilesManager` persists to `~/.tileforge/recent.json`
- **Auto-Save** — 2min timer, sidecar `.autosave`, recovery dialog on startup
- **Grid Overlay** — G key cycles Normal/Fine/Off via `GridConfig`

**P2 Bugfix:** Ctrl+N crashed due to em dash in SpriteFont (`U+2014` -> ASCII hyphen) and dialog nesting destroying parent (added `Stack<>` to DialogManager).

### P3 — Polish (410 -> 453 tests)
- **Stamp Brush** — BrushTool mode when clipboard has content. Escape chain: PlayMode -> Clipboard -> Selection -> Entity
- **Tile Palette** — `TilePalettePanel` with sprite-to-group index, click selects, double-click edits
- **Export** — `MapExporter` (clean JSON, no editor state) + `PngExporter` (RenderTarget2D). Ctrl+E
- **Minimap** — Bottom-right overlay, group-hash colors, click-to-pan, Ctrl+M toggle

---

## Rename: TileForge2 -> TileForge

Renamed all artifacts (73 .cs files, csproj, sln, folders, file extension). Backward compat: loads both `.tileforge` and `.tileforge2`, saves as `.tileforge`.

---

## Test Progression

| Phase | Tests |
|-------|-------|
| R1 baseline | 112 |
| R2 managers | 199 |
| R3 events | 233 |
| R4 interfaces | 286 |
| P1 tools | 360 |
| P2 workflow | 410 |
| P3 polish | 453 |

## Keyboard Shortcuts

| Key | Action | Key | Action |
|-----|--------|-----|--------|
| B | Brush | Ctrl+S | Save |
| E | Eraser | Ctrl+O | Open |
| F | Fill | Ctrl+Shift+O | Recent |
| N | Entity | Ctrl+N | New project |
| I | Picker | Ctrl+Z/Y | Undo/Redo |
| M | Selection | Ctrl+R | Resize |
| G | Grid cycle | Ctrl+C/V | Copy/Paste |
| V | Layer visibility | Ctrl+E | Export |
| Tab | Next layer | Ctrl+M | Minimap |
| Delete | Clear/Remove | F5 | Play mode |
| Escape | Dismiss chain | Middle drag | Pan |
