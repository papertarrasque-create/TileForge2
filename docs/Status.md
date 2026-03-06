---
updated: 2026-03-06
status: current
---

# TileForge -- Project Status

## Current State

**Branch:** `HUD` (branched from `game-state`, which is ready for merge to `main`)
**Tests:** 1539 passing, 0 failures
**Last milestone:** G15 gameplay features (pickup dialogue, concluded dialogue, terrain notifications)

All planned phases G1-G14 are complete. Editor phases R1-R4 and P1-P3 are complete.

## Active Work

- **G15 gameplay features:**
  - **Pickup dialogue:** `on_pickup_dialogue` entity property shows dialogue on first pickup of an item group. Tracked via `pickup_dialogue_shown:{name}` flag.
  - **Concluded dialogue:** `concluded_flag` + `concluded_dialogue` properties on NPC/Interactable entities. When the flag is set, shows reminder dialogue instead of main tree.
  - **Terrain notifications:** GameLog message when stepping onto slow terrain (MovementCost > 1.0), showing group name and cost multiplier.
- **Previous:** Play mode revert/keep, HUD minimap, Obsidian vault -- see [[Changelog]]

## Next Up (G15+)

### Gameplay Features (from Notes 2026-03-06)
- **Residual damage effects** -- Lingering damage (e.g., fire) with continued damage flash on sprite for duration.
- **Combat pace redesign** -- Current spam-bump is too fast. Needs research on deliberate, tactical alternatives.

### Architecture/Engine
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

- **GameplayScreen holds EditorState reference** -- Play mode mutates editor state via `SyncEntityRenderState()`. Mitigated by deep-copy snapshot in PlayModeController (revert on exit). A `GameWorldView` DTO would still be cleaner long-term.
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
