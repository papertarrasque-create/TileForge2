---
updated: 2026-03-16
status: current
---

# TileForge -- Project Status

## Current State

**Branch:** `Dialog` (branched from `HUD` from `game-state`)
**Tests:** 1681 passing, 0 failures
**Last milestone:** Phase 5 V1 Cleanup -- removed all v1 backward-compatibility fallback code

All planned phases G1-G14 are complete. Editor phases R1-R4 and P1-P3 are complete. Dialogue 2.0 is complete with v1 compat code removed.

## Active Work

- **V1 Cleanup (complete):** Removed legacy DialogueEditor, QuestRewards class, v1 fallback code from DialogueScreen, concluded_flag/concluded_dialogue entity properties. V1 JSON properties kept for deserialization; MigrateV1ToV2 handles conversion on load.
- **Dialogue 2.0 (complete):** Routes with conditions, actions, oneShot, unified triggering. DialogueTreeEditor now has native v2 UI. See [[ADR-002]].
- **Previous:** G15 gameplay features (pickup dialogue, terrain notifications), play mode revert/keep, HUD minimap, Obsidian vault -- see [[Changelog]]

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

### From Code Review (2026-03-03, re-assessed 2026-03-16)

Original review identified 5 issues. Re-assessment after dialogue 2.0 and v1 cleanup work:

1. **Logic in Draw methods** -- Originally 16 violations (C-), now **4 remaining**. SettingsScreen, SaveLoadScreen, InventoryScreen, PauseScreen, GameOverScreen all refactored. Remaining: QuestLogScreen (quest filtering in Draw -- high), PanelDock (3x Mouse.GetState in Draw -- high), DialogueTreeEditor (mouse hover -- moderate), RecentFilesDialog (hit-test cache -- moderate).
2. ~~**No layer depth system**~~ -- **Removed as issue.** Deferred mode with painter's algorithm is a deliberate design choice required by scissor clipping (TextInputField, DialogueTreeEditor). EntityRenderOrder handles layer boundaries. No Y-sorting or projectiles in current feature set.
3. **Excessive SpriteBatch Begin/End pairs** (D grade) -- Still present. TextInputField creates 2 extra pairs per Draw call. No easy fix without rearchitecting the scissor approach. Low runtime impact.
4. **Per-frame allocations** -- Original issues mostly fixed. SyncEntityRenderState dict (fixed, reuse pattern), AP/stats text (fixed, cached), InventoryScreen LINQ (gated by dirty flag), SettingsScreen dict clone (gated by dirty flag). **New issues:** SidebarHUD allocates `new Dictionary` for inventory and `new List` for message log every Draw call -- easy fix using same Clear() pattern.
5. **TextInputField rasterizer state bug** -- Still present. Restores potentially wrong rasterizer state when called from non-scissor context. Cosmetic impact only.

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
- 4 remaining Draw-side logic violations (down from 16)
- SidebarHUD has 2 new per-frame allocations (easy fix)
- TextInputField scissor clipping creates excess SpriteBatch pairs and has rasterizer bug
- Immediate-mode UI means no retained widget state -- some patterns are awkward
- Large files (GameplayScreen, TileForgeGame.cs)

**Overall:** The architecture has held up well through rapid feature development. Most original code review debt has been addressed. Remaining issues are localized (SidebarHUD allocations, 4 Draw-side violations) rather than systemic. The core data model, state management, and dialogue/quest systems are solid and validated end-to-end.

## Open Questions

- Should the game runtime eventually be separable from the editor?
- Is the property bag approach sustainable as entity complexity grows?
- Should TileForge Next be a rewrite or an evolution of v1?
