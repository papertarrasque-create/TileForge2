---
updated: 2026-03-06
status: current
---

# ADR-001: Initial Architecture Decisions

## Context

TileForge v1 was built as a tile-based map editor that evolved to include an embedded RPG game runtime. Development proceeded through 14+ phases (G1-G14) over approximately two weeks, with architecture decisions made incrementally. This ADR documents the foundational decisions, their rationale, and their consequences -- including decisions that created problems.

## Decisions

### 1. Single Application with Embedded Runtime

**Decision:** The game runtime lives inside the editor application. Play mode is entered via F5 and runs within the same MonoGame process.

**Rationale:** Avoids the complexity of a separate game player executable, shared data formats, and IPC. The editor can directly export maps to memory for instant play-mode entry.

**Consequences:**
- (+) Fast iteration: F5 instantly enters play mode with all project maps pre-exported
- (+) No build step, no asset pipeline, no deployment complexity
- (-) Game runtime is coupled to the editor (GameplayScreen holds EditorState reference)
- (-) Can't distribute a standalone game player without extracting the runtime
- (-) Play mode shares the editor's MonoGame game loop (Update/Draw at fixed 60fps)

### 2. TileGroup as Single Source of Truth

**Decision:** `TileGroup` defines both visual tile properties AND gameplay properties (solid, hazard, defense bonus, noise level, entity type, behavior, etc.). No separate TileDefinition or EntityDefinition classes.

**Rationale:** Avoids parallel class hierarchies and keeps authoring in one place. "One source of truth" -- the GroupEditor UI exposes everything.

**Consequences:**
- (+) Simple mental model: edit a group, all its instances update
- (+) No sync problems between editor data and runtime data
- (-) TileGroup has grown large with many optional properties
- (-) Runtime pays for editor-only data (group names, sprite indices) in memory
- (-) No validation layer between editor data and runtime expectations

### 3. Property Bags for Entity Extensibility

**Decision:** Entity properties are `Dictionary<string, string>` rather than typed class hierarchies. `DefaultProperties` on TileGroup are inherited by instances.

**Rationale:** Avoids class proliferation as entity types grow. New properties can be added without new classes or migration code. The GroupEditor UI dynamically renders controls based on known property names.

**Consequences:**
- (+) Adding new properties (e.g., `equip_poise`, `noise_level`) requires zero structural changes
- (+) Property presets in GroupEditor make common configurations easy
- (-) Stringly typed -- misspelled keys fail silently
- (-) No compile-time validation of property names or value types
- (-) Properties parsed from string on every access (int.TryParse scattered throughout)
- (-) Hard to discover what properties an entity supports without reading documentation
- **Hindsight:** A typed property system (or at least a PropertySchema with validation) would catch errors earlier. The string parsing overhead is noticeable in hot paths.

### 4. Flag-Based Entity Persistence

**Decision:** Entity state (alive/dead, collected) persists via `GameState.Flags` (e.g., `entity_inactive:{id}`). Entities in map files are stateless -- they always exist. The runtime checks flags to determine visibility.

**Rationale:** Maps are immutable templates. State lives entirely in GameState, which serializes cleanly.

**Consequences:**
- (+) Maps never need to be modified at runtime
- (+) Save/load is simple -- just serialize GameState
- (+) Entity state survives map transitions naturally (flags are global)
- (-) Can't query "what entities are inactive" without scanning all flags
- (-) Flag namespace is flat and unstructured -- collision risk grows with content scale
- (-) No way to persist per-entity mutable state (e.g., dialogue progress, shop inventory) beyond binary alive/dead

### 5. Immediate-Mode UI (DojoUI)

**Decision:** All editor UI is custom immediate-mode rendering using MonoGame's SpriteBatch. No WinForms, WPF, ImGui, or retained-mode widget system.

**Rationale:** Full control over rendering. Cross-platform (MonoGame DesktopGL). No external dependencies. Consistent pixel-art aesthetic.

**Consequences:**
- (+) Complete control over look and feel
- (+) No dependency on platform-specific UI frameworks
- (+) Works identically on all platforms MonoGame supports
- (-) No widget state retention -- leads to Draw-side logic accumulation (see code review)
- (-) TextInputField requires scissor batch breaks (up to 50 extra Begin/End pairs per frame)
- (-) Modal editors lack formal lifecycle hooks; cleanup is ad-hoc
- (-) Layout is manual and fragile -- no flex/grid/auto-layout
- **Hindsight:** The lack of an Update/Draw separation convention in the UI layer led to the most significant architectural issue (C- grade in code review). A retained-mode layer for complex widgets (text inputs, modals) would have prevented this.

### 6. Screen Stack for Play Mode

**Decision:** Play mode uses a stack-based `ScreenManager` with `GameScreen` abstract base. Only the topmost screen receives `Update()`. Overlay screens compose visually.

**Rationale:** Clean state machine pattern. Each screen is self-contained with `OnEnter()`/`OnExit()` lifecycle.

**Consequences:**
- (+) Strong isolation between game screens (A- grade in code review)
- (+) Easy to add new screens without modifying existing ones
- (+) Overlay pattern works well for pause menu, dialogue, inventory
- (-) Screens construct child screens in their own constructors (tight coupling)
- (-) Communication between screens uses flags on PlayModeController (outbox pattern) rather than direct calls
- (+) The outbox pattern actually prevents subtle bugs from mid-frame state changes

### 7. System.Text.Json for All Serialization

**Decision:** All save files, project files, dialogue files, and quest files use System.Text.Json.

**Rationale:** Built into .NET. No external dependencies. Human-readable output.

**Consequences:**
- (+) No NuGet dependency for serialization
- (+) JSON files are human-readable and hand-editable
- (-) Inconsistent naming conventions: quests use snake_case, dialogues use camelCase
- (-) Version migration is manual (checking `GameState.Version`, `ProjectFile` V1/V2)
- (-) No schema validation -- malformed files produce confusing errors

### 8. Editor is Not a GameScreen

**Decision:** The editor UI (PanelDock, DialogManager, InputRouter) is NOT wrapped in a GameScreen. ScreenManager is play-mode-only.

**Rationale:** The editor predates the game runtime. Wrapping it would require massive refactoring for no benefit.

**Consequences:**
- (+) Editor UI code isn't constrained by GameScreen lifecycle
- (+) No artificial coupling between editor state and game state
- (-) Two completely different UI paradigms in the same codebase
- (-) `TileForgeGame.Update()` has a cascading priority chain for modal routing (hardcoded if/else)
- (-) Play mode shortcuts can leak into editor (editor shortcuts fire during play mode due to ordering)

### 9. Polling Quest System (No Event Bus)

**Decision:** `QuestManager.CheckForUpdates()` polls flags/variables after state-changing actions. No event bus, no reactive subscriptions.

**Rationale:** Simple, deterministic, easy to test. Quest state derives from flags/variables -- it's a computed view.

**Consequences:**
- (+) Dead simple to understand and test
- (+) No event ordering issues, no missed events, no subscription leaks
- (+) Quest evaluation is a pure function of game state
- (-) Must remember to call `CheckForUpdates()` after every state change
- (-) Evaluates ALL quests on every check (no incremental evaluation)
- (-) Harder to add reactive behaviors (e.g., "trigger dialogue when quest completes")

### 10. Pre-Exported Maps for Play Mode

**Decision:** `PlayModeController.Enter()` exports ALL project maps to an in-memory dictionary. Map transitions load from this dictionary, not the filesystem.

**Rationale:** Instant transitions. No filesystem I/O during gameplay. Maps reflect the editor's current state, not last-saved state.

**Consequences:**
- (+) Map transitions are instantaneous
- (+) Play mode always uses the latest editor state
- (+) No file I/O during gameplay
- (-) Memory usage scales with number of maps (all loaded simultaneously)
- (-) Large projects with many maps could have significant memory overhead at play-mode entry

## Summary of Issues for TileForge Next

The decisions that most need rethinking:

1. **Draw-side logic** -- The immediate-mode UI's lack of Update/Draw discipline is the #1 code quality issue
2. **Stringly-typed property bags** -- Need validation, type safety, or at minimum a schema
3. **GameplayScreen/EditorState coupling** -- Play mode should not hold editor references
4. **TextInputField batch breaking** -- Needs an always-scissor-on approach (2 Begin/End pairs total)
5. **Inconsistent serialization conventions** -- Pick one naming convention and stick with it

## Related Docs

- [[Architecture]] -- Current system overview
- [[Status]] -- Known issues and architectural health
- [[Brief]] -- Project philosophy
