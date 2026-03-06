# TileForge — Editor PRD

## Mission
Professional-level tile map editor for indie top-down 2D RPGs. Focus on editor UX, code quality, and testability.

## Current State (All Complete)

C#/.NET 9/MonoGame 3.8. Custom immediate-mode UI. 453 baseline editor tests, 0 failures.

### Refactor (R1-R4)
- **R1:** 83 constants extracted to `LayoutConstants.cs` (was magic numbers everywhere)
- **R2:** TileForgeGame split 922 -> 317 lines. Created ProjectManager, DialogManager, InputRouter, PlayModeController
- **R3:** 7 events on EditorState with change-guarded setters. Dirty state tracking (`*` in title)
- **R4:** ISpriteSheet interface + MockSpriteSheet for testability without MonoGame

### P1 — Essential Tools
- Eyedropper/Picker (I key, Alt+click) with auto tool-switch
- Selection tool (M key) + copy/paste/delete with undo
- Map resize dialog (Ctrl+R) with top-left anchor preservation
- Dirty state indicator (R3)

### P2 — Workflow
- New project wizard (Ctrl+N) with browse/tile size/map size
- Recent files (Ctrl+Shift+O) persisted to `~/.tileforge/recent.json`
- Auto-save (2min interval, sidecar `.autosave`, recovery dialog)
- Grid overlay cycling (G key: Normal/Fine/Off)

### P3 — Polish
- Stamp brush (clipboard pattern painting via BrushTool)
- Tile palette panel (spritesheet grid, click-to-select, double-click-to-edit)
- Export JSON (clean, no editor state) + PNG (native resolution)
- Minimap overlay (Ctrl+M, click-to-pan, viewport rect)

---

## Out of Scope (Deferred)
- Tiled TMX export
- MapGen / procedural generation
- Auto-tile / terrain rules
- Tile animation
- Plugin / scripting system
- Networked collaboration
