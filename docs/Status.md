---
updated: 2026-03-06
status: current
---

# TileForge -- Project Status

## Current State

**Branch:** `HUD` (branched from `game-state`, which is ready for merge to `main`)
**Tests:** 1507 passing, 0 failures
**Last milestone:** Sidebar HUD (retro CRPG sidebar with GameLog + scrollable message log)

All planned phases G1-G14 are complete. The HUD phase added a sidebar but no new tests (rendering-only code). Editor phases R1-R4 and P1-P3 are complete.

## Active Work

- Obsidian documentation vault created (24 wiki pages) -- see [[Home]]
- CLAUDE.md slimmed down, legacy root-level docs removed (content captured in vault)
- Sidebar HUD landed previously with text overflow fixes

## Next Up (G15+)

Per PRD-GAME.md, the next planned features are:
- **Ranged combat** -- New `ranged_chase` behavior reading `attack_range`/`preferred_distance` from property bags. Extension point already built.
- **A* pathfinding** -- Build `AStarPathfinder` implementing `IPathfinder`. One-line swap in GameplayScreen.
- **Animated enemy movement** -- Queue `EntityAction` list and play as sequential lerps. EntityAction struct is ready.
- **Standalone registry export** -- `tiles.json`/`entities.json` for external tooling.

## Known Issues

### From Code Review (2026-03-03)

A formal code review identified these issues (see `CODE-REVIEW.md` for full details):

1. **Logic in Draw methods** (C- grade) -- 16 violations where input handling, state mutation, or game logic runs during rendering. RecentFilesDialog and QuestLogScreen are the worst offenders.
2. **No layer depth system** (F grade, but architecturally justified) -- All sprite draws use `SpriteSortMode.Deferred` with painter's algorithm. Works correctly but won't scale to Y-sorting or projectiles.
3. **Excessive SpriteBatch Begin/End pairs** (D grade) -- Up to 52 pairs/frame in worst case, primarily caused by TextInputField scissor clipping.
4. **Per-frame allocations** (C grade) -- Dictionary allocations in SyncEntityRenderState, string concatenation in HUD, LINQ in InventoryScreen, dictionary cloning in SettingsScreen.
5. **TextInputField rasterizer state bug** -- Restores wrong rasterizer state when called from non-scissor context.

### Architectural Concerns

- **GameplayScreen holds EditorState reference** -- Play mode can mutate editor state via `SyncEntityRenderState()`. A `GameWorldView` DTO would be cleaner.
- **Editor modals lack formal lifecycle hooks** -- No `OnEnter()`/`OnExit()` like game screens have. Cleanup is scattered.
- **Property bags are stringly typed** -- All entity properties are `Dictionary<string, string>`. Works for now but error-prone and hard to validate.

## Architectural Health Assessment

**Strengths:**
- Clean editor/play mode boundary via PlayModeController
- Strong screen stack isolation (ScreenManager, A- grade)
- High test coverage relative to codebase size
- Data-driven design has scaled well through 14 phases
- Property bag extensibility has avoided class proliferation

**Weaknesses:**
- Rendering code has accumulated logic that belongs in Update (systematic Draw-side issue)
- Performance debt from per-frame allocations and uncached queries
- Immediate-mode UI means no retained widget state -- some patterns are awkward
- Large files (GameplayScreen is likely 800+ lines, TileForgeGame.cs handles too much routing)

**Overall:** The architecture has held up well through rapid feature development. The main debt is in the rendering layer (Draw-side logic, batch management) and per-frame allocation patterns. The core data model and state management are solid.

## Open Questions

- Should the game runtime eventually be separable from the editor?
- Is the property bag approach sustainable as entity complexity grows?
- When does the project need a proper render layer with depth sorting?
- Should TileForge Next be a rewrite or an evolution of v1?
