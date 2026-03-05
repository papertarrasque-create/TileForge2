# TileForge MonoGame Best Practices Code Review

**Date:** 2026-03-03
**Branch:** `game-state` (commit `a618534`)
**Reviewer:** Claude Opus 4.6 (automated, 5 parallel audit agents)

---

## Grading Summary

| # | Criterion | Grade | Violations | Notes |
|---|-----------|-------|------------|-------|
| 1 | No logic in Draw | **C-** | 16 violations, 7 borderline | Systematic issue: input handling, state mutation, game logic in Draw |
| 2 | Layer depth + SpriteSortMode | **F** | 0/200+ calls use layerDepth | No depth system exists; BUT has valid architectural justification |
| 3 | Single spriteBatch.Begin/End | **D** | Up to 52 pairs/frame worst case | TextInputField is the primary offender |
| 4 | State machine isolation | **A-** | 5 minor violations | Clean ScreenManager + stack; strong editor/play separation |
| 5 | Stop redoing work | **C** | 6 critical, 10 moderate | Per-frame dict allocs, LINQ, string building, uncached queries |
| 6 | Bugs found | -- | 1 correctness bug | TextInputField restores wrong rasterizer state |

---

## Criterion 1: Never Put Logic in Draw

**16 violations found. 3 high severity, 13 moderate. 20+ methods clean.**

### High Severity

**RecentFilesDialog.cs:75** — Full input handling in Draw. `Mouse.GetState()`, hit-testing, click processing, and `SelectedPath`/`IsComplete` state mutation all happen during rendering. This is the most egregious violation in the codebase.

**QuestLogScreen.cs:62-73** — Quest status evaluation (`GetQuestStatus`, `EvaluateObjective`) runs every frame in Draw, mutating `_activeQuests`/`_completedQuests` lists. Game state queries (flags, variables) have no place in Draw.

**GameplayScreen.cs:370** — `AnyHostileNearby()` iterates all entities with aggro range calculations and Manhattan distance in Draw, just to decide whether to show a HUD hint. Also `GetDefenseBonusAt()` at line 349 iterates all map layers.

### Systematic Pattern: Hit-Test Rect Rebuilding in Draw

Four editor modals share the same anti-pattern — clearing and rebuilding hit-test rectangle lists during Draw, plus running `UpdateTooltipHover()` (which calls `Mouse.GetState()`) in Draw:

- `DialogueTreeEditor.cs:458-469`
- `QuestEditor.cs:286-288`
- `DialogueEditor.cs:262-266`
- `WorldMapEditor.cs:444-449` (also reads `Mouse.GetState()` at line 598)

### Other Violations

| File | Line | Issue |
|------|------|-------|
| `TextInputField.cs` | 89-94 | Cursor blink timer ticked in Draw (frame-rate dependent) |
| `FileBrowserDialog.cs` | 427-463 | Layout fields (`_panelRect`, `_listRect`, `_visibleCount`) + scroll state mutated in Draw |
| `NewProjectDialog.cs` | 143-146 | Mouse hover state computed in Draw |
| `ShortcutsDialog.cs` | 124-126 | Scroll state mutated in Draw |
| `DialogueTreeEditor.cs` | 633 | Widget `IsSelected` property set during Draw |
| `TileForgeGame.cs` | 895 | `_firstDrawLogged` boolean mutated in Draw (trivial) |

### Borderline (acceptable but worth noting)

- `GameplayScreen.Draw` calls `GetEffectiveAttack()`/`GetEffectiveDefense()` — read-only but performs equipment iteration every frame
- `InventoryScreen.Draw` has fallback `BuildMenuItems()` call via `??` operator
- `SettingsScreen.Draw` calls `GetBindings()` which clones the entire dictionary
- `GroupEditor.Draw` calls `ComputeLayout()` — straddles the line between layout and state
- `PanelDock.Draw` reads `Mouse.GetState()` for drag ghost positioning
- `DialogueTreeEditor.DrawCanvas` reads `Mouse.GetState()` for connection drag preview
- `DialogueTreeEditor.DrawCanvas` allocates a new `RasterizerState` every frame

### Clean Methods (properly separated)

PauseScreen, GameOverScreen, SaveLoadScreen, DialogueScreen, MapCanvas, ScreenManager, PlayModeController, StatusBar, MapTabBar, Minimap, AboutDialog, DialogManager, ConfirmDialog, InputDialog, ExportDialog, ContextMenu, Dropdown, Checkbox, NumericField, TooltipManager, MenuBar.

---

## Criterion 2: Layer Depth & SpriteSortMode

**All 11 `spriteBatch.Begin` calls use `SpriteSortMode.Deferred`. Zero Draw calls pass a non-zero `layerDepth`. No depth constant system exists.**

### All Begin Calls

| File | Line | SpriteSortMode |
|------|------|---------------|
| `TileForgeGame.cs` | 905 | Deferred (implicit) — Pass 1: scissor-clipped canvas |
| `TileForgeGame.cs` | 925 | Deferred (implicit) — Pass 2: UI chrome |
| `ScrollPanel.cs` | 49, 68 | Deferred (implicit) — scissor toggle |
| `TextInputField.cs` | 113, 129 | Deferred (implicit) — scissor toggle |
| `FileBrowserDialog.cs` | 469, 494 | Deferred (implicit) — scissor toggle |
| `DialogueTreeEditor.cs` | 553, 599 | Deferred (explicit) — canvas scissor |
| `PngExporter.cs` | 24 | Deferred (implicit) — offscreen render |

### Architectural Justification

This is technically a blanket violation, but the codebase has a **valid reason**: it relies heavily on scissor clipping (ScrollPanel, TextInputField, FileBrowserDialog, DialogueTreeEditor), which requires End/Begin transitions to change `RasterizerState`. Sorted sprite batch modes (`BackToFront`/`FrontToBack`) defer actual drawing until `End()`, which would break scissor rectangle changes mid-batch.

The current painter's algorithm approach (draw-call ordering) works correctly:
- MapCanvas iterates layers in index order
- Entities render at the correct layer boundary via `EntityRenderOrder`
- ScreenManager draws screens in stack order

### Good Practice

All 11 Begin calls correctly use `SamplerState.PointClamp` for pixel-art rendering.

### Recommendation

If the game runtime evolves to need complex sprite ordering (Y-sorting, projectiles, tall sprites), introduce `SpriteSortMode.BackToFront` for the game content pass only (`TileForgeGame.cs:905`), with a `RenderLayers` static class:

```csharp
public static class RenderLayers
{
    public const float TileBase = 0.0f;
    public const float TileOverlay = 0.2f;
    public const float Entity = 0.5f;
    public const float Projectile = 0.6f;
    public const float HudBackground = 0.8f;
    public const float HudText = 0.9f;
}
```

Keep `Deferred` for all UI rendering where scissor clipping is used.

---

## Criterion 3: Single spriteBatch.Begin/End

**11 Begin and 11 End call sites across 6 files. Worst case: ~52 Begin/End pairs per frame.**

### Top-Level Architecture (justified)

`TileForgeGame.cs` uses 2 top-level pairs:
- **Pass 1** (line 905): `Begin(scissor)` — canvas content clipped to canvas bounds
- **Pass 2** (line 925): `Begin(no-scissor)` — UI chrome overlay

`PngExporter.cs` has 1 pair for offscreen rendering (not in frame loop).

These 3 pairs are justified — you cannot change `RasterizerState` mid-batch.

### The Problem: TextInputField

`TextInputField.cs:112-129` calls End/Begin **twice per Draw call** (once to enable scissor, once to "restore"). Every text field in every modal triggers this.

Worst-case frame counts:

| Context | Begin/End pairs |
|---------|----------------|
| Play mode, no dialogs | 2 |
| GroupEditor with entity (8 fields) | ~14 |
| DialogueTreeEditor, node + 4 choices | ~52 |

When called from Pass 1 (which already has scissor enabled), these transitions are **completely unnecessary** — only `GraphicsDevice.ScissorRectangle` needs to change, which doesn't require restarting the batch.

### Bug Found

**`TextInputField.cs:129`** — The "restore" `spriteBatch.Begin` always uses `_scissorRasterizer` regardless of what the caller had. If called from Pass 2 (no scissor), it leaves the batch in scissor mode instead of restoring the caller's state. Compare with `DialogueTreeEditor.cs:599` which correctly saves/restores via `oldRasterizer`.

### Recommended Fix

Standardize on always-enabled scissor clipping (full-screen scissor rectangle = no clipping). Then components only need to change `GraphicsDevice.ScissorRectangle` without ever restarting the batch:

```csharp
// Pass 1: Canvas content
_spriteBatch.Begin(scissorRasterizer);
GraphicsDevice.ScissorRectangle = canvasBounds;
// ... all canvas drawing; children just change ScissorRectangle ...
_spriteBatch.End();

// Pass 2: UI chrome
_spriteBatch.Begin(scissorRasterizer);  // always scissor-enabled
GraphicsDevice.ScissorRectangle = fullScreenBounds;  // full-screen = no clip
// ... all UI drawing; dialogs/fields change ScissorRectangle as needed ...
_spriteBatch.End();
```

This brings the entire codebase down to **2 Begin/End pairs per frame**.

---

## Criterion 4: State Machine Isolation

**Strong implementation. A- grade.**

### Architecture (works well)

**Level 1 — Editor vs. Play Mode:**
- `EditorState.IsPlayMode` boolean with `PlayModeController` as clean mediator
- `Enter()` saves all editor state, creates game runtime, pushes GameplayScreen
- `Exit()` clears screen stack, restores editor state, nulls game runtime

**Level 2 — Play-Mode Screen Stack:**
- Stack-based `ScreenManager` with `GameScreen` abstract base
- `OnEnter()`/`OnExit()` lifecycle hooks, `IsOverlay` property
- Only topmost screen receives `Update()` — lower screens are frozen
- Overlay screens compose visually without logic leakage
- 8 screens: GameplayScreen, PauseScreen, DialogueScreen, InventoryScreen, SaveLoadScreen, SettingsScreen, QuestLogScreen, GameOverScreen

**Level 3 — Editor Update Priority Chain:**
- `TileForgeGame.Update()` uses early-return priority: DialogManager > QuestEditor > DialogueEditor > WorldMapEditor > GroupEditor > InputRouter > Play Mode > Editor

### Strengths

- Each screen is fully self-contained (own `GameMenuList`, status messages, rendering)
- `PlayModeController` acts as outbox pattern — screens set `PendingTransition`/`RestartRequested` flags, controller checks after screen processes
- `GameInputManager` provides action-based abstraction decoupling screens from raw keyboard input
- `InputRouter` skips editor keybinds during play mode
- Comprehensive test coverage (9 ScreenManager tests verifying isolation)

### Minor Issues

| ID | Issue | Impact |
|----|-------|--------|
| V1 | `GameplayScreen` takes direct `EditorState` reference and mutates it via `SyncEntityRenderState()` | Moderate — coupling creates fragile dependency; a play-mode bug could corrupt editor state |
| V2 | Editor modals use cascading nullable-field checks instead of a unified modal manager | Minor — works but doesn't scale |
| V3 | `ToolbarRibbon`/`StatusBar` drawn unconditionally, branch internally on `IsPlayMode` | Trivial — components handle it correctly |
| V4 | Editor shortcuts (Ctrl+Z/R/E) active during play mode — `IsPlayMode` check is after these | Minor — unlikely to cause issues but incomplete isolation |
| V5 | Editor modals lack formal `OnEnter()`/`OnExit()` lifecycle hooks | Minor — cleanup logic scattered in TileForgeGame |

### Recommendation

Introduce a `GameWorldView` data transfer object constructed from `EditorState` during `PlayModeController.Enter()`. Game screens interact only with this object, eliminating the shared mutable state risk (V1).

---

## Criterion 5: Stop Redoing Work

**6 critical, 10 moderate, 7 minor issues. 8 good practices found.**

### Critical: Per-Frame Allocations

| Issue | File:Line | What Happens |
|-------|-----------|--------------|
| New `Dictionary` every frame | `GameplayScreen.cs:278` | `SyncEntityRenderState()` allocates dict + N inserts, unconditionally every `Update()` |
| HUD string `+=` loop | `GameplayScreen.cs:362-364` | AP pips built by string concatenation in a loop every frame |
| HUD string interpolation | `GameplayScreen.cs:341` | ATK/DEF stats text rebuilt every frame via `$"..."` |
| `BuildMenuItems()` + LINQ every frame | `InventoryScreen.cs:88` | `GroupBy().Select().ToList()` — 3 LINQ allocations per `Update()` |
| `GetBindings()` clones dict in Draw | `SettingsScreen.cs:120` | New dictionary + cloned `Keys[]` arrays every frame |
| LINQ `.Select(k.ToString())` in Draw | `SettingsScreen.cs:130` | ~20 string allocations per frame for key display |

### Critical: Duplicate MeasureString

`GameplayScreen.cs:353,365` — `font.MeasureString(statsText)` called twice on the same string every frame. Store the result in a local.

### Moderate: Uncached Expensive Operations

| Issue | File:Line | Impact |
|-------|-----------|--------|
| Minimap full map scan | `Minimap.cs:38-60` | 7,200 cells (60x40x3) + hash per cell, every frame |
| `AnyHostileNearby()` x3 per turn | `GameplayScreen.cs:370,775,816` | Full entity scan with aggro range, called in Draw + 2 Update paths |
| Equipment iteration every call | `GameStateManager.cs:379-390` | `GetEffectiveAttack()`/`Defense()` iterate equipment, called every frame |
| `ToolbarRibbon.ComputeButtonRects()` x2 | `ToolbarRibbon.cs:75,155` | Same rects computed in both Update and Draw |
| `MapPanel.ComputeLayout()` x2-3 | `MapPanel.cs:192,287,493` | Clears and rebuilds `_entries` list each time |
| `CenterCameraOnPlayer()` every frame | `GameplayScreen.cs:266` | Runs even when player hasn't moved |
| `DialogueScreen.Draw` substring + measure | `DialogueScreen.cs:200,203` | `Substring()` + `MeasureString()` on full text every frame |
| `QuestLogScreen.Draw` evaluates all quests | `QuestLogScreen.cs:66-73` | Quest status + objective evaluation every frame |

### Minor

- `new Color()` struct constructors in Draw (GameplayScreen.cs:317,331,420) — should be `static readonly`
- `MeasureString("PAUSED")` / `MeasureString("GAME OVER")` every frame for static text
- `GetCanvasBounds()` called 5+ times in a single Update
- `MapTabBar.Draw` truncates tab names via O(n^2) `MeasureString` while-loop

### Good Practices Found

- `Renderer` caches its 1x1 pixel texture (created once, reused)
- `MapCanvas.Draw()` implements viewport culling (only renders visible tiles)
- `GameStateManager.ProcessStatusEffects()` reuses a field-level list via `.Clear()`
- `QuestManager.CheckForUpdates()` reuses `_reusableEvents` list
- `GameInputManager.IsActionJustPressed()` uses raw foreach, no LINQ
- Overlay screens use `static readonly string[]` for menu items
- `FloatingMessages` uses reverse iteration for safe `RemoveAt`
- Layout constants properly declared as `const`/`static readonly`

---

## Bug Report

### BUG: TextInputField Rasterizer State Restore

**File:** `DojoUI/TextInputField.cs:129`
**Severity:** Low (cosmetic, may cause clipping artifacts)

After drawing text with scissor clipping, the "restore" `spriteBatch.Begin` always uses `_scissorRasterizer` instead of saving and restoring the caller's actual rasterizer state. When called from a non-scissor context (Pass 2), this silently leaves the batch in scissor mode.

**Compare with:** `DialogueTreeEditor.cs:599` which correctly saves `oldRasterizer` before its scissor batch and restores it afterward.

**Fix:** Save the caller's rasterizer state before the first `End()` and restore it in the final `Begin()`.

---

## Priority Fixes (ordered by impact)

1. **TextInputField Begin/End elimination** — Eliminate per-Draw End/Begin cycles. Accept the caller's scissor state; only change `ScissorRectangle`. Fix the rasterizer restore bug. *Eliminates up to 50 batch flushes per frame.*

2. **RecentFilesDialog input handling** — Move all `Mouse.GetState()`, hit-testing, and click processing to an `Update` method. *Most egregious Draw-logic violation.*

3. **GameplayScreen HUD caching** — Cache `statsText`, AP pips, cover value, `AnyHostileNearby()` result, and `MeasureString` results in Update. Reuse the `activeById` dictionary in `SyncEntityRenderState()` via `.Clear()`. *Eliminates ~10 allocations + expensive queries per frame.*

4. **InventoryScreen / SettingsScreen dirty flags** — Don't rebuild menu items or clone bindings every frame. Only rebuild when inventory/bindings actually change. *Eliminates LINQ + dictionary clone per frame.*

5. **Editor modal Draw cleanup** — Move hit-test rect computation and `UpdateTooltipHover()` from Draw to Update across DialogueTreeEditor, QuestEditor, DialogueEditor, WorldMapEditor. *Systematic fix for 4 classes.*

6. **Minimap caching** — Render to a cached `RenderTarget2D`; only regenerate on map mutation. *Eliminates 7,200-cell scan per frame.*
