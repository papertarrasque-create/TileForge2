# RPG Game State & Data Architecture — Planning Reference (Historical)

> **Status:** All items complete. This document was the original planning sketch for G1–G6. See PRD-GAME.md for what was actually built.

## Original Scope (All Done)

1. **Core Data Model** — TileDefinition/EntityDefinition with property bags, TileRegistry/EntityRegistry, runtime MapData format
2. **Game State Manager** — GameState/PlayerState, flags (`HashSet<string>`), variables, GameStateManager mutation API
3. **Map Loading & Transitions** — MapLoader, trigger entities (`target_map`/`target_x`/`target_y`), flag-based entity persistence
4. **Save/Load** — System.Text.Json, slot-based SaveManager, versioned save format
5. **Screen Management** — GameScreen base, ScreenManager stack, GameplayScreen/PauseScreen
6. **Input Abstraction** — GameAction enum, InputManager with edge detection, rebindable keys

## Design Principles (Carried Forward)

- **Data-driven over code-driven.** New content = JSON data, not new classes.
- **Serialize everything.** If GameState can't round-trip to JSON, it doesn't exist.
- **Property bags for extensibility.** `Dictionary<string, string>` on entities avoids premature hierarchies.
- **Editor is the authoring tool.** Gameplay properties set in GroupEditor UI and exported.
- **One source of truth.** TileGroup defines both tiles and entities. Maps reference by name.
