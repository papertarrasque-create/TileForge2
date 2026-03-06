# TILEFORGE -- CLAUDE.MD

## What This Is
TileForge is a tile-based level editor with an embedded RPG game runtime. Vanilla C# + MonoGame. No engine magic. The editor is the authoring tool for all game data. Press F5 to play.

## Your Persona
Expert game designer and C# developer.

## Session Protocol
- **Start of every session:** Read `docs/Brief.md` and `docs/Status.md` to load project context and current state.
- **End of every session:** Append a session summary to `docs/Sessions/YYYY-MM-DD.md` (create if needed) and update `docs/Status.md` to reflect any state changes.

---

## Design Principles
- **Data-driven over code-driven.** New content = JSON data, not new classes.
- **Serialize everything.** If GameState can't round-trip to JSON, it doesn't exist.
- **Property bags for extensibility.** `Dictionary<string, string>` on entities avoids premature hierarchies.
- **Editor is the authoring tool.** Gameplay properties are set in the GroupEditor UI and exported.
- **Evolve, don't rebuild.** Enhance existing systems. Don't create parallel ones.
- **One source of truth.** TileGroup defines both tiles and entities. Maps reference by name.
- **Don't ignore bugs.** When you discover a bug, stop what you are doing and fix it.

## Architecture Rules
- Game runtime code: `TileForge/Game/` namespace
- All new classes testable without MonoGame (follow ISpriteSheet pattern)
- System.Text.Json for all serialization
- ScreenManager is play-mode-only -- the editor is NOT a GameScreen
- xUnit tests for everything; 0 failures allowed
- **ASCII-only in rendered strings.** Any string passed to `SpriteFont.MeasureString()` or `SpriteBatch.DrawString()` must use only characters present in the bundled SpriteFont (printable ASCII 32-126). No Unicode ellipsis, em-dashes, curly quotes, or other non-ASCII glyphs -- use ASCII equivalents (`...`, `--`, `"`, `'`).

## Model Assignment (for Claude Code)
- **Sonnet** -- Mechanical work: data classes, enums, serialization, tests following established patterns
- **Opus** -- Architectural integration: GroupEditor UI, PlayModeController evolution, GameplayScreen, multi-system coordination
- Sonnet tasks can run as parallel subagents. Opus tasks run sequentially with full context.

## Documentation

The **docs/** Obsidian vault is the living documentation layer. It is the primary reference for project context:

| Doc | Purpose |
|-----|---------|
| `docs/Brief.md` | Project identity, philosophy, goals |
| `docs/Status.md` | Current state, active work, known issues, open questions |
| `docs/Architecture.md` | System overview, data flow, boundaries, coupling |
| `docs/Changelog.md` | Feature history and architectural shifts |
| `docs/ADRs/` | Architecture Decision Records |
| `docs/Sessions/` | Per-session summaries |

See `docs/Home.md` for a full index of all wiki pages.