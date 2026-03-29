---
updated: 2026-03-28
status: current
---

# TileForge -- Project Status

## Current State

**Branch:** `Dialog` (branched from `HUD` from `game-state`)
**Tests:** 1796 passing, 0 failures
**Last milestone:** Combat Rebalance -- two-tier enemy progression with 5 enemy types

All planned phases G1-G14 are complete. Editor phases R1-R4 and P1-P3 are complete. Dialogue 2.0 is complete with v1 compat code removed.

## Active Work

- **Combat Rebalance (complete):** Player poise 20->10, defense 2->3. Five enemy types across two tiers: Tier 1 (Rat speed 2, Goblin, Skeleton chase_patrol) and Tier 2 (Zombie weight 3, Ghost speed 2). New entity groups in QuestTestMap.tileforge with placeholder sprites at (1,1).
- **Adventure Designer Skill (complete):** Claude Code skill (`/adventure-designer`) for game content generation. Tested and deployed for combat rebalance.
- **Previous:** Entity Animation, Combat Knockback, Property Schema Registry, V1 Cleanup, Dialogue 2.0, G15 gameplay features, HUD minimap

## Next Up (G15+)

### Gameplay Features (from Notes 2026-03-06)
- **Residual damage effects** -- Lingering damage (e.g., fire) with continued damage flash on sprite for duration.

### Architecture/Engine
- **Ranged combat** -- New `ranged_chase` behavior reading `attack_range`/`preferred_distance` from property bags. Extension point already built.
- **A* pathfinding** -- Build `AStarPathfinder` implementing `IPathfinder`. One-line swap in GameplayScreen.
- **Animated enemy movement** -- Extend EntityAnimator to NPC movement. Requires refactoring `ExecuteEntityTurn` from synchronous loop to async/state-machine for multi-AP sequential animations. EntityAnimator API already supports it (`StartHop` works with any ID).
- **Standalone registry export** -- `tiles.json`/`entities.json` for external tooling.

## Known Issues

### Architectural Concerns

- **GameplayScreen holds EditorState reference** -- Play mode mutates editor state via `SyncEntityRenderState()`. Mitigated by deep-copy snapshot in PlayModeController (revert on exit). A `GameWorldView` DTO would still be cleaner long-term.
- **Editor modals lack formal lifecycle hooks** -- No `OnEnter()`/`OnExit()` like game screens have. Cleanup is scattered.
- ~~**Property bags are stringly typed**~~ -- **Resolved.** PropertySchema registry provides compile-time key constants, typed access helpers, and load-time validation. `Dictionary<string, string>` retained for serialization; all access goes through `PropertyKeys` + `PropertyAccess`.
- **UndoStack event wiring stale after play mode revert** -- `PlayModeController.Exit()` skips `WireUndoStack()` when `ActiveMapIndex` hasn't changed. Dirty tracking breaks after revert. Low functional impact.

## Open Questions

- Should the game runtime eventually be separable from the editor?
- ~~Is the property bag approach sustainable as entity complexity grows?~~ -- Addressed by PropertySchema registry; path to strict mode (lock down unknown keys) when ready.
- Should TileForge Next be a rewrite or an evolution of v1?
