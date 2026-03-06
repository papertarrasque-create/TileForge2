---
updated: 2026-03-06
status: current
---

# Sidebar HUD

A retro CRPG sidebar (inspired by Caves of Qud, SKALD: Against the Black Priory, and Return to Kroz) displaying player stats, equipment, inventory, and a scrollable message log.

## Layout

280px wide, drawn on the right side of the screen **outside** the game viewport scissor rect. Play mode viewport is partitioned: game canvas width = screen width - 280px.

### Sections (top to bottom)

**1. PLAYER**
- Current map name
- HP bar (color-coded: green > 50%, yellow > 25%, red <= 25%)
- PP (Poise) bar (blue > 50%, yellow > 25%, red <= 25%)
- ATK/DEF stats (effective values from [[Equipment]])
- AP pips (`*` = available, `.` = spent)
- Status effects (`[BURNING 3]`, `[POISONED 2]`, etc.)
- COVER indicator (`COVER:+n` in blue when on defensive terrain)

**2. EQUIPMENT**
- Weapon: {name or "-"}
- Armor: {name or "-"}
- Accessory: {name or "-"}

**3. ITEMS**
- Grouped inventory with counts (e.g., "Health Potion x2")
- Max 8 item types displayed, remaining as "+N more"

**4. LOG**
- Scrollable message history
- Word-wrapped text via `TextUtils.WrapText()`
- Color per entry (matches floating message colors)
- Height adapts to remaining space after minimap reservation

**5. MINIMAP**
- Renders at the bottom of the sidebar via `Minimap.DrawInRect()`
- Adaptive height: computed from map aspect ratio within content width, clamped between 60px min and 40% of remaining space
- Shows tiles (color-cached), entity dots, camera viewport rect, and player position
- Canvas minimap is hidden during play mode (sidebar version replaces it)

## GameLog

`GameLog` class -- persistent message log (200 max entries):

```
GameLog
  MaxEntries: 200
  Entries: IReadOnlyList<LogEntry>
  Version: int                      -- Incremented on each Add (change detection)
  Add(text, color)
  Clear()
```

### What Gets Logged

The `LogAndFloat()` pattern in GameplayScreen writes to both floating messages (world-space, temporary) and the GameLog (persistent, scrollable) simultaneously:

| Event | Color |
|-------|-------|
| Combat hits | Gold |
| Damage taken | Red |
| Backstab | OrangeRed |
| Flanked | Orange |
| Poise broken | OrangeRed |
| Poise regen | CornflowerBlue |
| Item collected | LimeGreen |
| Quest events | Cyan |
| Dialogue lines | White |
| Map transitions | White |
| Entity alerts | Yellow |
| Status effects | Varies by type |

DialogueScreen also logs speaker lines independently.

## Log Scrolling

Two scroll modes:

**Auto-scroll (default):**
- Builds visual lines backwards from newest entry
- Fills viewport bottom-up
- Shows `-- older --` hint when content is clipped above

**Manual scroll:**
- Triggered by mouse wheel when hovering sidebar
- 3 entries per wheel tick
- Renders forward from entry offset
- Shows `-- more --` hint when content below
- Re-enables auto-scroll when scrolled past the end

## Floating Messages

World-space temporary text (visual-only, not persisted):

```
FloatingMessage
  Text, Color: string, Color
  TileX, TileY: int              -- World position
  Timer: float                    -- Counts down from Duration
  VerticalOffset: float           -- Drift accumulator
```

| Constant | Value |
|----------|-------|
| Duration | 1.0 second |
| DriftPixels | 16px upward/sec |
| Alpha fade | Last 0.3 seconds |

Rendered in world-space (tile position * tileSize * zoom + camera offset). Multiple floating messages can display simultaneously -- solves the problem of overwritten messages during multi-hit combat.

## Text Wrapping

`TextUtils.WrapText()` in DojoUI:
- Breaks on spaces where possible
- Falls back to character-level breaks for words wider than maxWidth
- Binary search for optimal break points
- Shared utility used by sidebar log and other UI

## Related

- [[Combat]] -- Events that generate log entries and floating messages
- [[Equipment]] -- Stat display in sidebar
- [[Status Effects]] -- Effect indicators in player section
- [[DojoUI]] -- TextUtils.WrapText, ScrollPanel
