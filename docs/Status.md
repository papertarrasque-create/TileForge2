---
updated: 2026-03-17
status: current
---

# TileForge -- Project Status

## Current State

**Branch:** `Dialog` (branched from `HUD` from `game-state`)
**Tests:** 1757 passing, 0 failures
**Last milestone:** Combat Knockback System -- knockback mechanic with weight-based resistance

All planned phases G1-G14 are complete. Editor phases R1-R4 and P1-P3 are complete. Dialogue 2.0 is complete with v1 compat code removed.

## Active Work

- **Combat Knockback (complete):** Defender knocked back 1 tile after attacks. Weight property (1-3) controls resistance. Stagger tracking per-turn. Symmetric for player and entity attacks. KnockbackResolver follows EntityAI pattern (static, pure). See spec: `docs/superpowers/specs/2026-03-17-combat-knockback-design.md`.
- **Previous:** Property Schema Registry, V1 Cleanup, Dialogue 2.0, G15 gameplay features, HUD minimap -- see [[Changelog]]

## Next Up (G15+)

### Gameplay Features (from Notes 2026-03-06)
- **Residual damage effects** -- Lingering damage (e.g., fire) with continued damage flash on sprite for duration.

### Architecture/Engine
- **Ranged combat** -- New `ranged_chase` behavior reading `attack_range`/`preferred_distance` from property bags. Extension point already built.
- **A* pathfinding** -- Build `AStarPathfinder` implementing `IPathfinder`. One-line swap in GameplayScreen.
- **Animated enemy movement** -- Queue `EntityAction` list and play as sequential lerps. EntityAction struct is ready.
- **Standalone registry export** -- `tiles.json`/`entities.json` for external tooling.

## Known Issues

### From Code Review (2026-03-03, re-assessed 2026-03-16)

Original review identified 5 issues. Re-assessment after dialogue 2.0 and v1 cleanup work:

1. ~~**Logic in Draw methods**~~ -- Originally 16 violations (C-), **all resolved**. Final 4 fixed: QuestLogScreen (filtering moved to Update), PanelDock (Mouse.GetState cached in Update), DialogueTreeEditor (mouse hover uses cached position), RecentFilesDialog (rect computation extracted to Update).
2. ~~**No layer depth system**~~ -- **Removed as issue.** Deferred mode with painter's algorithm is a deliberate design choice required by scissor clipping (TextInputField, DialogueTreeEditor). EntityRenderOrder handles layer boundaries. No Y-sorting or projectiles in current feature set.
3. ~~**Excessive SpriteBatch Begin/End pairs**~~ -- **Mitigated.** TextInputField now only creates extra Begin/End pairs when text actually overflows the field bounds. Most fields render in the caller's batch with zero overhead; scissor clipping only activates when needed.
4. ~~**Per-frame allocations**~~ -- **All resolved.** SidebarHUD Dictionary/List now use Clear() reuse pattern. Previous fixes: SyncEntityRenderState dict, AP/stats text cached, InventoryScreen LINQ gated by dirty flag, SettingsScreen dict clone gated by dirty flag.
5. ~~**TextInputField rasterizer state bug**~~ -- **Fixed.** Now checks caller's scissor state and restores with a known-good rasterizer object instead of relying on potentially stale device state.

### Architectural Concerns

- **GameplayScreen holds EditorState reference** -- Play mode mutates editor state via `SyncEntityRenderState()`. Mitigated by deep-copy snapshot in PlayModeController (revert on exit). A `GameWorldView` DTO would still be cleaner long-term.
- **Editor modals lack formal lifecycle hooks** -- No `OnEnter()`/`OnExit()` like game screens have. Cleanup is scattered.
- ~~**Property bags are stringly typed**~~ -- **Resolved.** PropertySchema registry provides compile-time key constants, typed access helpers, and load-time validation. `Dictionary<string, string>` retained for serialization; all access goes through `PropertyKeys` + `PropertyAccess`.
- **UndoStack event wiring stale after play mode revert** -- `PlayModeController.Exit()` skips `WireUndoStack()` when `ActiveMapIndex` hasn't changed. Dirty tracking breaks after revert. Low functional impact.

## Architectural Health Assessment

**Strengths:**
- Clean editor/play mode boundary via PlayModeController
- Strong screen stack isolation (ScreenManager, A- grade)
- High test coverage relative to codebase size
- Data-driven design has scaled well through 14 phases
- Property bag extensibility has avoided class proliferation

**Weaknesses:**
- Immediate-mode UI means no retained widget state -- some patterns are awkward
- Large files (GameplayScreen, TileForgeGame.cs)

**Overall:** The architecture has held up well through rapid feature development. All original code review debt has been fully addressed. The core data model, state management, and dialogue/quest systems are solid and validated end-to-end.

## Open Questions

- Should the game runtime eventually be separable from the editor?
- ~~Is the property bag approach sustainable as entity complexity grows?~~ -- Addressed by PropertySchema registry; path to strict mode (lock down unknown keys) when ready.
- Should TileForge Next be a rewrite or an evolution of v1?
