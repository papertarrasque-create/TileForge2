---
updated: 2026-03-06
status: current
---

# TileForge -- Project Brief

## What It Is

TileForge is a tile-based level editor with an embedded RPG game runtime. It is a single desktop application -- not an engine, not a framework. You author maps, place entities, define dialogue and quests, then press F5 to play. Everything is built in vanilla C# on MonoGame, with no engine abstractions or plugin systems.

The editor *is* the authoring tool. There is no separate data pipeline, no external scripting layer, no asset importer. Game content is defined through the editor UI and serialized as JSON.

## Philosophy

- **Data-driven over code-driven.** New content is JSON data, not new classes.
- **Serialize everything.** If it can't round-trip to JSON, it doesn't exist in the game state.
- **Property bags for extensibility.** `Dictionary<string, string>` on entities avoids premature hierarchies.
- **Editor is the single source of truth.** All gameplay data is authored in the GroupEditor UI and exported.
- **Evolve, don't rebuild.** Enhance existing systems rather than creating parallel ones.
- **One source of truth.** TileGroup defines both tiles and entities. Maps reference by name.
- **Don't ignore bugs.** Fix them immediately when discovered.

## Goals

1. Professional-level tile map editor for indie top-down 2D RPGs
2. Embedded play mode that grows into a full RPG runtime (turn-based, tactical)
3. In-editor authoring for all game data -- dialogue, quests, world layout, entity properties
4. Testable without MonoGame (`ISpriteSheet` pattern, pure logic separated from rendering)
5. Maintainable codebase with high test coverage (1528 tests, 0 failures)

## Current Scope

The editor is feature-complete for v1 (see [[Status]]). The game runtime covers:
- Tile/entity painting with layers, groups, undo/redo
- Multimap projects with tab-based map management
- AP-based tactical combat with terrain defense, backstab/flanking, poise
- Entity AI (idle/chase/patrol/chase_patrol) with noise/alertness stealth
- Equipment, inventory, status effects
- Map transitions (trigger-based + edge-based via world layout grid)
- Save/load, dialogue, quests
- Visual dialogue tree editor, quest editor, world map editor
- Retro CRPG sidebar HUD with scrollable message log

## What It Is Not

- Not a general-purpose game engine
- Not a Tiled competitor (no TMX export)
- Not multiplayer or networked
- Not scriptable (no plugin/mod system)
- No procedural generation, auto-tiling, or tile animation (all deferred)

## Tech Stack

- C# / .NET 9.0
- MonoGame 3.8 (DesktopGL)
- Custom immediate-mode UI (DojoUI)
- System.Text.Json for all serialization
- xUnit for testing

## Related Docs

- [[Architecture]] -- System overview and data flow
- [[Status]] -- Current state and open work
- [[Changelog]] -- Feature history
- [[ADRs/001-initial-architecture|ADR-001]] -- Foundational architecture decisions
