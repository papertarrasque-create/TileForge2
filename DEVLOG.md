# TileForge3 — Development Log

---

## 2026-02-22 — Project Kickoff: Codebase Audit & PRD

### What happened
- Cloned TileForge2 from `https://github.com/papertarrasque-create/TileForge2.git`
- Created `refactor` branch per project rules
- Performed comprehensive codebase audit

### Codebase Summary
| Metric | Value |
|--------|-------|
| Language | C# / .NET 9.0 / MonoGame 3.8 |
| Source files | ~30 (.cs) across 2 projects |
| Largest file | TileForgeGame.cs (922 lines — god class) |
| Test coverage | 0% (no test project exists) |
| Architecture | Data → Editor → UI, Command pattern undo, Strategy pattern tools |
| UI system | Custom immediate-mode (no WinForms/WPF) |

### Key Findings

**Strengths:**
- Clean Data/Editor/UI separation
- Well-structured undo/redo (ICommand + UndoStack)
- Extensible tool system (ITool interface)
- Efficient tile culling in MapCanvas
- Human-readable JSON project files

**Weaknesses (Refactor Targets):**
1. **God class** — TileForgeGame.cs handles I/O, dialogs, state, play mode, rendering (922 lines)
2. **No tests** — zero automated tests of any kind
3. **Magic numbers** — panel width (200), toolbar height (28), statusbar height (22), zoom levels, map size all hardcoded
4. **Tight coupling** — UI directly reads/mutates EditorState
5. **Silent failures** — empty catch blocks on project load
6. **Mixed concerns** — EditorState holds both editor and play mode state

### Deliverables
- [PRD.md](PRD.md) — Enhancement proposals (3 priority tiers) + 5-phase refactor plan
- This devlog

### Enhancement Proposals (Summary)
| Priority | Feature | Rationale |
|----------|---------|-----------|
| P1 | Eyedropper tool | Standard in all tile editors, huge workflow win |
| P1 | Selection tool (copy/paste/move) | Essential for efficient level design |
| P1 | Dirty state indicator | Users need to know when to save |
| P1 | Map resize dialog | Fixed 40×30 is too limiting |
| P2 | New project wizard | Replace implicit PNG-drop workflow |
| P2 | Recent files | Quick access to projects |
| P2 | Auto-save | Crash protection |
| P3 | Minimap | Navigation aid for large maps |
| P3 | Tileset palette panel | Persistent sprite browser |
| P3 | Export (TMX, JSON, PNG) | Interop with game engines |

### Refactor Phases (Summary)
1. **R1** — Extract constants/configuration
2. **R2** — Split TileForgeGame into focused managers
3. **R3** — Event bus for state change decoupling
4. **R4** — Extract interfaces for testability
5. **R5** — Comprehensive test suite (target: 30-50 unit tests)

### Next Steps
- Begin Phase R1: extract magic numbers into LayoutConstants
- Set up xUnit test project
- Write first tests against data model (MapData, TileGroup, ProjectFile)

---

## 2026-02-22 — Phase R1 Complete: Constants Extraction + Test Foundation

### Phase R1: Extract Constants
Created `TileForge2/LayoutConstants.cs` — centralized source of truth for all layout/sizing/color values.

| Category | Constants Extracted |
|----------|-------------------|
| Numeric (sizes, offsets, widths) | 26 |
| Colors (backgrounds, borders, highlights) | 57 |
| **Total** | **83** |

**Files modified:** 13 (all UI classes + tool preview colors)

**Design decisions:**
- Existing public APIs (`PanelDock.Width`, `Toolbar.Height`, etc.) preserved — they delegate to LayoutConstants
- `const` for integers, `static readonly` for Color structs
- DojoUI library left untouched (conservative — shared lib)
- Algorithmic values (icon pixel art, sprite variation formula) left inline

### Test Project Setup
Created `TileForge2.Tests/` xUnit project with comprehensive baseline tests.

| Test File | Tests | Coverage |
|-----------|-------|----------|
| MapDataTests.cs | 21 | Constructor, bounds, layers, add layer |
| MapLayerTests.cs | 18 | Cells, indexing, visibility, bounds safety |
| EditorStateTests.cs | 24 | Add/Remove/Rename groups, map cell updates, entity cleanup |
| UndoStackTests.cs | 18 | Push/undo/redo lifecycle, clear, state flags |
| ProjectFileTests.cs | 31 | JSON roundtrip, restore helpers, path resolution, edge cases |
| **Total** | **112** | **All passing** |

### Verification
- Build: 0 warnings, 0 errors
- Tests: 112/112 passing
- No behavioral changes — pure structural refactor

### Next Steps
- Phase R2: Split TileForgeGame (922 lines) into ProjectManager, DialogManager, InputRouter, PlayModeController
- Write R2 unit tests for the new managers

---

## 2026-02-22 — Phase R2 Complete: TileForgeGame Split + Manager Tests

### Phase R2: Split TileForgeGame
Decomposed the 922-line god class into 4 focused managers + thin orchestrator.

| File | Lines | Responsibility |
|------|-------|---------------|
| TileForgeGame.cs | **317** (was 922) | Orchestrator — Init, Update/Draw delegation, GroupEditor |
| PlayModeController.cs | 212 | Enter/Exit play, movement, collision, camera follow |
| ProjectManager.cs | 239 | Save/Load/Open, tile size parsing, spritesheet loading |
| DialogManager.cs | 53 | Dialog lifecycle, text input routing |
| InputRouter.cs | 170 | All keyboard shortcuts, tool switching, auto-tool-switch |

**66% reduction** in TileForgeGame. Each manager has a single responsibility and clean API.

**Key design decisions:**
- `PlayModeController.Enter()` returns bool instead of accessing Window.Title directly
- `ProjectManager` receives `Action<IDialog, Action<IDialog>>` delegate for dialog display (no direct DialogManager coupling)
- `InputRouter` receives Action callbacks for save/open/play mode (no direct manager coupling)
- `DialogManager.Update()` returns bool to signal "dialog active, skip other input"

### R2 Unit Tests
| Test File | Tests | Coverage |
|-----------|-------|----------|
| ProjectManagerTests.cs | 28 | ParseTileSize: square, rectangular, padding, invalid, zero, negative, whitespace |
| DialogManagerTests.cs | 16 | Show/Update lifecycle, callbacks, text routing, sequential dialogs |
| InputRouterTests.cs | 31 | All keybinds (Ctrl+S/O/Z/Y, tools, layers, F5, Escape, Delete), auto-tool-switch |
| PlayModeControllerTests.cs | 8 | Enter/Exit preconditions, state cleanup, camera restore |
| **New tests** | **87** | |
| **Total suite** | **199** | **All passing** |

**Note:** PlayModeController collision/movement logic requires MonoGame SpriteSheet — will become testable after Phase R4 (interface extraction).

### Verification
- Build: 0 warnings, 0 errors
- Tests: 199/199 passing
- No behavioral changes

### Next Steps
- Phase R3: Event bus for state change decoupling
- Phase R4: Extract interfaces (ISpriteSheet, IRenderer, IFileSystem) for full testability

---

## 2026-02-22 — Phase R3 Complete: Event System + Dirty State Tracking

### Event System
Added 7 C# events to EditorState with change-guarded property setters:

| Event | Type | Trigger |
|-------|------|---------|
| `ActiveToolChanged` | `Action<ITool>` | Tool property setter |
| `ActiveLayerChanged` | `Action<string>` | Layer name property setter |
| `SelectedGroupChanged` | `Action<string>` | Group name property setter |
| `SelectedEntityChanged` | `Action<string>` | Entity ID property setter |
| `PlayModeChanged` | `Action<bool>` | Play mode property setter |
| `MapDirtied` | `Action` | UndoStack.Push/Undo/Redo → MarkDirty() |
| `UndoRedoStateChanged` | `Action` | UndoStack state changes |

Added `StateChanged` event to UndoStack (fires after Push, Undo, Redo, Clear).

**Key design:** Property setters auto-raise events with `if (old != value)` guard — zero changes needed at any mutation site. Fully backwards-compatible.

### Dirty State Tracking (New Feature)
- `IsDirty` flag on EditorState, set by any UndoStack operation
- Window title shows `*` prefix when dirty, cleared on save/load
- `ClearDirty()` called in ProjectManager.Save and Load

### R3 Tests
| Test File | Tests | Coverage |
|-----------|-------|----------|
| EditorStateEventTests.cs | 27 | All 7 events: fire/no-fire, dirty state, multi-subscriber, cascading |
| UndoStackEventTests.cs | 7 | StateChanged on Push/Undo/Redo/Clear, empty-stack no-fire |
| **New tests** | **34** | |
| **Total suite** | **233** | **All passing** |

---

## 2026-02-22 — Phase R4 Complete: ISpriteSheet Interface + Full Test Coverage

### ISpriteSheet Interface
Created `DojoUI/ISpriteSheet.cs` — interface for all SpriteSheet properties/methods used by TileForge2.

**Files modified:**
- `DojoUI/SpriteSheet.cs` — implements `ISpriteSheet`
- `EditorState.Sheet` — changed from `SpriteSheet` to `ISpriteSheet`
- `ProjectFile.Save()` — parameter changed to `ISpriteSheet`
- `GroupEditor.CenterOnSheet()` — parameter changed to `ISpriteSheet`

**IFileSystem skipped** — ProjectFile already had full coverage via JSON roundtrip tests; ISpriteSheet closed the remaining gap.

### Testability Unlocked
With `MockSpriteSheet` (no GraphicsDevice needed), previously untestable logic is now fully covered:

**PlayModeController** — rewritten from 8 limited tests to **40 comprehensive tests:**
- Enter() success/failure paths (13 tests)
- Movement in all 4 directions with lerp verification (11 tests)
- Collision: solid tiles, solid entities, map boundaries (7 tests)
- Entity interaction: walk-into messages, bump messages, timer (4 tests)
- Exit: camera restoration, state cleanup (5 tests)

**ProjectFile.Save()** — **13 new roundtrip tests:**
- Full Save→Load roundtrip preserving all data
- Relative path resolution
- Null/empty edge cases
- Boolean serialization optimization (false omitted, true written)

### R4 Tests
| Test File | New Tests | Coverage |
|-----------|-----------|----------|
| PlayModeControllerTests.cs | +32 (40 total) | Full collision, movement, interaction |
| ProjectFileTests.cs | +13 | Save→Load roundtrip with mock sheet |
| Helpers/MockSpriteSheet.cs | — | Shared test helper |
| **New tests** | **53** | |
| **Total suite** | **286** | **All passing** |

### Verification
- Build: 0 warnings, 0 errors (both DojoUI and TileForge2)
- Tests: 286/286 passing
- No behavioral changes (except R3's dirty state `*` indicator)

---

## Refactor Complete — Final Summary

### Test Growth
| Phase | New Tests | Running Total |
|-------|-----------|---------------|
| R1 (baseline) | 112 | 112 |
| R2 (managers) | +87 | 199 |
| R3 (events) | +34 | 233 |
| R4 (interfaces) | +53 | 286 |
| **Total** | **286** | **286** |

### Architecture Before/After
| Metric | Before | After |
|--------|--------|-------|
| TileForgeGame.cs | 922 lines | 317 lines |
| Magic numbers | ~83 scattered | 1 centralized file |
| Test coverage | 0 tests | 286 tests |
| State mutation | Direct property sets | Event-raising setters |
| MonoGame coupling | Everywhere | Isolated behind ISpriteSheet |
| Dirty state | Not tracked | `*` in title, IsDirty flag |

### Files Created
- `TileForge2/LayoutConstants.cs` — 83 centralized constants
- `TileForge2/PlayModeController.cs` — Play mode logic
- `TileForge2/ProjectManager.cs` — Save/Load/Open
- `TileForge2/DialogManager.cs` — Dialog lifecycle
- `TileForge2/InputRouter.cs` — Keyboard routing
- `DojoUI/ISpriteSheet.cs` — Testability interface
- `TileForge2.Tests/` — 11 test files + 1 helper

---

## 2026-02-22 — Phase P1 Complete: Priority 1 Features

### P1.1 Eyedropper / Picker Tool
- Created `TileForge2/Editor/Tools/PickerTool.cs` — implements ITool
- **I key** switches to Picker tool; **Alt+click** quick-picks on any tool
- Entity detection takes priority over tile cells at same position
- Auto-switches to BrushTool (tile groups) or EntityTool (entity groups) after picking
- Added to ToolPanel as 5th button with procedural eyedropper icon
- Yellow preview color (`PickerPreviewColor`) via LayoutConstants

### P1.2 Selection Tool (Copy/Paste/Delete)
- Created `TileForge2/Editor/Tools/SelectionTool.cs` — rectangular selection via click-drag
- Created `TileForge2/Editor/TileClipboard.cs` — row-major clipboard data structure
- Created `TileForge2/Editor/Commands/PasteCommand.cs` — undoable paste with cell snapshot
- Created `TileForge2/Editor/Commands/ClearSelectionCommand.cs` — undoable clear with cell snapshot
- **M key** activates Selection tool
- **Ctrl+C** copies selection from active layer into clipboard
- **Ctrl+V** pastes clipboard at selection origin via PasteCommand (undoable)
- **Delete** clears selected tiles via ClearSelectionCommand (undoable)
- **Escape** clears selection
- Cyan selection outline rendered in MapCanvas (visible with any active tool)
- Added to ToolPanel as 6th button with selection marquee icon
- Added `SelectionOutlineColor`, `SelectionPreviewColor`, `SelectionOutlineThickness` to LayoutConstants
- Added `TileSelection` and `Clipboard` properties to EditorState
- Tool auto-switch logic skips when SelectionTool is active

### P1.3 Dirty State Indicator
- Already completed in Phase R3 (IsDirty flag, `*` in window title, MapDirtied event)

### P1.4 Map Resize Dialog
- Created `TileForge2/Editor/Commands/ResizeMapCommand.cs` — undoable with full cell snapshot per layer
- Added `MapData.Resize()` — preserves cells top-left anchored, removes out-of-bounds entities
- **Ctrl+R** opens InputDialog with current size as default (e.g. "60x40")
- Minimum size clamped to 1x1

### P1 Tests
| Test File | New Tests | Coverage |
|-----------|-----------|---------|
| PickerToolTests.cs | 13 | OnPress picks tile/entity, priority, auto-switch, edge cases |
| MapDataTests.cs (resize) | +10 | Grow, shrink, preserve, entities, clamp |
| ResizeMapCommandTests.cs | 9 | Execute, undo, roundtrip, multi-layer |
| TileClipboardTests.cs | 14 | Construction, GetCell, row-major indexing, bounds |
| SelectionToolTests.cs | 11 | OnPress/OnDrag/OnRelease, rectangle calc, clear on outside click |
| PasteCommandTests.cs | 9 | Execute pastes, undo restores, null skip, bounds clip |
| ClearSelectionCommandTests.cs | 9 | Execute clears, undo restores, bounds, roundtrip |
| **New P1 tests** | **75** | |
| **Total suite** | **360** | **All passing** |

### Files Created
- `TileForge2/Editor/Tools/PickerTool.cs`
- `TileForge2/Editor/Tools/SelectionTool.cs`
- `TileForge2/Editor/TileClipboard.cs`
- `TileForge2/Editor/Commands/PasteCommand.cs`
- `TileForge2/Editor/Commands/ClearSelectionCommand.cs`
- `TileForge2/Editor/Commands/ResizeMapCommand.cs`

### Files Modified
- `TileForge2/LayoutConstants.cs` — +4 constants (PickerPreviewColor, SelectionOutlineColor, SelectionPreviewColor, SelectionOutlineThickness)
- `TileForge2/InputRouter.cs` — I/M keybinds, Ctrl+C/V, Delete with selection, Escape clears selection
- `TileForge2/Editor/EditorState.cs` — TileSelection, Clipboard properties; ActiveTool setter clears selection on tool change
- `TileForge2/Data/MapData.cs` — Resize() method
- `TileForge2/UI/MapCanvas.cs` — Alt+click quick-pick, selection outline overlay
- `TileForge2/UI/ToolPanel.cs` — 6 tools (was 4), Picker + Selection buttons and icons
- `TileForge2/UI/StatusBar.cs` — Updated hint string with [I] pick, [M] select, Ctrl+C/V

### Verification
- Build: 0 warnings, 0 errors
- Tests: 360/360 passing

### Next Steps
- P2 features: New project wizard, Recent files, Auto-save, Grid overlay

---

## 2026-02-22 — Phase P2 Complete: Priority 2 Features

### P2.1 New Project Wizard
- Created `DojoUI/NewProjectDialog.cs` — multi-field dialog with spritesheet browse, tile size, map size
- Added `ProjectManager.NewProject()` — resets state, loads spritesheet, creates fresh map
- **Ctrl+N** triggers new project wizard
- Tab navigates between fields, Enter confirms
- Browse callback integrates with existing `FileBrowserDialog`

### P2.2 Recent Files List
- Created `TileForge2/RecentFilesManager.cs` — persists to `~/.tileforge/recent.json`, max 10 entries
- Created `DojoUI/RecentFilesDialog.cs` — clickable list dialog with hover highlight, Esc cancel
- `AddRecent()` deduplicates, normalizes paths, trims to max count
- `PruneNonExistent()` removes dead entries before display
- **Ctrl+Shift+O** opens recent files dialog
- Recent files tracked on every Save and Load

### P2.3 Auto-Save
- Created `TileForge2/AutoSaveManager.cs` — periodic sidecar auto-save system
- Auto-saves to `{project}.autosave` sidecar file every 2 minutes (configurable)
- Only triggers when state is dirty and a project path exists
- `CheckForRecovery()` detects autosave files newer than the project file
- `LoadWithRecoveryCheck()` in TileForgeGame shows ConfirmDialog on recovery
- Manual save via `DoSave()` cleans up sidecar file
- Added `ProjectManager.SaveToPath()` — saves without altering dirty state or project path

### P2.4 Grid Overlay Options
- Created `TileForge2/Editor/GridConfig.cs` — `GridMode` enum (Off/Normal/Fine) + `CycleMode()`
- **G key** cycles: Normal → Fine → Off → Normal
- Fine mode draws half-tile subdivision lines
- Grid colors configurable per GridConfig instance
- Added `GridSubdivisionColor` to LayoutConstants
- StatusBar shows current grid mode: `[Grid]`, `[Grid:Fine]`, `[Grid:Off]`

### P2 Tests
| Test File | New Tests | Coverage |
|-----------|-----------|---------|
| GridConfigTests.cs | 8 | Default mode, all cycle transitions, full cycle, default colors |
| AutoSaveManagerTests.cs | 12 | Timer accumulation, dirty/enabled guards, path generation, recovery, cleanup |
| InputRouterP2Tests.cs | 7 | Ctrl+Shift+O (open recent), Ctrl+N (new project), null callback safety, plain N distinction |
| RecentFilesManagerTests.cs | 8 | Add/deduplicate/reorder/trim, null/whitespace guard, prune, path normalization |
| ProjectManagerParseTileSizeTests.cs | 12 | Square, rectangular, padding, combined, whitespace, zero, negative, non-numeric, edge cases |
| **New P2 tests** | **47** | |
| **Total suite** | **407** | **All passing** |

### Files Created
- `DojoUI/NewProjectDialog.cs` — New project wizard dialog
- `DojoUI/RecentFilesDialog.cs` — Recent files selection dialog
- `TileForge2/RecentFilesManager.cs` — Recent files persistence
- `TileForge2/AutoSaveManager.cs` — Auto-save manager
- `TileForge2/Editor/GridConfig.cs` — Grid overlay configuration

### Files Modified
- `TileForge2/TileForge2.csproj` — Added InternalsVisibleTo for test access to ParseTileSize
- `TileForge2/LayoutConstants.cs` — +1 constant (GridSubdivisionColor)
- `TileForge2/Editor/EditorState.cs` — +1 property (Grid)
- `TileForge2/InputRouter.cs` — Ctrl+Shift+O, Ctrl+N keybinds; optional openRecent/newProject params
- `TileForge2/ProjectManager.cs` — NewProject(), OpenRecent(), SaveToPath(), RecentFilesManager integration
- `TileForge2/TileForgeGame.cs` — AutoSaveManager, LoadWithRecoveryCheck(), NewProject()
- `TileForge2/UI/MapCanvas.cs` — GridConfig-based rendering, Fine mode subdivisions
- `TileForge2/UI/StatusBar.cs` — Grid mode display

### Verification
- Build: 0 errors
- Tests: 407/407 passing

---

## 2026-02-22 — P2 Bugfix: Ctrl+N Crash (Dialog Stacking + SpriteFont)

### Bug Report
Ctrl+N crashed the application immediately on dialog open.

### Root Causes

**1. SpriteFont character not in font (crash)**
`NewProjectDialog.Draw()` rendered `"(none — click Browse)"` containing an em dash (`U+2014`). MonoGame's `SpriteFont` only supports characters defined in the font's character range — the em dash was not included.

**Fix:** Replaced em dash with ASCII hyphen: `"(none - click Browse)"`.

**2. Dialog nesting destroyed parent dialog (functional bug)**
When the Browse button inside `NewProjectDialog` opened a `FileBrowserDialog`, `DialogManager.Show()` overwrote the active dialog. The `NewProjectDialog` and its completion callback were permanently lost.

**Fix:** Added dialog stacking to `DialogManager` via `Stack<(IDialog, Action<IDialog>)>`. When `Show()` is called while a dialog is active, the current dialog is pushed onto a stack. When the child dialog completes, the parent is popped and restored.

**3. Browse button fired every frame while held (input bug)**
Browse click handling was in `Draw()` with no edge detection — it fired on every frame the mouse button was held.

**Fix:** Moved click handling from `Draw()` to `Update()` with `_prevMouseLeft` tracking for proper edge detection.

### Tests Added
| Test | Coverage |
|------|----------|
| `Show_WhileDialogActive_PushesAndShowsNew` | Nested dialog replaces parent in update routing |
| `Show_NestedDialogCompletes_RestoresParent` | Parent restored after child completes |
| `Show_NestedDialogCompletes_CallsChildCallbackOnly` | Only child callback fires on child completion |
| `Show_NestedDialogCompletes_ParentCompletesLater` | Full lifecycle: child completes → parent completes |

### Files Modified
- `TileForge2/DialogManager.cs` — Dialog stacking via `Stack<(IDialog, Action<IDialog>)>`
- `DojoUI/NewProjectDialog.cs` — Browse click moved to Update with edge detection; em dash removed
- `TileForge2.Tests/DialogManagerTests.cs` — +3 new stacking tests, 1 updated

### Verification
- Build: 0 errors
- Tests: 410/410 passing

### Next Steps
- P3 features: Minimap, Tileset palette panel, Export (JSON + PNG), Stamp brush

---

## 2026-02-22 — Phase P3 Complete: Professional Polish

### Design Decisions
- **Stamp Brush**: Mode on existing BrushTool (activates when clipboard has content), not a separate tool
- **Tile Palette click**: Click selects group containing sprite; double-click opens GroupEditor
- **Export scope**: JSON + PNG first; TMX deferred (well-documented XML, no architectural blockers)

### P3.4 Stamp Brush
- Modified `TileForge2/Editor/Tools/BrushTool.cs` — Added `PaintStamp()`, `DrawStampPreview()`
- When `EditorState.Clipboard` has content, BrushTool paints full clipboard pattern at cursor
- Escape chain updated: PlayMode → Clipboard (stamp) → TileSelection → SelectedEntity → ExitGame
- No sprite variation applied — stamp reproduces exactly what was copied
- Stamp preview shows multi-tile outline with faint fill

### P3.2 Tile Palette Panel
- Created `TileForge2/UI/TilePalettePanel.cs` — Panel subclass (~170 lines)
- Builds `Dictionary<(int col, int row), TileGroup>` sprite-to-group index (first-wins when groups share sprites)
- Scrollable, flexible size mode, follows established Panel/PanelDock patterns
- Ungrouped sprites show subtle 3×3 dot indicator in bottom-right
- `WantsEditGroup` signal cleared each frame (same pattern as MapPanel)

### P3.3 Export (JSON + PNG)
- Created `TileForge2/Export/MapExporter.cs` — Static `ExportJson()` with clean data model classes
  - Strips all editor state (camera, panel order, zoom, collapsed layers)
  - `IsSolid = false` → null (omitted via `WhenWritingNull`), empty Properties dict → null
- Created `TileForge2/Export/PngExporter.cs` — RenderTarget2D at native resolution
  - Same variation formula `((x*31 + y*37) % count)` as MapCanvas
  - Entities rendered after EntityRenderOrder layer; transparent background
- Created `DojoUI/ExportDialog.cs` — IDialog with format toggle (Tab), path field, Enter/Escape
- Added `SetText()` method to `DojoUI/TextInputField.cs` for format toggle path update
- **Ctrl+E** opens export dialog

### P3.1 Minimap
- Created `TileForge2/UI/Minimap.cs` — Canvas overlay (~170 lines)
- Group name hash → deterministic RGB color (no texture sampling needed)
- Entity dots, camera viewport rectangle, player dot (play mode, 2× size, bright green)
- Click-to-pan: converts minimap position to world coords, centers camera
- Minimap intercepts clicks BEFORE tool dispatch in MapCanvas
- Max 160px, aspect-ratio preserved, 10px margin from canvas edge
- **Ctrl+M** toggles visibility

### P3 Tests
| Test File | New Tests | Coverage |
|-----------|-----------|---------|
| BrushToolStampTests.cs | 12 | Stamp painting, null safety, undo, escape chain, clip to bounds |
| TilePalettePanelTests.cs | 9 | Index building, first-wins, display size, properties |
| MapExporterTests.cs | 10 | JSON format, roundtrip, no editor state, entities, edge cases |
| MinimapTests.cs | 12 | Color determinism, rect calc, click handling, toggle, keybind |
| **New P3 tests** | **43** | |
| **Total suite** | **453** | **All passing** |

### New Constants Added (LayoutConstants.cs)
- Stamp Brush: `StampPreviewColor`, `StampOutlineColor`
- Tile Palette: `TilePalettePanelPreferredHeight`, `TilePaletteMinTileDisplaySize`, `TilePaletteDoubleClickThreshold`, 4 colors
- Minimap: `MinimapMaxSize`, `MinimapMargin`, `MinimapTileAlpha`, 5 colors

### Files Created
- `TileForge2/Editor/Tools/BrushToolStampTests.cs` (test)
- `TileForge2/UI/TilePalettePanel.cs`
- `TileForge2/UI/TilePalettePanelTests.cs` (test)
- `TileForge2/Export/MapExporter.cs`
- `TileForge2/Export/PngExporter.cs`
- `DojoUI/ExportDialog.cs`
- `TileForge2/Export/MapExporterTests.cs` (test)
- `TileForge2/UI/Minimap.cs`
- `TileForge2/UI/MinimapTests.cs` (test)

### Files Modified
- `TileForge2/Editor/Tools/BrushTool.cs` — PaintStamp(), DrawStampPreview()
- `TileForge2/InputRouter.cs` — Ctrl+E export, Ctrl+M minimap, Escape clipboard clearing, `_export`/`_toggleMinimap` params
- `TileForge2/LayoutConstants.cs` — All P3 constants
- `TileForge2/TileForgeGame.cs` — TilePalettePanel, ShowExportDialog(), minimap toggle wiring
- `TileForge2/UI/MapCanvas.cs` — Minimap property, click intercept, draw call
- `TileForge2/UI/StatusBar.cs` — Updated hints with Ctrl+M, Ctrl+E, stamp indicator
- `DojoUI/TextInputField.cs` — SetText() method

### Verification
- Build: 0 errors
- Tests: 453/453 passing

### Test Growth (All Phases)
| Phase | New Tests | Running Total |
|-------|-----------|---------------|
| R1 (baseline) | 112 | 112 |
| R2 (managers) | +87 | 199 |
| R3 (events) | +34 | 233 |
| R4 (interfaces) | +53 | 286 |
| P1 (essential tools) | +75 | 360* |
| P2 (workflow) | +47 | 407* |
| P2 bugfix | +3 | 410 |
| P3 (polish) | +43 | 453 |

*P1 count was 360 before P2 bugfix adjusted to 407→410.

### Keyboard Shortcuts Summary
| Shortcut | Action |
|----------|--------|
| B | Brush tool |
| E | Eraser tool |
| F | Fill tool |
| N | Entity tool |
| I | Picker tool |
| M | Selection tool |
| G | Cycle grid (Normal/Fine/Off) |
| V | Toggle layer visibility |
| Tab | Next layer |
| Shift+Up/Down | Reorder layers |
| Delete | Clear selection / remove entity |
| Escape | Clear stamp → selection → entity → exit |
| Ctrl+S | Save |
| Ctrl+O | Open |
| Ctrl+Shift+O | Open recent |
| Ctrl+N | New project |
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Ctrl+R | Resize map |
| Ctrl+C | Copy selection |
| Ctrl+V | Paste clipboard |
| Ctrl+E | Export (JSON/PNG) |
| Ctrl+M | Toggle minimap |
| F5 | Toggle play mode |

---

## 2026-02-22 — Full Rename: TileForge2 → TileForge

### What
Renamed all project artifacts from `TileForge2` to `TileForge` — folders, solution, csproj, namespaces, assembly names, window titles, file extension, and documentation.

### Changes
| Category | Count |
|----------|-------|
| Namespace/using declarations | 73 .cs files |
| Window title strings | 3 files (9 occurrences) |
| Comments/doc strings | 2 files |
| .csproj InternalsVisibleTo + ProjectReference | 2 files |
| .sln project names + paths | 1 file |
| File extension `.tileforge2` → `.tileforge` | 5 prod + 3 test files |
| Folder renames | TileForge2/ → TileForge/, TileForge2.Tests/ → TileForge.Tests/ |
| Deleted old duplicates | TileForge/CLAUDE.md, TileForge/PRD.md |
| Documentation updates | CLAUDE.md, PRD.md, README.md, MEMORY.md |

### Backward Compatibility
- **Load** accepts both `.tileforge` and `.tileforge2` extensions
- **Save** always uses `.tileforge`
- File browser shows both extensions
- File drop handler accepts both extensions

### New Structure
```
TileForge3/
├── TileForge.sln
├── TileForge/          (was TileForge2/)
├── TileForge.Tests/    (was TileForge2.Tests/)
├── DojoUI/
├── CLAUDE.md
├── PRD.md
├── DEVLOG.md
└── README.md
```
